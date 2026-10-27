import { Locator, Page } from '@playwright/test';

/** The member's item requests at /account/requests (mocks pages/my-requests). */
export class MyRequestsPage {
  readonly heading: Locator;
  readonly subtitle: Locator;
  readonly requestItem: Locator;
  readonly requests: Locator;
  readonly limitLine: Locator;
  readonly empty: Locator;
  readonly loading: Locator;
  readonly tryAgain: Locator;
  readonly toast: Locator;
  readonly dialog: Locator;

  constructor(private readonly page: Page) {
    const main = page.locator('main');
    this.heading = main.getByRole('heading', { level: 1 });
    this.subtitle = main.locator('.page-header .subtitle');
    this.requestItem = main.getByRole('link', { name: 'Request an item' });
    this.requests = main.locator('li.request');
    this.limitLine = main.locator('.request-list__limit');
    this.empty = main.locator('.empty');
    this.loading = main.locator('[aria-busy="true"]');
    this.tryAgain = main.getByRole('button', { name: 'Try again' });
    this.toast = page.locator('.toast-region .toast');
    this.dialog = page.getByRole('dialog');
  }

  async goto(): Promise<void> {
    await this.page.goto('/account/requests');
  }

  /** The block of the request with these keywords. */
  request(keywords: string): Locator {
    return this.requests.filter({ has: this.page.getByRole('heading', { level: 2, name: keywords, exact: true }) });
  }
}
