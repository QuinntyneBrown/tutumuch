import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { OrdersApi, Purchase } from '@tutumuch/api';
import {
  AlertComponent,
  ButtonDirective,
  DIALOG_DATA,
  DialogComponent,
  DialogRef,
  FormFieldComponent,
} from '@tutumuch/components';

/** What Ask to cancel needs: the order and the seller it asks. */
export interface RequestCancellationData {
  orderId: string;
  sellerDisplayName: string;
}

/**
 * "Ask to cancel?" (L2-035, mocks dialogs/request-cancellation): an optional message to the
 * seller, then "Send request". A failure keeps the message and offers "Try again". Closes with
 * the order and its pending request.
 */
@Component({
  selector: 'tm-request-cancellation-dialog',
  imports: [AlertComponent, ButtonDirective, DialogComponent, FormFieldComponent],
  templateUrl: './request-cancellation-dialog.html',
  styleUrl: './request-cancellation-dialog.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RequestCancellationDialog {
  private readonly api = inject(OrdersApi);
  readonly ref = inject<DialogRef<Purchase>>(DialogRef);
  readonly data = inject<RequestCancellationData>(DIALOG_DATA);

  readonly message = signal('');
  readonly state = signal<'default' | 'busy' | 'failed'>('default');
  readonly messageError = signal<string | null>(null);

  async submit(): Promise<void> {
    if (this.state() === 'busy') return;
    this.state.set('busy');
    const result = await this.api.requestCancellation(this.data.orderId, this.message());
    if (result.ok) {
      this.ref.close(result.value);
      return;
    }
    const field = result.problem.errors['message'];
    this.messageError.set(field?.message ?? null);
    this.state.set(field ? 'default' : 'failed');
  }
}
