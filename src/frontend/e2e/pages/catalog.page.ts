import { Locator, Page } from '@playwright/test';

/** The catalog at / and the category pages /dresses, /tops... (mocks pages/catalog). */
export class CatalogPage {
  readonly heading: Locator;
  readonly listings: Locator;
  readonly grid: Locator;
  readonly cards: Locator;
  readonly skeletonGrid: Locator;
  readonly count: Locator;
  readonly shown: Locator;
  readonly loadMore: Locator;
  readonly emptyState: Locator;
  readonly offlineBanner: Locator;
  readonly loadMoreError: Locator;

  constructor(private readonly page: Page) {
    this.heading = page.locator('main').getByRole('heading', { level: 1 });
    this.listings = page.getByRole('region', { name: 'Listings' });
    this.grid = this.listings.locator('ul.listing-grid:not([aria-busy])');
    this.cards = this.grid.getByRole('listitem');
    this.skeletonGrid = this.listings.getByRole('list', { name: 'Loading listings' });
    this.count = this.listings.locator('[aria-live="polite"]').first();
    this.shown = this.listings.locator('.load-more > span').first();
    this.loadMore = this.listings.getByRole('button', { name: 'Load more' });
    this.emptyState = this.listings.locator('.empty');
    this.offlineBanner = page.getByRole('status').filter({ hasText: 'You’re offline' });
    this.loadMoreError = this.listings.getByRole('alert');
  }

  async goto(path = '/'): Promise<void> {
    await this.page.goto(path);
  }

  card(title: string): Locator {
    return this.cards.filter({ has: this.page.getByText(title, { exact: true }) });
  }

  /** The number of columns the grid lays out. */
  async columns(): Promise<number> {
    return this.grid.evaluate((el) => getComputedStyle(el).gridTemplateColumns.split(' ').length);
  }
}
