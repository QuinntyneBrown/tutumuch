import { Locator, Page } from '@playwright/test';

/** The seller's "Your listings" at /account/listings (mocks pages/my-listings). */
export class MyListingsPage {
  readonly heading: Locator;
  readonly subtitle: Locator;
  readonly sellOutgrownGear: Locator;
  readonly filter: Locator;
  readonly rows: Locator;
  readonly loading: Locator;
  readonly empty: Locator;
  readonly tryAgain: Locator;
  readonly draftToast: Locator;

  constructor(private readonly page: Page) {
    const main = page.locator('main');
    this.heading = main.getByRole('heading', { level: 1 });
    this.subtitle = main.locator('.page-header .subtitle');
    this.sellOutgrownGear = main.locator('.page-header').getByRole('link', { name: 'Sell outgrown gear' });
    this.filter = main.getByRole('group', { name: 'Filter by status' });
    this.rows = main.locator('.order-list:not([aria-busy]) li');
    this.loading = main.locator('[aria-busy="true"]');
    this.empty = main.locator('.empty');
    this.tryAgain = main.getByRole('button', { name: 'Try again' });
    this.draftToast = page.locator('.toast-region .toast').filter({ hasText: 'Drafts older than 90 days are deleted' });
  }

  async goto(query = ''): Promise<void> {
    await this.page.goto(`/account/listings${query}`);
  }

  chip(label: string): Locator {
    return this.filter.getByRole('button', { name: new RegExp(`^${label} \\d+$`) });
  }

  row(title: string): Locator {
    return this.rows.filter({ hasText: title });
  }
}
