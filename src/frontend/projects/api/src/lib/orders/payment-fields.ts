import { InjectionToken } from '@angular/core';

/** The provider's answer to tokenize: a payment method token, or why the card can't be used yet. */
export type TokenizeResult = { ok: true; token: string } | { ok: false; message: string };

/**
 * The payment provider's hosted card fields (L2-030). Card numbers and security codes are typed
 * into the provider's frame and go to the provider only; TuTuMuch receives the payment method token.
 */
export interface PaymentFields {
  /** Puts the card fields inside host. */
  mount(host: HTMLElement): void;
  tokenize(): Promise<TokenizeResult>;
  unmount(): void;
}

/** One test card of the test-mode fields and the token the provider's test mode returns for it. */
export const TEST_CARDS = [
  { label: 'Visa ending 4242 (test card)', token: 'tok_visa' },
  { label: 'Card that is declined (test card)', token: 'tok_chargeDeclined' },
] as const;

/**
 * Test-mode stand-in for the provider's hosted fields until a provider is chosen (ADR
 * orders/0001). It offers test cards to pick from, so no card number is ever typed into TuTuMuch.
 */
export class TestModePaymentFields implements PaymentFields {
  private select: HTMLSelectElement | null = null;

  mount(host: HTMLElement): void {
    const select = host.ownerDocument.createElement('select');
    select.className = 'select';
    select.setAttribute('aria-label', 'Test card');
    for (const card of TEST_CARDS) {
      const option = host.ownerDocument.createElement('option');
      option.value = card.token;
      option.textContent = card.label;
      select.append(option);
    }
    host.replaceChildren(select);
    this.select = select;
  }

  tokenize(): Promise<TokenizeResult> {
    return Promise.resolve(
      this.select ? { ok: true, token: this.select.value } : { ok: false, message: 'Enter your card details' },
    );
  }

  unmount(): void {
    this.select?.remove();
    this.select = null;
  }
}

/** Creates the card fields for one checkout page. Replace it with the provider's SDK adapter. */
export const PAYMENT_FIELDS = new InjectionToken<() => PaymentFields>('PAYMENT_FIELDS', {
  providedIn: 'root',
  factory: () => () => new TestModePaymentFields(),
});
