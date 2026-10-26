import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CheckoutItem, OrdersApi } from '@tutumuch/api';
import {
  AlertComponent,
  ButtonDirective,
  DIALOG_DATA,
  DialogComponent,
  DialogRef,
  ItemRowComponent,
} from '@tutumuch/components';

import { formatMoney } from '../money';

/** What Relist needs: the cancelled order's item with its price and shipping. */
export interface RelistData {
  item: CheckoutItem;
  itemPrice: string;
  shippingCost: string;
}

/**
 * "Relist this item?" (L2-035 AC4, mocks dialogs/relist): the item with its price and shipping,
 * then "Relist", which returns the listing to the catalog unchanged, or "Edit first", which
 * opens the edit page (edit-and-withdraw-listing). A failure offers another try. Closes with true
 * once relisted.
 */
@Component({
  selector: 'tm-relist-dialog',
  imports: [RouterLink, AlertComponent, ButtonDirective, DialogComponent, ItemRowComponent],
  templateUrl: './relist-dialog.html',
  styleUrl: './relist-dialog.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RelistDialog {
  private readonly api = inject(OrdersApi);
  readonly ref = inject<DialogRef<boolean>>(DialogRef);
  readonly data = inject<RelistData>(DIALOG_DATA);

  readonly state = signal<'default' | 'busy' | 'failed'>('default');
  readonly meta = `${formatMoney(this.data.itemPrice)} · ${formatMoney(this.data.shippingCost)} shipping`;

  async submit(): Promise<void> {
    if (this.state() === 'busy') return;
    this.state.set('busy');
    const result = await this.api.relist(this.data.item.reference);
    if (result.ok) {
      this.ref.close(true);
      return;
    }
    this.state.set('failed');
  }
}
