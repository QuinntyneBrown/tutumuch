import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FindsApi } from '@tutumuch/api';
import { AlertComponent, ButtonDirective, DIALOG_DATA, DialogComponent, DialogRef } from '@tutumuch/components';

/**
 * "Withdraw this find?" (L2-116, mocks dialogs/withdraw-find): "Keep find" has focus; a 409
 * GRAB_IN_PROGRESS says TuTuMuch is grabbing it, any other failure offers another try. Closes with
 * true once withdrawn.
 */
@Component({
  selector: 'tl-withdraw-find-dialog',
  imports: [AlertComponent, ButtonDirective, DialogComponent],
  template: `
    <tl-dialog heading="Withdraw this find?" icon="trash" tone="danger" size="sm">
      <span description>It disappears from Shelf finds.</span>
      @if (state() === 'in-progress') {
        <tl-alert top tone="warning" heading="TuTuMuch is grabbing this one for a shopper"
          >You can withdraw it once the grab ends.</tl-alert
        >
      } @else if (state() === 'failed') {
        <tl-alert top tone="danger" heading="Couldn’t withdraw">Your find is still live.</tl-alert>
      }
      <ng-container ngProjectAs="[footer]">
        <button tlButton type="button" data-autofocus [disabled]="state() === 'busy'" (click)="ref.close()">
          Keep find
        </button>
        @if (state() !== 'in-progress') {
          <button tlButton="danger" type="button" [busy]="state() === 'busy'" (click)="submit()">
            {{ state() === 'failed' ? 'Try again' : 'Withdraw' }}
          </button>
        }
      </ng-container>
    </tl-dialog>
  `,
  styles: ':host { display: contents; }',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WithdrawFindDialog {
  private readonly api = inject(FindsApi);
  readonly ref = inject<DialogRef<boolean>>(DialogRef);
  readonly reference = inject<string>(DIALOG_DATA);

  readonly state = signal<'default' | 'busy' | 'failed' | 'in-progress'>('default');

  async submit(): Promise<void> {
    if (this.state() === 'busy') return;
    this.state.set('busy');
    const result = await this.api.withdraw(this.reference);
    if (result.ok) {
      this.ref.close(true);
      return;
    }
    this.state.set(result.problem.code === 'GRAB_IN_PROGRESS' ? 'in-progress' : 'failed');
  }
}
