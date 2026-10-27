import { Locator, Page } from '@playwright/test';

/** The member's email settings at /account/emails (mocks pages/notification-settings). */
export class EmailSettingsPage {
  readonly heading: Locator;
  readonly lead: Locator;
  readonly rows: Locator;
  readonly security: Locator;
  readonly orders: Locator;
  readonly reviews: Locator;
  readonly itemRequests: Locator;
  readonly marketing: Locator;
  readonly save: Locator;
  readonly saved: Locator;
  readonly loading: Locator;
  readonly error: Locator;
  readonly tryAgain: Locator;
  readonly toast: Locator;

  constructor(private readonly page: Page) {
    const main = page.locator('main');
    this.heading = main.getByRole('heading', { level: 1 });
    this.lead = main.locator('.page-header .subtitle');
    this.rows = main.locator('.settings-list li');
    this.security = main.getByRole('switch', { name: 'Account security emails, always on' });
    this.orders = main.getByRole('switch', { name: 'Order emails, always on' });
    this.reviews = main.getByRole('switch', { name: 'Review emails' });
    this.itemRequests = main.getByRole('switch', { name: 'Item request emails' });
    this.marketing = main.getByRole('switch', { name: 'Marketing emails' });
    this.save = main.getByRole('button', { name: 'Save' });
    this.saved = main.getByRole('status').filter({ hasText: 'Email settings saved' });
    this.loading = main.locator('[aria-busy="true"]');
    this.error = main.locator('.empty');
    this.tryAgain = main.getByRole('button', { name: 'Try again' });
    this.toast = page.locator('.toast-region .toast');
  }

  async goto(): Promise<void> {
    await this.page.goto('/account/emails');
  }

  row(title: string): Locator {
    return this.rows.filter({ hasText: title });
  }
}
