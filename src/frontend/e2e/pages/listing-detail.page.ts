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
  /** Buy now on a showcase listing: a button, not a link to checkout (L2-108). */
  readonly buyNowButton: Locator;
  /** The Sold notice that replaces the buy bar after Buy now on a showcase listing. */
  readonly soldNotice: Locator;
  readonly soldNoticeHeading: Locator;
  readonly requestItem: Locator;
  readonly updateRequest: Locator;
  /** The owner's Showcase switch and See requests link (L2-107). */
  readonly showcaseSwitch: Locator;
  readonly seeRequests: Locator;
  readonly toast: Locator;

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
    this.buyNowButton = page.getByRole('button', { name: 'Buy now' });
    this.soldNotice = page.locator('.sold-notice');
    this.soldNoticeHeading = this.soldNotice.getByRole('heading', { name: 'This one has already found a home' });
    this.requestItem = this.soldNotice.getByRole('button', { name: 'Request this item' });
    this.updateRequest = this.soldNotice.getByRole('button', { name: 'Update your request' });
    this.showcaseSwitch = page.getByRole('switch', { name: 'Showcase' });
    this.seeRequests = page.getByRole('link', { name: 'See requests' });
    this.toast = page.locator('.toast-region .toast');
  }

  async goto(reference = 'L-10001', query = ''): Promise<void> {
    await this.page.goto(`/items/${reference}${query}`);
  }

  /** The photo shown in the main view: the one scrolled into the track. */
  async shownPhotoAlt(): Promise<string | null> {
    return this.track.evaluate((track) => {
      const index = Math.round(track.scrollLeft / track.clientWidth);
      return track.querySelectorAll('img')[index]?.getAttribute('alt') ?? null;
    });
  }
}
