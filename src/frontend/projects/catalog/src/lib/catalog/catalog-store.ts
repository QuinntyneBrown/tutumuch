import { Injectable, computed, inject, signal } from '@angular/core';
import { CatalogApi, CatalogFilterParams, ListingCard, shortRef } from '@tutumuch/api';

export type CatalogView = 'loading' | 'loaded' | 'empty' | 'error' | 'offline';

/** The last successful first page of the whole catalog: public listing data only. */
const SNAPSHOT_KEY = 'tutumuch.catalog.snapshot.v1';

/**
 * Signal state of one catalog page (browse-catalog). load() fetches page 1 of a result set
 * (search-and-filter-catalog) and replaces the cards; loadMore() appends the next page, dropping any card already shown. Offline, the last
 * saved first page of the unfiltered catalog is shown instead. Storage failures only disable the offline view.
 */
@Injectable()
export class CatalogStore {
  private readonly api = inject(CatalogApi);
  private filters: CatalogFilterParams = {};
  /** Only the latest load may set the state, so a slow earlier result set never wins. */
  private sequence = 0;

  readonly items = signal<ListingCard[]>([]);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly view = signal<CatalogView>('loading');
  readonly errorRef = signal('');
  readonly loadingMore = signal(false);
  readonly moreErrorRef = signal<string | null>(null);

  readonly hasMore = computed(() => this.view() === 'loaded' && this.items().length < this.totalCount());

  async load(filters: CatalogFilterParams): Promise<void> {
    this.filters = filters;
    const whole = Object.keys(filters).length === 0;
    this.view.set('loading');
    this.moreErrorRef.set(null);
    const sequence = ++this.sequence;
    const result = await this.api.listings(1, filters);
    if (sequence !== this.sequence) return;
    if (!result.ok) {
      const snapshot = result.problem.status === 0 && !navigator.onLine && whole ? readSnapshot() : null;
      if (snapshot) {
        this.items.set(snapshot);
        this.totalCount.set(snapshot.length);
        this.view.set('offline');
      } else {
        this.errorRef.set(shortRef(result.problem.correlationId));
        this.view.set('error');
      }
      return;
    }
    this.items.set(result.value.items);
    this.totalCount.set(result.value.totalCount);
    this.page.set(1);
    this.view.set(result.value.totalCount === 0 ? 'empty' : 'loaded');
    if (whole) writeSnapshot(result.value.items);
  }

  retry(): Promise<void> {
    return this.load(this.filters);
  }

  async loadMore(): Promise<void> {
    if (this.loadingMore()) return;
    this.loadingMore.set(true);
    this.moreErrorRef.set(null);
    const next = this.page() + 1;
    const sequence = this.sequence;
    const result = await this.api.listings(next, this.filters);
    this.loadingMore.set(false);
    if (sequence !== this.sequence) return;
    if (!result.ok) {
      this.moreErrorRef.set(shortRef(result.problem.correlationId));
      return;
    }
    const shown = new Set(this.items().map((card) => card.id));
    this.items.update((items) => [...items, ...result.value.items.filter((card) => !shown.has(card.id))]);
    this.totalCount.set(result.value.totalCount);
    this.page.set(next);
  }
}

function readSnapshot(): ListingCard[] | null {
  try {
    const saved = JSON.parse(localStorage.getItem(SNAPSHOT_KEY) ?? 'null') as ListingCard[] | null;
    return Array.isArray(saved) && saved.length > 0 ? saved : null;
  } catch {
    return null;
  }
}

function writeSnapshot(items: ListingCard[]): void {
  try {
    localStorage.setItem(SNAPSHOT_KEY, JSON.stringify(items));
  } catch {
    // Blocked storage only disables the offline view.
  }
}
