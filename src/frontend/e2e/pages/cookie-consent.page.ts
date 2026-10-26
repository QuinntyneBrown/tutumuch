import { Locator, Page } from '@playwright/test';

/** The cookie consent banner, the "Cookie settings" dialog and the footer link that opens it. */
export class CookieConsentPage {
  readonly banner: Locator;
  readonly accept: Locator;
  readonly necessaryOnly: Locator;
  readonly bannerSettings: Locator;
  readonly footerSettings: Locator;
  readonly dialog: Locator;
  readonly necessary: Locator;
  readonly analytics: Locator;
  readonly save: Locator;
  readonly cancel: Locator;
  readonly toast: Locator;

  constructor(private readonly page: Page) {
    this.banner = page.getByRole('region', { name: 'Cookies' });
    this.accept = this.banner.getByRole('button', { name: 'Accept analytics' });
    this.necessaryOnly = this.banner.getByRole('button', { name: 'Necessary only' });
    this.bannerSettings = this.banner.getByRole('button', { name: 'Cookie settings' });
    this.footerSettings = page
      .getByRole('contentinfo')
      .getByRole('navigation', { name: 'Footer' })
      .getByRole('button', { name: 'Cookie settings' });
    this.dialog = page.getByRole('dialog', { name: 'Cookie settings' });
    this.necessary = this.dialog.getByRole('switch', { name: 'Strictly necessary' });
    this.analytics = this.dialog.getByRole('switch', { name: 'Analytics' });
    this.save = this.dialog.getByRole('button', { name: 'Save' });
    this.cancel = this.dialog.getByRole('button', { name: 'Cancel' });
    this.toast = page.getByRole('status').filter({ hasText: 'Cookie choice saved' });
  }

  async cookieNames(): Promise<string[]> {
    return (await this.page.context().cookies()).map((cookie) => cookie.name);
  }

  async consentCookie(): Promise<string | undefined> {
    return (await this.page.context().cookies()).find((cookie) => cookie.name === 'tm_consent')?.value;
  }
}
