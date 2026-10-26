import { Locator, Page } from '@playwright/test';

/** The one-click marketing unsubscribe at /emails/unsubscribe?token=… (not mocked; L2-043). */
export class UnsubscribePage {
  readonly heading: Locator;
  readonly lead: Locator;
  readonly manageEmails: Locator;
  readonly tryAgain: Locator;
  readonly ref: Locator;

  constructor(private readonly page: Page) {
    const main = page.locator('main');
    this.heading = main.getByRole('heading', { level: 1 });
    this.lead = main.locator('.unsubscribe__lead');
    this.manageEmails = main.getByRole('link', { name: 'Manage your emails' });
    this.tryAgain = main.getByRole('button', { name: 'Try again' });
    this.ref = main.locator('.ref');
  }

  async goto(token: string): Promise<void> {
    await this.page.goto(`/emails/unsubscribe?token=${encodeURIComponent(token)}`);
  }
}
