import { Locator, Page } from '@playwright/test';

/** The listing detail page at /items/{reference} (mocks pages/listing-detail). */
export class ListingDetailPage {
  readonly heading: Locator;
  readonly breadcrumb: Locator;
  readonly gallery: Locator;
  readonly thumbs: Locator;
  readonly track: Locator;
  readonly info: Locator;
  readonly price: Locator;
  readonly facts: Locator;
  readonly flaws: Locator;
  readonly seller: Locator;
  readonly buyBar: Locator;
  readonly buyNow: Locator;
  readonly editListing: Locator;
  readonly withdraw: Locator;
  readonly stamp: Locator;
  readonly reservedAlert: Locator;
  readonly similar: Locator;
  readonly loading: Locator;

  constructor(private readonly page: Page) {
    this.heading = page.locator('main').getByRole('heading', { level: 1 });
    this.breadcrumb = page.getByRole('navigation', { name: 'Breadcrumb' });
    this.gallery = page.getByRole('region', { name: 'Photos' });
    this.thumbs = this.gallery.getByRole('button', { name: /^Show photo/ });
    this.track = this.gallery.locator('.gallery__track');
    this.info = page.locator('.detail__info');
    this.price = page.locator('.detail__price');
    this.facts = page.locator('dl.facts');
    this.flaws = page.locator('.flaws');
    this.seller = page.locator('.seller');
    this.buyBar = page.locator('.buy-bar');
    this.buyNow = page.getByRole('link', { name: 'Buy now' });
    this.editListing = page.getByRole('link', { name: 'Edit listing' });
    this.withdraw = page.getByRole('button', { name: 'Withdraw' });
    this.stamp = this.info.locator('.photo__stamp');
    this.reservedAlert = this.info.locator('.alert');
    this.similar = page.getByRole('region', { name: 'Similar in the closet' });
    this.loading = page.locator('[aria-busy="true"][aria-label="Loading listing"]');
  }

  async goto(reference = 'L-10001'): Promise<void> {
    await this.page.goto(`/items/${reference}`);
  }

  /** The photo shown in the main view: the one scrolled into the track. */
  async shownPhotoAlt(): Promise<string | null> {
    return this.track.evaluate((track) => {
      const index = Math.round(track.scrollLeft / track.clientWidth);
      return track.querySelectorAll('img')[index]?.getAttribute('alt') ?? null;
    });
  }
}
