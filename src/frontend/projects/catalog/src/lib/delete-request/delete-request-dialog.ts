import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ItemRequestsApi } from '@tutumuch/api';
import { AlertComponent, ButtonDirective, DIALOG_DATA, DialogComponent, DialogRef } from '@tutumuch/components';

/** What Delete needs: the request's ID and keywords. */
export interface DeleteRequestData {
  id: string;
  keywords: string;
}

/**
 * "Delete this request?" (L2-099, mocks dialogs/delete-request): the safe action "Keep request"
 * has focus; "Delete" removes the request and its matches. A failure leaves it open and offers
 * another try. Closes with true once deleted.
 */
@Component({
  selector: 'tl-delete-request-dialog',
  imports: [AlertComponent, ButtonDirective, DialogComponent],
  templateUrl: './delete-request-dialog.html',
  styleUrl: './delete-request-dialog.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DeleteRequestDialog {
  private readonly api = inject(ItemRequestsApi);
  readonly ref = inject<DialogRef<boolean>>(DialogRef);
  readonly data = inject<DeleteRequestData>(DIALOG_DATA);

  readonly state = signal<'default' | 'busy' | 'failed'>('default');

  async submit(): Promise<void> {
    if (this.state() === 'busy') return;
    this.state.set('busy');
    const result = await this.api.remove(this.data.id);
    if (result.ok || result.problem.status === 404) {
      this.ref.close(true);
      return;
    }
    this.state.set('failed');
  }
}
