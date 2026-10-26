import { Injectable, inject } from '@angular/core';
import { DialogRef, DialogService } from '@tutumuch/components';

import { CookieSettingsDialog } from './cookie-settings-dialog';

/** Opens "Cookie settings" from any entry point: the footer and the consent banner. */
@Injectable({ providedIn: 'root' })
export class CookieSettingsDialogService {
  private readonly dialogs = inject(DialogService);

  open(): DialogRef<boolean> {
    return this.dialogs.open<CookieSettingsDialog, undefined, boolean>(CookieSettingsDialog);
  }
}
