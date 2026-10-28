import { Injectable, computed, inject, signal } from '@angular/core';
import { CatalogApi, ListingCard, ListingDetail, ShowcaseApi, ShowcaseRequest, shortRef } from '@tutumuch/api';

export type ListingDetailView = 'loading' | 'loaded' | 'unavailable' | 'error';

/**
 * What the buy bar offers: checkout, sign in first, Buy now that shows the Sold notice of a
 * showcase listing (L2-108), the seller's own actions, or nothing.
 */
export type PrimaryAction = 'buy' | 'sign-in-to-buy' | 'showcase' | 'manage' | 'none';

/** How many other items a Sold listing suggests. */
const SIMILAR_COUNT = 4;

/**
 * Signal state of one listing detail page (view-listing-detail). A 404 is the unavailable view;
 * any other failure is the error view with its reference. A Sold listing also loads up to 4
 * other items in its category. On a showcase listing Buy now shows the Sold notice instead of
 * checkout, and a signed-in member's request for it is read the first time the notice shows.
 */
@Injectable()
export class ListingDetailStore {
  private readonly api = inject(CatalogApi);
  private readonly showcaseApi = inject(ShowcaseApi);
  private reference = '';

  readonly listing = signal<ListingDetail | null>(null);
  readonly view = signal<ListingDetailView>('loading');
  readonly errorRef = signal('');
  readonly similar = signal<ListingCard[]>([]);
  /** True once Buy now was chosen on a showcase listing (L2-108). */
  readonly soldNoticeShown = signal(false);
  /** The member's request for this showcase listing, null when there is none. */
  readonly myRequest = signal<ShowcaseRequest | null>(null);
  private myRequestLoaded = false;

  readonly primaryAction = computed<PrimaryAction>(() => {
    const listing = this.listing();
    if (!listing || listing.status !== 'Active') return 'none';
    if (listing.viewer.relation !== 'Seller' && listing.showcase) return 'showcase';
    switch (listing.viewer.relation) {
      case 'Seller':
        return 'manage';
      case 'Anonymous':
        return 'sign-in-to-buy';
      default:
        return 'buy';
    }
  });

  async load(reference: string): Promise<void> {
    this.reference = reference;
    this.view.set('loading');
    this.similar.set([]);
    this.soldNoticeShown.set(false);
    this.myRequest.set(null);
    this.myRequestLoaded = false;
    const result = await this.api.listing(reference);
    if (!result.ok) {
      this.listing.set(null);
      this.errorRef.set(shortRef(result.problem.correlationId));
      this.view.set(result.problem.status === 404 ? 'unavailable' : 'error');
      return;
    }
    this.listing.set(result.value);
    this.view.set('loaded');
    if (result.value.status === 'Sold') await this.loadSimilar(result.value);
  }

  retry(): Promise<void> {
    return this.load(this.reference);
  }

  /** Shows the Sold notice; a signed-in member's request is read once, so the notice can say when they asked. */
  async showSoldNotice(): Promise<void> {
    this.soldNoticeShown.set(true);
    if (this.myRequestLoaded || this.listing()?.viewer.relation !== 'OtherMember') return;
    this.myRequestLoaded = true;
    const result = await this.showcaseApi.myRequest(this.reference);
    this.myRequest.set(result.ok ? result.value : null);
  }

  /** Marks or unmarks the owner's listing (L2-107); false when TuTuMuch API refused it. */
  async setShowcase(showcase: boolean): Promise<boolean> {
    const listing = this.listing();
    if (!listing) return false;
    const result = await this.showcaseApi.setShowcase(listing.reference, showcase);
    this.listing.set({ ...listing, showcase: result.ok ? result.value.showcase : listing.showcase });
    return result.ok;
  }

  private async loadSimilar(listing: ListingDetail): Promise<void> {
    const result = await this.api.listings(1, { category: listing.category.toLowerCase() });
    if (!result.ok) return;
    this.similar.set(result.value.items.filter((card) => card.reference !== listing.reference).slice(0, SIMILAR_COUNT));
  }
}
