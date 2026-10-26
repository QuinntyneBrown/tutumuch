import { ChangeDetectionStrategy, Component } from '@angular/core';
import { SkipLinkComponent, ToastRegionComponent } from '@tutumuch/components';
import { CookieConsentComponent } from '@tutumuch/privacy';

import { AppShellComponent } from './app-shell/app-shell';

/** Root component: skip link, the app shell, the cookie consent banner and the toast region (responsive-accessible-shell). */
@Component({
  selector: 'tm-root',
  imports: [SkipLinkComponent, AppShellComponent, CookieConsentComponent, ToastRegionComponent],
  templateUrl: './app.html',
  styleUrl: './app.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {}
