import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ListingsApi } from '@tutumuch/api';
import { AlertComponent, ButtonDirective, DIALOG_DATA, DialogComponent, DialogRef } from '@tutumuch/components';

/** What Withdraw needs: the listing's reference and title. */
export interface WithdrawListingData {
  reference: string;
  title: string;
}

/**
 * "Withdraw “{title}”?" (L2-020, mocks dialogs/withdraw-listing): the safe action "Keep listing"
 * has focus; "Withdraw" takes the listing off the catalog for good. A failure leaves the listing
 * live and offers another try. Closes with true once withdrawn.
 */
@Component({
  selector: 'tl-withdraw-listing-dialog',
  imports: [AlertComponent, ButtonDirective, DialogComponent],
  templateUrl: './withdraw-listing-dialog.html',
  styleUrl: './withdraw-listing-dialog.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WithdrawListingDialog {
  private readonly api = inject(ListingsApi);
  readonly ref = inject<DialogRef<boolean>>(DialogRef);
  readonly data = inject<WithdrawListingData>(DIALOG_DATA);

  readonly state = signal<'default' | 'busy' | 'failed'>('default');

  async submit(): Promise<void> {
    if (this.state() === 'busy') return;
    this.state.set('busy');
    const result = await this.api.withdraw(this.data.reference);
    if (result.ok) {
      this.ref.close(true);
      return;
    }
    this.state.set('failed');
  }
}
