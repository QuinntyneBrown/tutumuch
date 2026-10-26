import { ChangeDetectionStrategy, Component, computed, effect, inject, input, untracked } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { Router, RouterLink } from '@angular/router';
import {
  AlertComponent,
  ButtonDirective,
  BuyBarComponent,
  CenterStateComponent,
  DialogService,
  GalleryComponent,
  IconComponent,
  ListingGridComponent,
  SellerSummaryComponent,
  SizeTagComponent,
} from '@tutumuch/components';

import { toCardView } from '../card-view';
import { WithdrawListingData, WithdrawListingDialog } from '../withdraw-listing/withdraw-listing-dialog';
import { ListingDetailStore } from './listing-detail-store';

/** "$85.00": the detail page always shows cents (mocks pages/listing-detail). */
export function formatMoney(value: string): string {
  return `$${Number(value).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

/** "Just now", "5 minutes ago", "2 hours ago", "Yesterday" or "4 days ago". */
export function listedAgo(iso: string, now = Date.now()): string {
  const minutes = Math.floor((now - Date.parse(iso)) / 60_000);
  if (minutes < 1) return 'Just now';
  if (minutes < 60) return minutes === 1 ? '1 minute ago' : `${minutes} minutes ago`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return hours === 1 ? '1 hour ago' : `${hours} hours ago`;
  const days = Math.floor(hours / 24);
  return days === 1 ? 'Yesterday' : `${days} days ago`;
}

/**
 * The listing detail page at /items/{reference} (mocks pages/listing-detail): gallery, details,
 * seller summary and the buy bar for the viewer, with the loading, unavailable and error states.
 * The seller of an Active listing gets "Edit listing" and "Withdraw" (edit-and-withdraw-listing).
 * One column with a sticky buy bar below 992 px, two columns from 992 px (L2-025, L2-063).
 * All listing text is bound as text, never as HTML.
 */
@Component({
  selector: 'tm-listing-detail-page',
  imports: [
    RouterLink,
    AlertComponent,
    ButtonDirective,
    BuyBarComponent,
    CenterStateComponent,
    GalleryComponent,
    IconComponent,
    ListingGridComponent,
    SellerSummaryComponent,
    SizeTagComponent,
  ],
  providers: [ListingDetailStore],
  templateUrl: './listing-detail-page.html',
  styleUrl: './listing-detail-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ListingDetailPage {
  readonly store = inject(ListingDetailStore);
  private readonly title = inject(Title);
  private readonly dialogs = inject(DialogService);
  private readonly router = inject(Router);

  /** From the route parameter. */
  readonly reference = input.required<string>();

  readonly money = formatMoney;
  readonly similarCards = computed(() => this.store.similar().map(toCardView));
  readonly categoryPath = computed(() => `/${(this.store.listing()?.category ?? '').toLowerCase()}`);
  readonly listed = computed(() => {
    const listing = this.store.listing();
    return listing ? listedAgo(listing.publishedAt) : '';
  });
  readonly fullSize = computed(() => {
    const label = this.store.listing()?.sizeLabel ?? '';
    return `Men's ${label}`;
  });

  /** Confirms, then takes the listing off the catalog and goes to the seller's listings (L2-020). */
  async withdraw(): Promise<void> {
    const listing = this.store.listing();
    if (!listing) return;
    const ref = this.dialogs.open<WithdrawListingDialog, WithdrawListingData, boolean>(WithdrawListingDialog, {
      reference: listing.reference,
      title: listing.title,
    });
    if (await ref.closed) await this.router.navigate(['/account/listings']);
  }

  constructor() {
    effect(() => {
      const reference = this.reference();
      untracked(() => void this.store.load(reference));
    });
    effect(() => {
      const listing = this.store.listing();
      if (listing) this.title.setTitle(`${listing.title} · ${listing.brand} · Tutu Much`);
    });
  }
}
