import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthState, FindBody, FindsApi, shortRef } from '@tutumuch/api';
import {
  AlertComponent,
  ButtonDirective,
  CenterStateComponent,
  DialogService,
  ErrorSummaryComponent,
  FixedSizeComponent,
  FormFieldComponent,
  InputGroupComponent,
  SwitchComponent,
  ToastService,
  type SummaryError,
} from '@tutumuch/components';

import { VerifyEmailPromptComponent } from '../verify-email-prompt/verify-email-prompt';

/** The six categories of L2-011 in the guideline order; every find is for girls (L1-003). */
export const FIND_CATEGORIES = ['Dance', 'Sports', 'Clothing', 'Shoes', 'Cameras and tech', 'Toys and crafts'] as const;

/** The field id on the form of each find field TuTuMuch API names in a 422. */
const FIELD_IDS: Record<string, string> = {
  title: 'title',
  category: 'cat',
  shelfPrice: 'price',
  storeName: 'store',
  storeAddress: 'address',
  seenOn: 'seen',
  note: 'note',
  grabFee: 'fee',
};
const ORDER = ['title', 'cat', 'price', 'store', 'address', 'seen', 'note', 'fee'];

type View = 'loading' | 'form' | 'limited' | 'unverified';

/** Today in UTC as "YYYY-MM-DD", the API's day (L2-113). */
function utcDay(offsetDays = 0): string {
  const now = new Date();
  return new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate() + offsetDays))
    .toISOString()
    .slice(0, 10);
}

/** "40", "$40.00" or "1,200.50" as a number; null when blank; NaN when not an amount. */
function amount(value: string): number | null {
  const text = value.trim().replace(/^\$/, '').replace(/,/g, '');
  if (!text) return null;
  return /^\d+(\.\d+)?$/.test(text) ? Number(text) : Number.NaN;
}

/**
 * Post a shelf find at /finds/new (L2-113, mocks pages/post-find): the item, where it was seen and,
 * for an administrator, "Offer to grab it" with the grab fee (ADR catalog/0009). The route matches
 * only when the launch phase lets the member sell; a visitor is sent to sign in and back.
 */
@Component({
  selector: 'tl-post-find-page',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    AlertComponent,
    ButtonDirective,
    CenterStateComponent,
    ErrorSummaryComponent,
    FixedSizeComponent,
    FormFieldComponent,
    InputGroupComponent,
    SwitchComponent,
  ],
  templateUrl: './post-find-page.html',
  styleUrl: './post-find-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PostFindPage implements OnInit {
  private readonly api = inject(FindsApi);
  private readonly auth = inject(AuthState);
  private readonly router = inject(Router);
  private readonly toasts = inject(ToastService);
  private readonly dialogs = inject(DialogService);

  readonly categories = FIND_CATEGORIES;
  readonly today = utcDay();
  readonly earliest = utcDay(-7);

  readonly form = inject(NonNullableFormBuilder).group({
    title: '',
    category: '',
    shelfPrice: '',
    storeName: '',
    storeAddress: '',
    seenOn: utcDay(),
    note: '',
    grabFee: '',
  });
  private readonly value = toSignal(this.form.valueChanges, { initialValue: this.form.getRawValue() });

  readonly view = signal<View>('loading');
  readonly busy = signal(false);
  readonly offerGrab = signal(false);
  readonly errors = signal<Record<string, string>>({});
  readonly summary = signal<SummaryError[]>([]);
  readonly failure = signal('');
  readonly isAdministrator = computed(() => this.auth.member()?.isAdministrator === true);
  readonly titleCount = computed(() => `${(this.value().title ?? '').length} / 80`);
  readonly noteCount = computed(() => `${(this.value().note ?? '').length} / 500`);

  async ngOnInit(): Promise<void> {
    await this.auth.load();
    const member = this.auth.member();
    if (!member) {
      await this.router.navigate(['/sign-in'], { queryParams: { returnUrl: '/finds/new' }, replaceUrl: true });
      return;
    }
    this.view.set(member.emailVerified ? 'form' : 'unverified');
  }

  resend(): void {
    this.dialogs.open(VerifyEmailPromptComponent, { email: this.auth.member()?.email ?? '' });
  }

  async post(): Promise<void> {
    if (this.busy()) return;
    const raw = this.form.getRawValue();
    const shelfPrice = amount(raw.shelfPrice);
    const grabFee = this.offerGrab() ? amount(raw.grabFee) : null;
    const local: Record<string, string> = {};
    if (Number.isNaN(shelfPrice)) local['price'] = 'Enter a price from $1.00 to $5,000.00';
    if (Number.isNaN(grabFee) || (this.offerGrab() && grabFee === null))
      local['fee'] = 'Enter a grab fee from $1.00 to $50.00';
    if (Object.keys(local).length) {
      this.showErrors(local);
      return;
    }
    const body: FindBody = {
      title: raw.title,
      category: raw.category,
      shelfPrice,
      storeName: raw.storeName,
      storeAddress: raw.storeAddress,
      seenOn: raw.seenOn,
      note: raw.note.trim() ? raw.note : null,
      grabFee,
    };
    this.busy.set(true);
    this.failure.set('');
    const result = await this.api.post(body);
    this.busy.set(false);
    if (result.ok) {
      this.toasts.show({ tone: 'success', title: 'Your find is live', text: 'Shoppers can see it on Shelf finds.' });
      await this.router.navigate(['/finds', result.value.reference]);
      return;
    }
    const problem = result.problem;
    if (problem.status === 401) {
      await this.router.navigate(['/sign-in'], { queryParams: { returnUrl: '/finds/new' } });
    } else if (problem.code === 'EMAIL_NOT_VERIFIED') {
      this.view.set('unverified');
    } else if (problem.status === 429) {
      this.view.set('limited');
    } else if (problem.status === 422 && Object.keys(problem.errors).length) {
      const errors: Record<string, string> = {};
      for (const [field, error] of Object.entries(problem.errors)) {
        errors[FIELD_IDS[field] ?? field] ??= error.message;
      }
      this.showErrors(errors);
    } else if (problem.status === 422 || problem.status === 403) {
      this.failure.set(problem.title);
    } else {
      this.failure.set(shortRef(problem.correlationId));
    }
  }

  private showErrors(errors: Record<string, string>): void {
    this.errors.set(errors);
    this.summary.set(
      Object.entries(errors)
        .sort(([a], [b]) => ORDER.indexOf(a) - ORDER.indexOf(b))
        .map(([fieldId, message]) => ({ fieldId, message })),
    );
  }
}
