import { Locator, Page } from '@playwright/test';

/** The administrator's Showcase requests page at /admin/requests (mocks pages/showcase-requests). */
export class ShowcaseRequestsPage {
  readonly heading: Locator;
  readonly subtitle: Locator;
  readonly requests: Locator;
  readonly loading: Locator;
  readonly empty: Locator;
  readonly state: Locator;
  readonly tryAgain: Locator;

  constructor(private readonly page: Page) {
    const main = page.locator('main');
    this.heading = main.getByRole('heading', { level: 1 });
    this.subtitle = main.locator('.page-header .subtitle');
    this.requests = main.locator('.request-list > li');
    this.loading = main.locator('[aria-busy="true"][aria-label="Loading requests"]');
    this.empty = main.locator('.empty');
    this.state = main.locator('.center-state');
    this.tryAgain = main.getByRole('button', { name: 'Try again' });
  }

  async goto(): Promise<void> {
    await this.page.goto('/admin/requests');
  }
}
