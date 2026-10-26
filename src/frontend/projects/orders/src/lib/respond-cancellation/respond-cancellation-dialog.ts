import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { OrdersApi, Sale } from '@tutumuch/api';
import {
  AlertComponent,
  ButtonDirective,
  DIALOG_DATA,
  DialogComponent,
  DialogRef,
  QuoteComponent,
} from '@tutumuch/components';

import { formatMoney } from '../money';
import { formatDay } from '../order-dates';

/** What Respond needs: the order, the buyer and their message, the total refunded and the ship-by date kept on decline. */
export interface RespondCancellationData {
  orderId: string;
  buyerDisplayName: string;
  message: string | null;
  total: string;
  shipBy: string;
}

/**
 * "{buyer} asked to cancel" (L2-035, mocks dialogs/respond-cancellation): the quoted message,
 * then "Decline" or "Accept and refund". A failure offers another try. Closes with the sale,
 * Cancelled when accepted and still Paid when declined.
 */
@Component({
  selector: 'tl-respond-cancellation-dialog',
  imports: [AlertComponent, ButtonDirective, DialogComponent, QuoteComponent],
  templateUrl: './respond-cancellation-dialog.html',
  styleUrl: './respond-cancellation-dialog.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RespondCancellationDialog {
  private readonly api = inject(OrdersApi);
  readonly ref = inject<DialogRef<Sale>>(DialogRef);
  readonly data = inject<RespondCancellationData>(DIALOG_DATA);

  readonly state = signal<'default' | 'busy' | 'failed'>('default');
  /** The decision being sent, which shows its button busy and disables the other. */
  readonly pending = signal<'Accept' | 'Decline' | null>(null);
  readonly money = formatMoney;
  readonly day = formatDay;

  async respond(decision: 'Accept' | 'Decline'): Promise<void> {
    if (this.state() === 'busy') return;
    this.state.set('busy');
    this.pending.set(decision);
    const result = await this.api.respondToCancellation(this.data.orderId, decision);
    this.pending.set(null);
    if (result.ok) {
      this.ref.close(result.value);
      return;
    }
    this.state.set('failed');
  }
}
