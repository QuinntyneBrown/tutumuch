import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CheckoutItem, OrdersApi, Purchase } from '@tutumuch/api';
import {
  AlertComponent,
  ButtonDirective,
  DIALOG_DATA,
  DialogComponent,
  DialogRef,
  ItemRowComponent,
} from '@tutumuch/components';

/** What Confirm receipt needs: the order, its item and the seller it pays. */
export interface ConfirmReceiptData {
  orderId: string;
  item: CheckoutItem;
  sellerDisplayName: string;
}

/**
 * "Got it and all good?" (L2-034, mocks dialogs/confirm-receipt): the item, then "Yes, complete
 * order". A failure offers "Try again". Closes with the completed order.
 */
@Component({
  selector: 'tm-confirm-receipt-dialog',
  imports: [AlertComponent, ButtonDirective, DialogComponent, ItemRowComponent],
  templateUrl: './confirm-receipt-dialog.html',
  styleUrl: './confirm-receipt-dialog.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConfirmReceiptDialog {
  private readonly api = inject(OrdersApi);
  readonly ref = inject<DialogRef<Purchase>>(DialogRef);
  readonly data = inject<ConfirmReceiptData>(DIALOG_DATA);

  readonly state = signal<'default' | 'busy' | 'failed'>('default');

  async submit(): Promise<void> {
    if (this.state() === 'busy') return;
    this.state.set('busy');
    const result = await this.api.confirmReceipt(this.data.orderId);
    if (result.ok) {
      this.ref.close(result.value);
      return;
    }
    this.state.set('failed');
  }
}
