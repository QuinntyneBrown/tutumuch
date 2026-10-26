import { Locator, Page } from '@playwright/test';

/** The public member profile at /members/{displayName} (mocks pages/profile). */
export class MemberProfilePage {
  readonly heading: Locator;
  readonly header: Locator;
  readonly stats: Locator;
  readonly forSale: Locator;
  readonly cards: Locator;
  readonly reviews: Locator;
  readonly loadMore: Locator;

  constructor(private readonly page: Page) {
    this.heading = page.locator('main').getByRole('heading', { level: 1 });
    this.header = page.locator('.profile-head');
    this.stats = page.locator('.stats');
    this.forSale = page.getByRole('region', { name: 'For sale' });
    this.cards = this.forSale.locator('.listing-card');
    this.reviews = page.getByRole('region', { name: 'Reviews' });
    this.loadMore = page.getByRole('button', { name: /Load more/ });
  }

  async goto(displayName = 'tutu_tessa'): Promise<void> {
    await this.page.goto(`/members/${displayName}`);
  }

  stat(label: string): Locator {
    return this.stats.locator('.stat').filter({ hasText: label }).locator('.stat__value');
  }
}
