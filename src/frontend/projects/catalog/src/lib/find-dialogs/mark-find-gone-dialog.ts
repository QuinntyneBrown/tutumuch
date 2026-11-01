import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FindDetail, FindsApi } from '@tutumuch/api';
import { AlertComponent, ButtonDirective, DIALOG_DATA, DialogComponent, DialogRef } from '@tutumuch/components';

/**
 * "Mark as gone?" (L2-116, mocks dialogs/mark-find-gone): "Keep it up" has focus; a 409
 * GRAB_IN_PROGRESS says TuTuMuch is grabbing it, any other failure offers another try. Closes with
 * the gone find.
 */
@Component({
  selector: 'tl-mark-find-gone-dialog',
  imports: [AlertComponent, ButtonDirective, DialogComponent],
  template: `
    <tl-dialog heading="Mark as gone?" icon="map-pin" size="sm">
      <span description>Gone from the shelf? Shoppers will see it’s gone.</span>
      @if (state() === 'in-progress') {
        <tl-alert top tone="warning" heading="TuTuMuch is grabbing this one for a shopper"
          >You can mark it once the grab ends.</tl-alert
        >
      } @else if (state() === 'failed') {
        <tl-alert top tone="danger" heading="Couldn’t mark it as gone">Your find is still live.</tl-alert>
      }
      <ng-container ngProjectAs="[footer]">
        <button tlButton type="button" data-autofocus [disabled]="state() === 'busy'" (click)="ref.close()">
          Keep it up
        </button>
        @if (state() !== 'in-progress') {
          <button tlButton="primary" type="button" [busy]="state() === 'busy'" (click)="submit()">
            {{ state() === 'failed' ? 'Try again' : 'Mark as gone' }}
          </button>
        }
      </ng-container>
    </tl-dialog>
  `,
  styles: ':host { display: contents; }',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MarkFindGoneDialog {
  private readonly api = inject(FindsApi);
  readonly ref = inject<DialogRef<FindDetail>>(DialogRef);
  readonly reference = inject<string>(DIALOG_DATA);

  readonly state = signal<'default' | 'busy' | 'failed' | 'in-progress'>('default');

  async submit(): Promise<void> {
    if (this.state() === 'busy') return;
    this.state.set('busy');
    const result = await this.api.markGone(this.reference);
    if (result.ok) {
      this.ref.close(result.value);
      return;
    }
    this.state.set(result.problem.code === 'GRAB_IN_PROGRESS' ? 'in-progress' : 'failed');
  }
}
