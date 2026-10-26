import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { OrderList, OrdersApi, shortRef } from '@tutumuch/api';
import {
  ButtonDirective,
  EmptyStateComponent,
  IconComponent,
  ItemRowComponent,
  StatusComponent,
} from '@tutumuch/components';

import { formatMoney } from '../money';
import { OrderHistoryList, orderMeta, orderSubtitle, statusTone } from './order-history-text';

/**
 * The member's "Purchases" at /account/purchases and "Sales" at /account/sales (mocks
 * pages/purchases, pages/sales, L2-037): their orders newest first, each linking to the buyer's
 * order or the seller's sale. The route's `list` data picks the list.
 */
@Component({
  selector: 'tl-order-history-page',
  imports: [RouterLink, ButtonDirective, EmptyStateComponent, IconComponent, ItemRowComponent, StatusComponent],
  templateUrl: './order-history-page.html',
  styleUrl: './order-history-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrderHistoryPage {
  private readonly api = inject(OrdersApi);

  readonly list: OrderHistoryList = inject(ActivatedRoute).snapshot.data['list'];
  readonly orders = signal<OrderList | null>(null);
  readonly view = signal<'loading' | 'loaded' | 'error'>('loading');
  readonly errorRef = signal('');

  readonly heading = this.list === 'purchases' ? 'Purchases' : 'Sales';
  readonly detailPath = this.list === 'purchases' ? '/orders' : '/sales';
  readonly subtitle = computed(() => {
    const orders = this.orders();
    return orders && orders.items.length > 0 ? orderSubtitle(this.list, orders.counts) : '';
  });

  readonly money = formatMoney;
  readonly tone = statusTone;

  constructor() {
    void this.load();
  }

  meta = (row: OrderList['items'][number]): string => orderMeta(this.list, row);

  retry(): Promise<void> {
    return this.load();
  }

  private async load(): Promise<void> {
    this.view.set('loading');
    const result = await (this.list === 'purchases' ? this.api.purchases() : this.api.sales());
    if (!result.ok) {
      this.errorRef.set(shortRef(result.problem.correlationId));
      this.view.set('error');
      return;
    }
    this.orders.set(result.value);
    this.view.set('loaded');
  }
}
