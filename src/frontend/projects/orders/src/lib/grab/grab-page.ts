import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import {
  ApiProblem,
  AuthState,
  FindDetail,
  FindsApi,
  GrabQuote,
  GrabsApi,
  ShippingAddress,
  shortRef,
} from '@tutumuch/api';
import {
  AlertComponent,
  ButtonDirective,
  CenterStateComponent,
  ChoiceComponent,
  ErrorSummaryComponent,
  FormFieldComponent,
  IconComponent,
  OrderSummaryComponent,
  OrderSummaryItem,
  SkeletonComponent,
  SummaryError,
  ToastService,
} from '@tutumuch/components';

import { ADDRESS_FIELDS } from '../checkout/checkout-store';
import { HostedCardFieldsComponent } from '../checkout/hosted-card-fields';
import { COUNTRIES, US_STATES } from '../checkout/us-states';
import { formatMoney } from '../money';

/** How often and how long the page asks whether the webhook settled the payment. */
const POLL_INTERVAL_MS = 1000;
const POLL_ATTEMPTS = 60;
const ACK_ERROR = 'Tick the box to confirm you understand the deposit';

type View = 'loading' | 'ready' | 'in-progress' | 'unverified' | 'error';
type Alert =
  | { kind: 'non-us' }
  | { kind: 'declined'; reason: string }
  | { kind: 'total-changed' }
  | { kind: 'card'; message: string }
  | { kind: 'error'; ref: string };

/**
 * "Grab it for me" at /finds/{reference}/grab (L2-117, mocks pages/grab, ADR orders/0009): a US
 * shipping address prices the grab; the summary shows the shelf price, grab fee, tax, total and the
 * non-refundable deposit; the shopper ticks the acknowledgement and pays the total with the
 * provider's hosted card fields, once per idempotency key. The page then polls until the webhook
 * has decided and goes to the grab.
 */
