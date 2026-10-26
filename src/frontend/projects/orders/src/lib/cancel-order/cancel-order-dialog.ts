import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  Injector,
  afterNextRender,
  inject,
  signal,
} from '@angular/core';
import { CANCELLATION_REASONS, OrdersApi, Sale } from '@tutumuch/api';
import {
  AlertComponent,
  ButtonDirective,
  ChoiceComponent,
  DIALOG_DATA,
  DialogComponent,
  DialogRef,
  FormFieldComponent,
  RadioGroupComponent,
} from '@tutumuch/components';

import { formatMoney } from '../money';

/** What Cancel order needs: the order, and the buyer and total the description names. */
export interface CancelOrderData {
  orderId: string;
  buyerDisplayName: string;
  total: string;
}

/**
 * "Cancel this sale?" (L2-035, mocks dialogs/cancel-order): a required reason and an optional
 * note to the buyer, then "Cancel sale". No reason shows "Choose a reason" without calling the
 * API; a failure keeps the choices and offers "Try again". Closes with the cancelled sale.
 */
@Component({
  selector: 'tl-cancel-order-dialog',
  imports: [AlertComponent, ButtonDirective, ChoiceComponent, DialogComponent, FormFieldComponent, RadioGroupComponent],
  templateUrl: './cancel-order-dialog.html',
  styleUrl: './cancel-order-dialog.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CancelOrderDialog {
  private readonly api = inject(OrdersApi);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly injector = inject(Injector);
  readonly ref = inject<DialogRef<Sale>>(DialogRef);
  readonly data = inject<CancelOrderData>(DIALOG_DATA);

  readonly reasons = CANCELLATION_REASONS;
  readonly reason = signal<string | null>(null);
  readonly note = signal('');
  readonly state = signal<'default' | 'busy' | 'failed'>('default');
  readonly reasonError = signal<string | null>(null);
  readonly noteError = signal<string | null>(null);
  readonly money = formatMoney;

  choose(reason: string): void {
    this.reason.set(reason);
    this.reasonError.set(null);
  }

  async submit(): Promise<void> {
    if (this.state() === 'busy') return;
    const reason = this.reason();
    if (!reason) {
      this.reasonError.set('Choose a reason');
      this.focus('cancel-reason-0');
      return;
    }
    this.state.set('busy');
    const result = await this.api.cancelOrder(this.data.orderId, reason, this.note());
    if (result.ok) {
      this.ref.close(result.value);
      return;
    }
    const { reason: reasonError, note } = result.problem.errors;
    if (result.problem.status === 422 && (reasonError || note)) {
      this.state.set('default');
      this.reasonError.set(reasonError?.message ?? null);
      this.noteError.set(note?.message ?? null);
      this.focus(reasonError ? 'cancel-reason-0' : 'cancel-note');
      return;
    }
    this.state.set('failed');
  }

  private focus(id: string): void {
    afterNextRender(() => this.host.nativeElement.querySelector<HTMLElement>(`#${id}`)?.focus(), {
      injector: this.injector,
    });
  }
}
