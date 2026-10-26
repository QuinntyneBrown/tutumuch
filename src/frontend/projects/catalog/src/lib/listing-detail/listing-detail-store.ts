import { Injectable, computed, inject, signal } from '@angular/core';
import { CatalogApi, ListingCard, ListingDetail, shortRef } from '@tutumuch/api';

export type ListingDetailView = 'loading' | 'loaded' | 'unavailable' | 'error';

/** What the buy bar offers: checkout, sign in first, the seller's own actions, or nothing. */
export type PrimaryAction = 'buy' | 'sign-in-to-buy' | 'manage' | 'none';

/** How many other items a Sold listing suggests. */
const SIMILAR_COUNT = 4;

/**
 * Signal state of one listing detail page (view-listing-detail). A 404 is the unavailable view;
 * any other failure is the error view with its reference. A Sold listing also loads up to 4
 * other items in its category.
 */
@Injectable()
export class ListingDetailStore {
  private readonly api = inject(CatalogApi);
  private reference = '';

  readonly listing = signal<ListingDetail | null>(null);
  readonly view = signal<ListingDetailView>('loading');
  readonly errorRef = signal('');
  readonly similar = signal<ListingCard[]>([]);

  readonly primaryAction = computed<PrimaryAction>(() => {
    const listing = this.listing();
    if (!listing || listing.status !== 'Active') return 'none';
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

  private async loadSimilar(listing: ListingDetail): Promise<void> {
    const result = await this.api.listings(1, { category: listing.category.toLowerCase() });
    if (!result.ok) return;
    this.similar.set(result.value.items.filter((card) => card.reference !== listing.reference).slice(0, SIMILAR_COUNT));
  }
}
