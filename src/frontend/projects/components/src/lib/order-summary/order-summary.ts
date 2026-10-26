import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { ItemRowComponent } from '../item-row/item-row';

/** The item of an order summary: title, meta line and an optional thumbnail (WebP and JPEG srcsets). */
export interface OrderSummaryItem {
  title: string;
  meta: string;
  photo: { src: string; webp: string; jpeg: string } | null;
}

/**
 * The summary panel of a checkout or an order (design system: Order summary): the item row and
 * the Item, Shipping, Sales tax and Total lines. Values are formatted by the caller; a null tax
 * or total reads as not yet known. An empty heading labels the panel "Order summary" without a
 * visible title, as on the order pages, which project the other party below the totals.
 */
@Component({
  selector: 'tl-order-summary',
  imports: [ItemRowComponent],
  templateUrl: './order-summary.html',
  styleUrl: './order-summary.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrderSummaryComponent {
  readonly item = input.required<OrderSummaryItem>();
  readonly itemPrice = input.required<string>();
  readonly shipping = input.required<string>();
  readonly salesTax = input<string | null>(null);
  readonly total = input<string | null>(null);
  readonly heading = input('Summary');
}
