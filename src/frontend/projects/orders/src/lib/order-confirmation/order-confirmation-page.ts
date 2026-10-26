import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CheckoutApi, Purchase, shortRef } from '@tutumuch/api';
import { ButtonDirective, CenterStateComponent, IconComponent, ItemRowComponent } from '@tutumuch/components';

import { formatMoney } from '../money';

/**
 * The order confirmation at /orders/{id}/confirmed (mocks pages/order-confirmation): what was
 * paid, with the card brand and last four, and the next steps of the order (L2-030).
 */
@Component({
  selector: 'tl-order-confirmation-page',
  imports: [RouterLink, ButtonDirective, CenterStateComponent, IconComponent, ItemRowComponent],
  templateUrl: './order-confirmation-page.html',
  styleUrl: './order-confirmation-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrderConfirmationPage {
  private readonly api = inject(CheckoutApi);

  /** The order id, from the route parameter. */
  readonly id = input.required<string>();

  readonly order = signal<Purchase | null>(null);
  readonly view = signal<'loading' | 'loaded' | 'error'>('loading');
  readonly errorRef = signal('');

  readonly paid = computed(() => {
    const order = this.order();
    if (!order) return '';
    const card = order.cardBrand && order.cardLast4 ? ` · ${order.cardBrand} ending ${order.cardLast4}` : '';
    return `Paid ${formatMoney(order.total)}${card}`;
  });

  constructor() {
    effect(() => {
      const id = this.id();
      untracked(() => void this.load(id));
    });
  }

  retry(): Promise<void> {
    return this.load(this.id());
  }

  private async load(id: string): Promise<void> {
    this.view.set('loading');
    const result = await this.api.purchase(id);
    if (!result.ok) {
      this.errorRef.set(shortRef(result.problem.correlationId));
      this.view.set('error');
      return;
    }
    this.order.set(result.value);
    this.view.set('loaded');
  }
}
