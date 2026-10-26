import { Locator, Page } from '@playwright/test';

/** The member's "Purchases" at /account/purchases or "Sales" at /account/sales (mocks pages/purchases, pages/sales). */
export class OrderHistoryPage {
  readonly heading: Locator;
  readonly subtitle: Locator;
  readonly rows: Locator;
  readonly loading: Locator;
  readonly empty: Locator;
  readonly tryAgain: Locator;

  constructor(
    private readonly page: Page,
    private readonly list: 'purchases' | 'sales',
  ) {
    const main = page.locator('main');
    this.heading = main.getByRole('heading', { level: 1 });
    this.subtitle = main.locator('.page-header .subtitle');
    this.rows = main.locator('.order-list:not([aria-busy]) li');
    this.loading = main.locator('[aria-busy="true"]');
    this.empty = main.locator('.empty');
    this.tryAgain = main.getByRole('button', { name: 'Try again' });
  }

  async goto(): Promise<void> {
    await this.page.goto(`/account/${this.list}`);
  }

  row(title: string): Locator {
    return this.rows.filter({ hasText: title });
  }
}
