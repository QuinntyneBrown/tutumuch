import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ApiProblem, Checkout, CheckoutApi, ShippingAddress, TokenizeResult, shortRef } from '@tutumuch/api';

import { formatMoney } from '../money';

export type CheckoutView = 'loading' | 'ready' | 'conflict' | 'expired' | 'error';

/** The alert at the top of the checkout form. */
export type CheckoutAlert =
  | { kind: 'non-us' }
  | { kind: 'declined'; reason: string }
  | { kind: 'total-changed' }
  | { kind: 'card'; message: string }
  | { kind: 'error'; ref: string };

/** Address fields as the API names them, mapped to the form's control ids, in summary order. */
export const ADDRESS_FIELDS: readonly { field: keyof ShippingAddress; id: string }[] = [
  { field: 'fullName', id: 'a-name' },
  { field: 'line1', id: 'a-street' },
  { field: 'line2', id: 'a-apt' },
  { field: 'city', id: 'a-city' },
  { field: 'state', id: 'a-state' },
  { field: 'zip', id: 'a-zip' },
  { field: 'country', id: 'a-country' },
];

/** How often and how long the page asks whether the webhook settled the payment. */
const POLL_INTERVAL_MS = 1000;
const POLL_ATTEMPTS = 60;

const sameAddress = (a: ShippingAddress | null, b: ShippingAddress | null): boolean =>
  !!a && !!b && ADDRESS_FIELDS.every(({ field }) => (a[field] ?? '') === (b[field] ?? ''));

/**
 * Signal state of one checkout (checkout-and-pay). Starting reserves the listing; a 409 is the
 * conflict view and any other failure the error view. Paying sets the address first when it
 * changed, stops if that changed the total the buyer saw (L2-027 AC5), then sends the provider's
 * token with one idempotency key per attempt (L2-030 AC6) and polls until the webhook settled it.
 */
@Injectable()
export class CheckoutStore {
  private readonly api = inject(CheckoutApi);
  private readonly router = inject(Router);
  private reference = '';
  /** The key of the current payment attempt; kept when the request may not have arrived. */
  private attemptKey: string | null = null;

  readonly checkout = signal<Checkout | null>(null);
  readonly view = signal<CheckoutView>('loading');
  readonly errorRef = signal('');
  readonly addressChoice = signal<'saved' | 'new'>('saved');
  /** Field errors keyed by control id. */
  readonly fieldErrors = signal<Record<string, string>>({});
  readonly alert = signal<CheckoutAlert | null>(null);
  readonly paying = signal(false);
  /** The countdown reached zero: Pay is disabled. */
  readonly timedOut = signal(false);

  readonly savedAddress = computed(() => this.checkout()?.savedAddress ?? null);
  readonly payLabel = computed(() => {
    const total = this.checkout()?.total;
    return total ? `Pay ${formatMoney(total)}` : 'Continue';
  });

  async start(reference: string): Promise<void> {
    this.reference = reference;
    this.view.set('loading');
    const result = await this.api.start(reference);
    if (!result.ok) {
      await this.startFailed(result.problem);
      return;
    }
    const saved = result.value.savedAddress;
    this.checkout.set(result.value);
    this.addressChoice.set(saved ? 'saved' : 'new');
    this.view.set('ready');
    if (saved && !sameAddress(result.value.address, saved)) {
      const priced = await this.api.setAddress(result.value.id, saved);
      if (priced.ok) this.checkout.set(priced.value);
    }
  }

  retry(): Promise<void> {
    return this.start(this.reference);
  }

  /** Pay, or Continue when no total is known yet. newAddress is the form's value. */
  async submit(newAddress: ShippingAddress, tokenize: () => Promise<TokenizeResult>): Promise<void> {
    const current = this.checkout();
    if (!current || this.paying() || this.timedOut()) return;
    this.paying.set(true);
    this.alert.set(null);
    this.fieldErrors.set({});
    try {
      const target = this.addressChoice() === 'saved' ? this.savedAddress() : newAddress;
      if (!target) return;
      if (!sameAddress(current.address, target)) {
        const priced = await this.api.setAddress(current.id, target);
        if (!priced.ok) {
          this.addressFailed(priced.problem);
          return;
        }
        this.checkout.set(priced.value);
        if (current.total !== priced.value.total) {
          // The buyer pays only a total they have seen (L2-027 AC5).
          if (current.total) this.alert.set({ kind: 'total-changed' });
          return;
        }
      }
      await this.pay(tokenize);
    } finally {
      this.paying.set(false);
    }
  }

