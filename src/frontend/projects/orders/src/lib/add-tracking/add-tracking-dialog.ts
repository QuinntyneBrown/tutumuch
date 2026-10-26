import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  Injector,
  afterNextRender,
  inject,
  signal,
} from '@angular/core';
import { CARRIERS, OrdersApi, Sale } from '@tutumuch/api';
import {
  AlertComponent,
  ButtonDirective,
  DIALOG_DATA,
  DialogComponent,
  DialogRef,
  FormFieldComponent,
} from '@tutumuch/components';

/** What Add tracking needs: the order and the buyer it emails. */
export interface AddTrackingData {
  orderId: string;
  buyerDisplayName: string;
}

/**
 * "Add tracking" (L2-033, mocks dialogs/add-tracking): carrier and tracking number, then "Mark as
 * shipped". A malformed number is shown under the field with the input kept; a failure keeps the
 * inputs and offers "Try again". Closes with the shipped sale.
 */
@Component({
  selector: 'tm-add-tracking-dialog',
  imports: [AlertComponent, ButtonDirective, DialogComponent, FormFieldComponent],
  templateUrl: './add-tracking-dialog.html',
  styleUrl: './add-tracking-dialog.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AddTrackingDialog {
  private readonly api = inject(OrdersApi);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly injector = inject(Injector);
  readonly ref = inject<DialogRef<Sale>>(DialogRef);
  readonly data = inject<AddTrackingData>(DIALOG_DATA);

  readonly carriers = CARRIERS;
  readonly carrier = signal<string>(CARRIERS[0]);
  readonly trackingNumber = signal('');
  readonly state = signal<'default' | 'busy' | 'failed'>('default');
  readonly errors = signal<{ carrier?: string; trackingNumber?: string }>({});

  async submit(): Promise<void> {
    if (this.state() === 'busy') return;
    if (!this.trackingNumber().trim()) {
      this.showErrors({ trackingNumber: 'Enter the tracking number.' });
      return;
    }
    this.state.set('busy');
    const result = await this.api.addTracking(this.data.orderId, this.carrier(), this.trackingNumber());
    if (result.ok) {
      this.ref.close(result.value);
      return;
    }
    const { carrier, trackingNumber } = result.problem.errors;
    if (result.problem.status === 422 && (carrier || trackingNumber)) {
      this.state.set('default');
      this.showErrors({ carrier: carrier?.message, trackingNumber: trackingNumber?.message });
      return;
    }
    this.errors.set({});
    this.state.set('failed');
  }

  private showErrors(errors: { carrier?: string; trackingNumber?: string }): void {
    this.errors.set(errors);
    const id = errors.carrier ? 'track-carrier' : 'track-number';
    afterNextRender(() => this.host.nativeElement.querySelector<HTMLElement>(`#${id}`)?.focus(), {
      injector: this.injector,
    });
  }
}
