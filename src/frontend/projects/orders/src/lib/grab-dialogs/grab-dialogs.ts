import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { AdminGrab, AdminGrabsApi, Grab, GrabUnavailableReason, GrabsApi } from '@tutumuch/api';
import {
  AlertComponent,
  ButtonDirective,
  ChoiceComponent,
  DIALOG_DATA,
  DialogComponent,
  DialogRef,
  RadioGroupComponent,
} from '@tutumuch/components';

import { formatMoney } from '../money';

type State = 'default' | 'busy' | 'failed' | 'changed';

/**
 * "Cancel this grab?" (L2-119, mocks dialogs/cancel-grab): before acceptance the shopper gets the
 * whole total back; after it, the total less the deposit, said in tone danger. "Keep grab" has
 * focus. Closes with the cancelled grab.
 */
@Component({
  selector: 'tl-cancel-grab-dialog',
  imports: [AlertComponent, ButtonDirective, DialogComponent],
  template: `
    <tl-dialog heading="Cancel this grab?" icon="alert" tone="danger" size="sm">
      <span description>
        @if (grab.status === 'Accepted') {
          You get {{ money(grab.refundIfCancelled ?? '0') }} back. TuTuMuch keeps the {{ money(grab.deposit) }} deposit.
        } @else {
          TuTuMuch hasn’t accepted yet, so you get all {{ money(grab.total) }} back.
        }
      </span>
      @if (state() === 'changed') {
        <tl-alert top tone="warning" heading="TuTuMuch already bought it">This grab can’t be cancelled now.</tl-alert>
      } @else if (state() === 'failed') {
        <tl-alert top tone="danger" heading="That didn’t go through">Your grab is unchanged.</tl-alert>
      }
      <ng-container ngProjectAs="[footer]">
        <button tlButton type="button" data-autofocus [disabled]="state() === 'busy'" (click)="ref.close()">
          Keep grab
        </button>
        @if (state() !== 'changed') {
          <button tlButton="danger" type="button" [busy]="state() === 'busy'" (click)="submit()">Cancel grab</button>
        }
      </ng-container>
    </tl-dialog>
  `,
  styles: ':host { display: contents; }',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CancelGrabDialog {
  private readonly api = inject(GrabsApi);
  readonly ref = inject<DialogRef<Grab>>(DialogRef);
  readonly grab = inject<Grab>(DIALOG_DATA);
  readonly money = formatMoney;
  readonly state = signal<State>('default');

  async submit(): Promise<void> {
    if (this.state() === 'busy') return;
    this.state.set('busy');
    const result = await this.api.cancel(this.grab.id);
    if (result.ok) {
      this.ref.close(result.value);
      return;
    }
    this.state.set(result.problem.code === 'GRAB_STATE_CHANGED' ? 'changed' : 'failed');
  }
}

/** "Got it?" (L2-119, mocks dialogs/grab-received): closes the grab. "Not yet" has focus. */
@Component({
  selector: 'tl-grab-received-dialog',
  imports: [AlertComponent, ButtonDirective, DialogComponent],
  template: `
    <tl-dialog heading="Got it?" icon="check-circle" size="sm">
      <span description>Tell TuTuMuch it arrived. This closes the grab.</span>
      @if (state() === 'failed') {
        <tl-alert top tone="danger" heading="That didn’t go through">Your grab is unchanged.</tl-alert>
      }
      <ng-container ngProjectAs="[footer]">
        <button tlButton type="button" data-autofocus [disabled]="state() === 'busy'" (click)="ref.close()">
          Not yet
        </button>
        <button tlButton="primary" type="button" [busy]="state() === 'busy'" (click)="submit()">
          Yes, I’ve got it
        </button>
      </ng-container>
    </tl-dialog>
  `,
  styles: ':host { display: contents; }',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GrabReceivedDialog {
  private readonly api = inject(GrabsApi);
  readonly ref = inject<DialogRef<Grab>>(DialogRef);
  readonly grab = inject<Grab>(DIALOG_DATA);
  readonly state = signal<State>('default');

  async submit(): Promise<void> {
    if (this.state() === 'busy') return;
    this.state.set('busy');
    const result = await this.api.received(this.grab.id);
    if (result.ok) {
      this.ref.close(result.value);
      return;
    }
    this.state.set('failed');
  }
}

/** "Decline grab {reference}?" (L2-118, mocks dialogs/decline-grab): the shopper is refunded in full. */
@Component({
  selector: 'tl-decline-grab-dialog',
  imports: [AlertComponent, ButtonDirective, DialogComponent],
  template: `
    <tl-dialog [heading]="'Decline grab ' + grab.reference + '?'" icon="alert" tone="danger" size="sm">
      <span description>{{ grab.shopperDisplayName }} is refunded {{ money(grab.total) }} in full and emailed.</span>
      @if (state() === 'changed') {
        <tl-alert top tone="warning" heading="This grab has changed">Refresh to see where it is.</tl-alert>
      } @else if (state() === 'failed') {
        <tl-alert top tone="danger" heading="That didn’t go through">The grab is unchanged.</tl-alert>
      }
      <ng-container ngProjectAs="[footer]">
        <button tlButton type="button" data-autofocus [disabled]="state() === 'busy'" (click)="ref.close()">
          Keep grab
        </button>
        @if (state() !== 'changed') {
          <button tlButton="danger" type="button" [busy]="state() === 'busy'" (click)="submit()">
            Decline and refund
          </button>
        }
      </ng-container>
    </tl-dialog>
  `,
  styles: ':host { display: contents; }',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DeclineGrabDialog {
  private readonly api = inject(AdminGrabsApi);
  readonly ref = inject<DialogRef<AdminGrab>>(DialogRef);
  readonly grab = inject<AdminGrab>(DIALOG_DATA);
  readonly money = formatMoney;
  readonly state = signal<State>('default');

  async submit(): Promise<void> {
    if (this.state() === 'busy') return;
    this.state.set('busy');
    const result = await this.api.act(this.grab.id, 'decline');
    if (result.ok) {
      this.ref.close(result.value);
      return;
    }
    this.state.set(result.problem.code === 'GRAB_STATE_CHANGED' ? 'changed' : 'failed');
  }
}

/**
 * "Couldn’t get it?" (L2-118, mocks dialogs/grab-unavailable): the reason, then a full refund and
 * the find marked gone.
 */
@Component({
  selector: 'tl-grab-unavailable-dialog',
  imports: [AlertComponent, ButtonDirective, ChoiceComponent, DialogComponent, RadioGroupComponent],
  template: `
    <tl-dialog heading="Couldn’t get it?" icon="map-pin">
      <span description
        >{{ grab.shopperDisplayName }} is refunded {{ money(grab.total) }} in full and the find is marked gone.</span
      >
      @if (state() === 'failed') {
        <tl-alert top tone="danger" heading="That didn’t go through">The grab is unchanged.</tl-alert>
      } @else if (state() === 'changed') {
        <tl-alert top tone="warning" heading="This grab has changed">Refresh to see where it is.</tl-alert>
      }
      <tl-radio-group legend="Why?" [error]="reasonError()">
        @for (option of reasons; track option.value; let i = $index) {
          <tl-choice [inputId]="'unavailable-' + i">
            <input
              type="radio"
              name="unavailable-reason"
              [id]="'unavailable-' + i"
              [attr.data-autofocus]="i === 0 ? '' : null"
              [checked]="reason() === option.value"
              [attr.aria-invalid]="reasonError() ? 'true' : null"
              (change)="choose(option.value)"
            />{{ option.label }}</tl-choice
          >
        }
      </tl-radio-group>
      <ng-container ngProjectAs="[footer]">
        <button tlButton type="button" [disabled]="state() === 'busy'" (click)="ref.close()">Cancel</button>
        @if (state() !== 'changed') {
          <button tlButton="danger" type="button" [busy]="state() === 'busy'" (click)="submit()">Refund in full</button>
        }
      </ng-container>
    </tl-dialog>
  `,
  styles: ':host { display: contents; }',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GrabUnavailableDialog {
  private readonly api = inject(AdminGrabsApi);
  readonly ref = inject<DialogRef<AdminGrab>>(DialogRef);
  readonly grab = inject<AdminGrab>(DIALOG_DATA);
  readonly money = formatMoney;
  readonly reasons: readonly { value: GrabUnavailableReason; label: string }[] = [
    { value: 'Gone', label: 'Gone from the shelf' },
    { value: 'PriceWentUp', label: 'Price went up' },
  ];
  readonly reason = signal<GrabUnavailableReason | null>(null);
  readonly reasonError = signal<string | null>(null);
  readonly state = signal<State>('default');

  choose(reason: GrabUnavailableReason): void {
    this.reason.set(reason);
    this.reasonError.set(null);
  }

  async submit(): Promise<void> {
    if (this.state() === 'busy') return;
    const reason = this.reason();
    if (!reason) {
      this.reasonError.set('Choose a reason');
      return;
    }
    this.state.set('busy');
    const result = await this.api.unavailable(this.grab.id, reason);
    if (result.ok) {
      this.ref.close(result.value);
      return;
    }
    this.state.set(result.problem.code === 'GRAB_STATE_CHANGED' ? 'changed' : 'failed');
  }
}