  /** "Cancel and release item": frees the item and goes back to it (L2-028 AC4). */
  async cancel(): Promise<void> {
    const current = this.checkout();
    if (current) await this.api.release(current.id);
    await this.router.navigateByUrl(`/items/${this.reference}`);
  }

  private async pay(tokenize: () => Promise<TokenizeResult>): Promise<void> {
    const current = this.checkout()!;
    const card = await tokenize();
    if (!card.ok) {
      this.alert.set({ kind: 'card', message: card.message });
      return;
    }
    this.attemptKey ??= crypto.randomUUID();
    const result = await this.api.pay(current.id, this.attemptKey, card.token, current.total!);
    if (!result.ok) {
      if (result.problem.status !== 0) this.attemptKey = null;
      this.payFailed(result.problem);
      return;
    }
    this.attemptKey = null;
    await this.awaitResult(current.id);
  }

  /** Asks until the provider's webhook settled the payment; the browser never decides it (L2-030). */
  private async awaitResult(id: string): Promise<void> {
    for (let attempt = 0; attempt < POLL_ATTEMPTS; attempt++) {
      if (attempt > 0) await new Promise((resolve) => setTimeout(resolve, POLL_INTERVAL_MS));
      const result = await this.api.get(id);
      if (!result.ok) continue;
      const checkout = result.value;
      if (checkout.status === 'Paid' && checkout.orderId) {
        await this.router.navigate(['/orders', checkout.orderId, 'confirmed']);
        return;
      }
      if (checkout.status === 'Expired' || checkout.status === 'Released') {
        this.view.set('expired');
        return;
      }
      if (checkout.status === 'Open') {
        this.checkout.set(checkout);
        this.alert.set({ kind: 'declined', reason: checkout.lastDeclineReason ?? 'Your bank declined the card.' });
        return;
      }
    }
    this.alert.set({ kind: 'error', ref: '' });
  }

  private async startFailed(problem: ApiProblem): Promise<void> {
    if (problem.status === 401) {
      await this.router.navigate(['/sign-in'], { queryParams: { returnUrl: `/checkout/${this.reference}` } });
    } else if (problem.code === 'LISTING_RESERVED') {
      this.view.set('conflict');
    } else if (
      problem.status === 404 ||
      problem.code === 'CANNOT_BUY_OWN_LISTING' ||
      problem.code === 'LISTING_UNAVAILABLE'
    ) {
      await this.router.navigateByUrl(`/items/${this.reference}`);
    } else {
      this.errorRef.set(shortRef(problem.correlationId));
      this.view.set('error');
    }
  }

  private addressFailed(problem: ApiProblem): void {
    if (problem.code === 'NON_US_ADDRESS') {
      this.alert.set({ kind: 'non-us' });
      this.fieldErrors.set({ 'a-country': 'Choose a US address to continue.' });
    } else if (problem.status === 422) {
      const errors: Record<string, string> = {};
      for (const { field, id } of ADDRESS_FIELDS) {
        const error = problem.errors[field];
        if (error) errors[id] = error.message;
      }
      this.fieldErrors.set(errors);
    } else {
      this.payFailed(problem);
    }
  }

  private payFailed(problem: ApiProblem): void {
    if (problem.code === 'RESERVATION_EXPIRED') {
      this.view.set('expired');
    } else if (problem.code === 'TOTAL_CHANGED') {
      this.alert.set({ kind: 'total-changed' });
      void this.api.get(this.checkout()!.id).then((r) => r.ok && this.checkout.set(r.value));
    } else {
      this.alert.set({ kind: 'error', ref: shortRef(problem.correlationId) });
    }
  }
}