@Component({
  selector: 'tl-grab-page',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    AlertComponent,
    ButtonDirective,
    CenterStateComponent,
    ChoiceComponent,
    ErrorSummaryComponent,
    FormFieldComponent,
    HostedCardFieldsComponent,
    IconComponent,
    OrderSummaryComponent,
    SkeletonComponent,
  ],
  templateUrl: './grab-page.html',
  styleUrl: './grab-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GrabPage {
  private readonly finds = inject(FindsApi);
  private readonly grabs = inject(GrabsApi);
  private readonly auth = inject(AuthState);
  private readonly router = inject(Router);
  private readonly toasts = inject(ToastService);
  private readonly card = viewChild(HostedCardFieldsComponent);
  /** The key of the current payment attempt; kept when the request may not have arrived. */
  private attemptKey: string | null = null;

  /** The find reference, from the route parameter. */
  readonly reference = input.required<string>();

  readonly states = US_STATES;
  readonly countries = COUNTRIES;
  readonly money = formatMoney;
  readonly form = inject(NonNullableFormBuilder).group({
    fullName: '',
    line1: '',
    line2: '',
    city: '',
    state: '',
    zip: '',
    country: 'US',
  });

  readonly view = signal<View>('loading');
  readonly find = signal<FindDetail | null>(null);
  readonly quote = signal<GrabQuote | null>(null);
  readonly acknowledged = signal(false);
  readonly ackError = signal<string | null>(null);
  readonly errors = signal<Record<string, string>>({});
  readonly alert = signal<Alert | null>(null);
  readonly pricing = signal(false);
  readonly paying = signal(false);
  readonly errorRef = signal('');

  /** The non-US refusal is an alert of its own, not a summary (mocks pages/grab non-us). */
  readonly summary = computed<SummaryError[]>(() => {
    if (this.alert()?.kind === 'non-us') return [];
    const fields: SummaryError[] = ADDRESS_FIELDS.filter(({ id }) => this.errors()[id]).map(({ id }) => ({
      fieldId: id,
      message: this.errors()[id],
    }));
    return this.ackError() ? [...fields, { fieldId: 'ack', message: this.ackError()! }] : fields;
  });
  readonly summaryItem = computed<OrderSummaryItem | null>(() => {
    const find = this.find();
    return find
      ? { title: find.title, meta: `${find.storeName} · ${find.reference}`, photo: null, icon: 'map-pin' }
      : null;
  });
  readonly payLabel = computed(() => {
    const quote = this.quote();
    return quote ? `Pay ${formatMoney(quote.total)}` : 'See the total';
  });

  constructor() {
    effect(() => {
      const reference = this.reference();
      untracked(() => void this.load(reference));
    });
    this.form.valueChanges.subscribe(() => {
      // A new address needs a new quote before paying (L2-027 AC5).
      if (this.quote() && !this.paying()) this.quote.set(null);
    });
  }

  async load(reference = this.reference()): Promise<void> {
    this.view.set('loading');
    await this.auth.load();
    const member = this.auth.member();
    if (!member) {
      await this.router.navigate(['/sign-in'], {
        queryParams: { returnUrl: `/finds/${reference}/grab` },
        replaceUrl: true,
      });
      return;
    }
    if (!member.emailVerified) {
      this.view.set('unverified');
      return;
    }
    const result = await this.finds.get(reference);
    if (!result.ok) {
      if (result.problem.status === 404) {
        await this.router.navigateByUrl(`/finds/${reference}`);
        return;
      }
      this.errorRef.set(shortRef(result.problem.correlationId));
      this.view.set('error');
      return;
    }
    const find = result.value;
    if (find.status !== 'Active' || !find.grabFee || find.mine) {
      await this.router.navigateByUrl(`/finds/${reference}`);
      return;
    }
    this.find.set(find);
    this.view.set(find.grabOpen ? 'in-progress' : 'ready');
  }

  toggleAck(checked: boolean): void {
    this.acknowledged.set(checked);
    if (checked) this.ackError.set(null);
  }

  /** "See the total" prices the address; "Pay {total}" pays it. */
  async submit(): Promise<void> {
    if (this.pricing() || this.paying()) return;
    if (!this.quote()) {
      await this.price();
      return;
    }
    if (!this.acknowledged()) {
      this.ackError.set(ACK_ERROR);
      return;
    }
    await this.pay();
  }

  private address(): ShippingAddress {
    const value = this.form.getRawValue();
    return { ...value, line2: value.line2.trim() || null };
  }

  private async price(): Promise<void> {
    this.pricing.set(true);
    this.alert.set(null);
    this.errors.set({});
    const result = await this.grabs.quote(this.reference(), this.address());
    this.pricing.set(false);
    if (result.ok) {
      this.quote.set(result.value);
      return;
    }
    this.failed(result.problem);
  }

  private async pay(): Promise<void> {
    const quote = this.quote()!;
    this.paying.set(true);
    this.alert.set(null);
    try {
      const card = this.card();
      const token = card ? await card.tokenize() : { ok: false as const, message: 'Enter your card details' };
      if (!token.ok) {
        this.alert.set({ kind: 'card', message: token.message });
        return;
      }
      this.attemptKey ??= crypto.randomUUID();
      const result = await this.grabs.start(
        {
          findReference: this.reference(),
          address: this.address(),
          depositAcknowledged: true,
          paymentMethodToken: token.token,
          expectedTotal: Number(quote.total),
        },
        this.attemptKey,
      );
      if (!result.ok) {
        if (result.problem.status !== 0) this.attemptKey = null;
        this.failed(result.problem);
        return;
      }
      this.attemptKey = null;
      await this.awaitResult(result.value.id);
    } finally {
      this.paying.set(false);
    }
  }

  /** Asks until the provider's webhook settled the payment; the browser never decides it (L2-030). */
  private async awaitResult(id: string): Promise<void> {
    for (let attempt = 0; attempt < POLL_ATTEMPTS; attempt++) {
      if (attempt > 0) await new Promise((resolve) => setTimeout(resolve, POLL_INTERVAL_MS));
      const result = await this.grabs.get(id);
      if (!result.ok) continue;
      const grab = result.value;
      if (grab.status === 'PaymentFailed') {
        this.alert.set({ kind: 'declined', reason: grab.paymentDeclineReason ?? 'Your bank declined the card.' });
        return;
      }
      if (grab.status !== 'PaymentPending') {
        this.toasts.show({
          tone: 'success',
          title: 'TuTuMuch has your grab',
          text: 'We’ll email you when it’s accepted.',
        });
        await this.router.navigate(['/account/grabs', id]);
        return;
      }
    }
    this.alert.set({ kind: 'error', ref: '' });
  }

  private failed(problem: ApiProblem): void {
    if (problem.status === 401) {
      void this.router.navigate(['/sign-in'], { queryParams: { returnUrl: `/finds/${this.reference()}/grab` } });
    } else if (problem.code === 'EMAIL_NOT_VERIFIED') {
      this.view.set('unverified');
    } else if (problem.code === 'GRAB_IN_PROGRESS') {
      this.view.set('in-progress');
    } else if (problem.code === 'NON_US_ADDRESS') {
      this.alert.set({ kind: 'non-us' });
      this.errors.set({ 'a-country': 'Choose a US address to continue.' });
    } else if (problem.code === 'DEPOSIT_NOT_ACKNOWLEDGED') {
      this.ackError.set(ACK_ERROR);
    } else if (problem.code === 'TOTAL_CHANGED') {
      this.quote.set(null);
      this.alert.set({ kind: 'total-changed' });
    } else if (problem.status === 422) {
      const errors: Record<string, string> = {};
      for (const { field, id } of ADDRESS_FIELDS) {
        const error = problem.errors[field];
        if (error) errors[id] = error.message;
      }
      this.errors.set(errors);
    } else if (
      problem.status === 404 ||
      problem.code === 'GRAB_NOT_OFFERED' ||
      problem.code === 'CANNOT_GRAB_OWN_FIND'
    ) {
      void this.router.navigateByUrl(`/finds/${this.reference()}`);
    } else {
      this.alert.set({ kind: 'error', ref: shortRef(problem.correlationId) });
    }
  }
}
