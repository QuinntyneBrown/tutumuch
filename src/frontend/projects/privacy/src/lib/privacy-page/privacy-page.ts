import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ButtonDirective } from '@tutumuch/components';

import { CookieSettingsDialogService } from '../cookie-settings/cookie-settings.service';

/**
 * Privacy at /privacy (L2-054, L2-056, L2-005; mocks pages/privacy): what TuTuMuch keeps, what stays
 * private and erasure, with a "Cookie settings" link that opens the dialog. Static copy, no TuTuMuch API call.
 */
@Component({
  selector: 'tl-privacy-page',
  imports: [ButtonDirective],
  templateUrl: './privacy-page.html',
  styleUrl: './privacy-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PrivacyPage {
  readonly cookieSettings = inject(CookieSettingsDialogService);

  readonly items = [
    { title: 'What we collect', detail: 'Email, password hash, display name, shipping addresses, and orders.' },
    { title: 'Never public', detail: 'Your email, phone and street address never appear on TuTuMuch pages.' },
    { title: 'Encrypted', detail: 'Shipping addresses are encrypted at rest. Card numbers never reach us.' },
    { title: 'Erasure', detail: 'Close your account and we erase your data within 30 days.' },
    { title: 'Cookies', detail: 'Only necessary cookies until you accept analytics.' },
  ];
}
