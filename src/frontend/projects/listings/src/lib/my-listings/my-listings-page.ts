import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ListingStatus, ListingsApi, MyListingRow, MyListings, shortRef } from '@tutumuch/api';
import {
  ButtonDirective,
  EmptyStateComponent,
  FilterChipsComponent,
  IconComponent,
  ItemRowComponent,
  StatusComponent,
  ToastService,
} from '@tutumuch/components';

import {
  draftExpiryText,
  draftExpiryTitle,
  filterOptions,
  listingsSubtitle,
  rowDate,
  rowLink,
  rowPrice,
  rowTitle,
  statusFromQuery,
  statusTone,
} from './my-listings-text';

/**
 * The seller's "Your listings" at /account/listings (mocks pages/my-listings, L2-021): every
 * listing newest first, filtered by the status in the URL query so the filter survives a reload
 * and back navigation. A seller with drafts is told once per visit that drafts older than 90 days
 * are deleted (mocks notifications/toast info, L2-019).
 */
@Component({
  selector: 'tm-my-listings-page',
  imports: [
    RouterLink,
    ButtonDirective,
    EmptyStateComponent,
    FilterChipsComponent,
    IconComponent,
    ItemRowComponent,
    StatusComponent,
  ],
  templateUrl: './my-listings-page.html',
  styleUrl: './my-listings-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyListingsPage {
  private readonly api = inject(ListingsApi);
  private readonly router = inject(Router);
  private readonly toasts = inject(ToastService);
  private draftNoticeShown = false;
  private readonly query = toSignal(inject(ActivatedRoute).queryParamMap, { requireSync: true });

  readonly status = computed(() => statusFromQuery(this.query().get('status')));
  readonly listings = signal<MyListings | null>(null);
  readonly view = signal<'loading' | 'loaded' | 'error'>('loading');
  readonly errorRef = signal('');

  readonly selected = computed(() => this.status()?.toLowerCase() ?? 'all');
  readonly options = computed(() => {
    const listings = this.listings();
    return listings ? filterOptions(listings.counts) : [];
  });
  readonly subtitle = computed(() => {
    const listings = this.listings();
    return listings && listings.counts.all > 0 ? listingsSubtitle(listings.counts) : '';
  });
  /** The chips show while loading and once the seller has a listing; not on the empty or error state. */
  readonly showFilter = computed(() => {
    const listings = this.listings();
    return this.view() === 'loading' || (this.view() === 'loaded' && !!listings && listings.counts.all > 0);
  });

  readonly title = rowTitle;
  readonly price = rowPrice;
  readonly link = rowLink;
  readonly tone = statusTone;

  constructor() {
    effect(() => {
      const status = this.status();
      untracked(() => void this.load(status));
    });
  }

  date = (row: MyListingRow): string => rowDate(row.createdAt, new Date());

  filter(value: string): void {
    void this.router.navigate([], { queryParams: { status: value === 'all' ? null : value } });
  }

  retry(): Promise<void> {
    return this.load(this.status());
  }

  private async load(status: ListingStatus | null): Promise<void> {
    this.view.set('loading');
    const result = await this.api.mine(status ?? undefined);
    if (!result.ok) {
      this.errorRef.set(shortRef(result.problem.correlationId));
      this.view.set('error');
      return;
    }
    this.listings.set(result.value);
    if (!this.draftNoticeShown && result.value.counts.draft > 0) {
      this.draftNoticeShown = true;
      this.toasts.show({ tone: 'info', title: draftExpiryTitle, text: draftExpiryText });
    }
    this.view.set('loaded');
  }
}
