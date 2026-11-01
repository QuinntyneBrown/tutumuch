import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { IconComponent, type IconName } from '../icon/icon';
import { ItemRowComponent } from '../item-row/item-row';

/** The item of an order summary: title, meta line and an optional thumbnail (WebP and JPEG srcsets). */
export interface OrderSummaryItem {
  title: string;
  meta: string;
  photo: { src: string; webp: string; jpeg: string } | null;
  /** Drawn in the thumbnail well when there is no photo, as for a shelf find (L2-117). */
  icon?: IconName;
}

/**
 * The summary panel of a checkout or an order (design system: Order summary): the item row and
 * the Item, Shipping, Sales tax and Total lines. Values are formatted by the caller; a null tax
 * or total reads as not yet known. An empty heading labels the panel "Order summary" without a
 * visible title, as on the order pages, which project the other party below the totals.
 */
@Component({
  selector: 'tl-order-summary',
  imports: [IconComponent, ItemRowComponent],
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
  /** The first two lines: "Item" and "Shipping", or "Shelf price" and "Grab fee" for a grab (L2-117). */
  readonly itemLabel = input('Item');
  readonly shippingLabel = input('Shipping');
  readonly totalLabel = input('Total');
}
