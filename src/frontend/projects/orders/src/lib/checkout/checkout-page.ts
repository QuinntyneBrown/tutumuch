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
import { RouterLink } from '@angular/router';
import { ShippingAddress } from '@tutumuch/api';
import {
  AlertComponent,
  ButtonDirective,
  CenterStateComponent,
  ErrorSummaryComponent,
  FormFieldComponent,
  IconComponent,
  OrderSummaryComponent,
  OrderSummaryItem,
  ReservationPhase,
  ReservationTimerComponent,
  SummaryError,
} from '@tutumuch/components';

import { formatMoney } from '../money';
import { ADDRESS_FIELDS, CheckoutStore } from './checkout-store';
import { HostedCardFieldsComponent } from './hosted-card-fields';
import { COUNTRIES, US_STATES } from './us-states';

/**
 * Checkout at /checkout/{reference} (mocks pages/checkout, notifications/reservation-timer):
 * the reservation countdown, Ship to with the saved address or a new US address, Pay with the
 * provider's hosted card fields, and the order summary. Two columns from 992 px (L2-063).
 */
@Component({
  selector: 'tm-checkout-page',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    AlertComponent,
    ButtonDirective,
    CenterStateComponent,
    ErrorSummaryComponent,
    FormFieldComponent,
    IconComponent,
    OrderSummaryComponent,
    ReservationTimerComponent,
    HostedCardFieldsComponent,
  ],
  providers: [CheckoutStore],
  templateUrl: './checkout-page.html',
  styleUrl: './checkout-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CheckoutPage {
  readonly store = inject(CheckoutStore);

  /** The listing reference, from the route parameter. */
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

  private readonly card = viewChild(HostedCardFieldsComponent);

  /** The countdown's phase; warning and danger show the banner above the form. */
  readonly phase = signal<ReservationPhase>('info');

  readonly errors = this.store.fieldErrors;
  /** The non-US refusal is an alert of its own, not a summary (mocks pages/checkout non-us). */
  readonly summary = computed<SummaryError[]>(() =>
    this.store.alert()?.kind === 'non-us'
      ? []
      : ADDRESS_FIELDS.filter(({ id }) => this.errors()[id]).map(({ id }) => ({
          fieldId: id,
          message: this.errors()[id],
        })),
  );
  readonly summaryItem = computed<OrderSummaryItem | null>(() => {
    const checkout = this.store.checkout();
    if (!checkout) return null;
    const { item } = checkout;
    return { title: item.title, meta: `${item.brand} · ${item.size} · ${item.condition}`, photo: item.photo };
  });
  readonly shipping = computed(() => {
    const cost = this.store.checkout()?.shippingCost ?? '0.00';
    return cost === '0.00' ? 'Free' : formatMoney(cost);
  });

  constructor() {
    effect(() => {
      const reference = this.reference();
      untracked(() => void this.store.start(reference));
    });
    effect(() => {
      const saved = this.store.savedAddress();
      if (saved && !this.form.dirty) this.form.patchValue({ fullName: saved.fullName });
    });
  }

  onPhase(phase: ReservationPhase): void {
    this.phase.set(phase);
    if (phase === 'danger') this.store.timedOut.set(true);
  }

  chooseAddress(choice: 'saved' | 'new'): void {
    this.store.addressChoice.set(choice);
  }

  submit(): void {
    const value = this.form.getRawValue();
    const address: ShippingAddress = { ...value, line2: value.line2.trim() || null };
    const card = this.card();
    void this.store.submit(address, () =>
      card ? card.tokenize() : Promise.resolve({ ok: false as const, message: 'Enter your card details' }),
    );
  }
}
