import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ConsentBannerComponent, ToastService } from '@tutumuch/components';

import { CookieSettingsDialogService } from '../cookie-settings/cookie-settings.service';
import { CookieConsentService } from './cookie-consent.service';

/**
 * The consent banner in the app shell (L2-056, mocks notifications/cookie-consent): shown while no
 * choice is recorded. Either button saves the choice and shows "Cookie choice saved"; "Cookie
 * settings" opens the dialog, and saving there closes the banner too.
 */
@Component({
  selector: 'tm-cookie-consent',
  imports: [ConsentBannerComponent],
  templateUrl: './cookie-consent.html',
  styleUrl: './cookie-consent.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CookieConsentComponent {
  private readonly toasts = inject(ToastService);
  readonly consent = inject(CookieConsentService);
  readonly settings = inject(CookieSettingsDialogService);

  accept(): void {
    this.consent.acceptAnalytics();
    this.saved('Analytics on. Change it any time in the footer.');
  }

  decline(): void {
    this.consent.necessaryOnly();
    this.saved('Necessary only. Change it any time in the footer.');
  }

  private saved(text: string): void {
    this.toasts.show({ tone: 'success', title: 'Cookie choice saved', text });
  }
}
