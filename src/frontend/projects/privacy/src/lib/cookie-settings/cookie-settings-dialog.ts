import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ButtonDirective, DialogComponent, DialogRef, SwitchComponent } from '@tutumuch/components';

import { CookieConsentService } from '../consent/cookie-consent.service';

/**
 * "Cookie settings" (L2-056, mocks dialogs/cookie-settings): strictly necessary is locked on,
 * analytics starts at the current choice and has the first focus. "Save" records the choice,
 * which also counts as a first choice; "Cancel" changes nothing. No request, so no busy state.
 */
@Component({
  selector: 'tl-cookie-settings-dialog',
  imports: [ButtonDirective, DialogComponent, SwitchComponent],
  templateUrl: './cookie-settings-dialog.html',
  styleUrl: './cookie-settings-dialog.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CookieSettingsDialog {
  private readonly consent = inject(CookieConsentService);
  readonly ref = inject<DialogRef<boolean>>(DialogRef);
  readonly analytics = signal(this.consent.consent() === 'analytics');

  save(): void {
    this.consent.save(this.analytics());
    this.ref.close(this.analytics());
  }
}
