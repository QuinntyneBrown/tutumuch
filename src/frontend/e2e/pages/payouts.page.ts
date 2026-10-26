import { Locator, Page } from '@playwright/test';

/** The seller's payouts page at /account/payouts and the payout banner above the top bar (mocks pages/payouts, notifications/payout-banner). */
export class PayoutsPage {
  readonly heading: Locator;
  readonly paidOut: Locator;
  readonly pending: Locator;
  readonly bank: Locator;
  readonly change: Locator;
  readonly sectionHeading: Locator;
  readonly rows: Locator;
  readonly setupAlert: Locator;
  readonly setUp: Locator;
  readonly alert: Locator;
  readonly loading: Locator;
  readonly empty: Locator;
  readonly banner: Locator;
  readonly dismissBanner: Locator;

  constructor(private readonly page: Page) {
    const main = page.locator('main');
    this.heading = main.getByRole('heading', { level: 1 });
    this.paidOut = main.locator('.stat', { hasText: 'Paid out' }).locator('.stat__value');
    this.pending = main.locator('.stat', { hasText: 'Pending' }).locator('.stat__value');
    this.bank = main.locator('.settings-list li');
    this.change = this.bank.getByRole('button', { name: 'Change' });
    this.sectionHeading = main.locator('.section-head h2');
    this.rows = main.locator('.order-list li');
    this.setupAlert = main.locator('.alert--warning');
    this.setUp = main.getByRole('button', { name: 'Set up payouts' });
    this.alert = main.locator('.alert--danger');
    this.loading = main.locator('[aria-busy="true"]');
    this.empty = main.locator('.empty');
    this.banner = page.locator('.alert.banner');
    this.dismissBanner = this.banner.getByRole('button', { name: 'Dismiss' });
  }

  async goto(query = ''): Promise<void> {
    await this.page.goto(`/account/payouts${query}`);
  }

  row(reference: string): Locator {
    return this.rows.filter({ hasText: reference });
  }
}
