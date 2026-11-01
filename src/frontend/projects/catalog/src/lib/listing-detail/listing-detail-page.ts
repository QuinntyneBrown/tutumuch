import {
  ChangeDetectionStrategy,
  Component,
  Injector,
  DestroyRef,
  afterNextRender,
  computed,
  effect,
  inject,
  input,
  linkedSignal,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { Title } from '@angular/platform-browser';
import { Router, RouterLink } from '@angular/router';
import { AuctionSummary, AuthState, ListingDetail, showcaseRequestUrl } from '@tutumuch/api';
import {
  AlertComponent,
  AuctionPanelComponent,
  AuctionStatus,
  ButtonDirective,
  BuyBarComponent,
  CenterStateComponent,
  ConditionBadgeComponent,
  DialogService,
  GalleryComponent,
  IconComponent,
  ListingGridComponent,
  SellerSummaryComponent,
  SizeTagComponent,
  SkeletonComponent,
  SoldNoticeComponent,
  SwitchComponent,
  ToastService,
  auctionTime,
  timeLeft,
} from '@tutumuch/components';

import { toCardView } from '../card-view';
import {
  RequestShowcaseItemData,
  RequestShowcaseItemDialog,
  RequestShowcaseItemResult,
  requestedOn,
} from '../request-showcase-item/request-showcase-item-dialog';
import { PlaceBidData, PlaceBidDialog, PlaceBidResult } from '../place-bid/place-bid-dialog';
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
 * A sold listing is greyed and stamped Sold and offers "Find similar" (page guidelines 9).
 * The seller of an Active listing gets "Edit listing" and "Withdraw" (edit-and-withdraw-listing).
 * On a showcase listing Buy now shows the Sold notice and "Request this item" instead of checkout;
 * `?request=1` shows it with the request dialog open; the owner gets the Showcase switch
 * (request-showcase-item, L2-107 to L2-109).
 * One column with a sticky buy bar below 992 px, two columns from 992 px (L2-025, L2-063).
 * All listing text is bound as text, never as HTML.
 */
@Component({
  selector: 'tl-listing-detail-page',
  imports: [
    RouterLink,
    AlertComponent,
    AuctionPanelComponent,
    ButtonDirective,
    BuyBarComponent,
    CenterStateComponent,
    ConditionBadgeComponent,
    GalleryComponent,
    IconComponent,
    ListingGridComponent,
    SellerSummaryComponent,
    SizeTagComponent,
    SkeletonComponent,
    SoldNoticeComponent,
    SwitchComponent,
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
  private readonly auth = inject(AuthState);
  private readonly toasts = inject(ToastService);
  private readonly injector = inject(Injector);
  private readonly notice = viewChild(SoldNoticeComponent);

  /** From the route parameter. */
  readonly reference = input.required<string>();
  /** `?request=1` after signing in from "Request this item" (L2-109). */
  readonly request = input<string>();

  readonly money = formatMoney;
  readonly similarCards = computed(() => this.store.similar().map((card) => toCardView(card)));
  readonly categoryPath = computed(() => `/${(this.store.listing()?.category ?? '').toLowerCase()}`);
  readonly listed = computed(() => {
    const listing = this.store.listing();
    return listing ? listedAgo(listing.publishedAt) : '';
  });
  readonly fullSize = computed(() => `${this.store.listing()?.sizeLabel ?? ''}'`);
  readonly requestedOn = computed(() => {
    const request = this.store.myRequest();
    return request ? requestedOn(request.requestedAt) : null;
  });
  /** The switch's state: the listing's flag, put back when TuTuMuch API refuses a change. */
  readonly showcaseOn = linkedSignal(() => this.store.listing()?.showcase ?? false);
  /** The owner's switch: an administrator on their own Draft or Active listing (L2-107). */
  readonly showcaseSwitch = computed(() => {
    const listing = this.store.listing();
    return (
      !!listing &&
      listing.viewer.relation === 'Seller' &&
      this.auth.member()?.isAdministrator === true &&
      !listing.auction &&
      (listing.status === 'Active' || listing.status === 'Draft')
    );
  });

  /** Now, every 30 seconds, so the time left counts down and a running auction ends on the page. */
  readonly now = signal(Date.now());
  /** The auction panel of an auction listing (L2-114), as this viewer sees it; null for a fixed price. */
  readonly auction = computed(() => {
    const listing = this.store.listing();
    return listing?.auction ? auctionPanel(listing, listing.auction, this.now()) : null;
  });
  readonly barTotal = computed(() => {
    const listing = this.store.listing();
    return this.auction()?.amount ?? (listing ? formatMoney(listing.total) : '');
  });
  readonly barCaption = computed(() => this.auction()?.label ?? 'incl. shipping');

  /** Opens Place bid; a placed bid updates the panel and says so (L2-115). */
  async placeBid(): Promise<void> {
    const listing = this.store.listing();
    if (!listing?.auction) return;
    const ref = this.dialogs.open<PlaceBidDialog, PlaceBidData, PlaceBidResult>(PlaceBidDialog, {
      reference: listing.reference,
      title: listing.title,
      photo: listing.photos[0] ?? null,
      shipping: listing.shippingCost === '0.00' ? 'free' : formatMoney(listing.shippingCost),
      auction: listing.auction,
      verified: this.auth.isVerified(),
    });
    const result = await ref.closed;
    if (result === 'signed-out') {
      await this.router.navigate(['/sign-in'], { queryParams: { returnUrl: `/items/${listing.reference}` } });
    } else if (result === 'ended') {
      await this.store.load(listing.reference);
    } else if (result) {
      this.store.updateAuction(result);
      this.toasts.show({ tone: 'success', title: 'Bid placed. You’re the highest bidder' });
    }
  }

  /** Buy now on a showcase listing: no checkout; the Sold notice takes the buy bar's place and focus (L2-108). */
  buyShowcase(): void {
    void this.store.showSoldNotice();
    afterNextRender(() => this.notice()?.focus(), { injector: this.injector });
  }

  /** A visitor signs in first and comes back with the dialog open; a member opens it now (L2-109). */
  async requestItem(): Promise<void> {
    const listing = this.store.listing();
    if (!listing) return;
    if (listing.viewer.relation === 'Anonymous') {
      await this.router.navigate(['/sign-in'], { queryParams: { returnUrl: showcaseRequestUrl(listing.reference) } });
      return;
    }
    const ref = this.dialogs.open<RequestShowcaseItemDialog, RequestShowcaseItemData, RequestShowcaseItemResult>(
      RequestShowcaseItemDialog,
      {
        reference: listing.reference,
        title: listing.title,
        meta: `${formatMoney(listing.price)} · ${listing.brand}`,
        photo: listing.photos[0] ?? null,
        existing: this.store.myRequest(),
      },
    );
    const result = await ref.closed;
    if (result === 'signed-out') {
      await this.router.navigate(['/sign-in'], { queryParams: { returnUrl: showcaseRequestUrl(listing.reference) } });
    } else if (result) {
      this.store.myRequest.set(result.request);
      this.toasts.show(
        result.created
          ? { tone: 'success', title: 'Request sent', text: 'Thank you. TuTuMuch sees every request.' }
          : { tone: 'info', title: 'Request updated' },
      );
    }
  }

  /** The owner's switch; on a refusal it goes back and says so (L2-107). */
  async toggleShowcase(showcase: boolean): Promise<void> {
    if (!(await this.store.setShowcase(showcase))) {
      this.showcaseOn.set(!showcase);
      this.toasts.show({ tone: 'danger', title: 'Couldn’t change the showcase', text: 'Try again in a moment.' });
    }
  }

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
    const tick = setInterval(() => this.now.set(Date.now()), 30_000);
    inject(DestroyRef).onDestroy(() => clearInterval(tick));
    effect(() => {
      const reference = this.reference();
      untracked(() => void this.store.load(reference));
    });
    effect(() => {
      const listing = this.store.listing();
      if (listing) this.title.setTitle(`${listing.title} · ${listing.brand} · TuTuMuch`);
    });
    effect(() => {
      if (this.request() !== '1' || this.store.primaryAction() !== 'showcase') return;
      untracked(() => void this.openRequested());
    });
  }

  /** Back from sign-in with `?request=1`: the notice, and the dialog for a member. */
  private async openRequested(): Promise<void> {
    await this.store.showSoldNotice();
    if (this.store.listing()?.viewer.relation === 'OtherMember') await this.requestItem();
  }
}

/** The auction panel's inputs and the viewer's status line (L2-114, mocks pages/listing-detail auction states). */
export interface AuctionPanelView {
  label: string;
  amount: string;
  bids: string;
  timeLeft: string | null;
  soon: boolean;
  ended: boolean;
  endsAt: string;
  shipping: string;
  status: AuctionStatus | null;
}

/** What the auction panel shows to this viewer at `now`; never another bidder (L2-120). */
export function auctionPanel(listing: ListingDetail, auction: AuctionSummary, now: number): AuctionPanelView {
  const left = timeLeft(auction.endsAt, now);
  const ended = auction.ended || left.ended;
  const label = ended && auction.bidCount > 0 ? 'Winning bid' : auction.bidCount > 0 ? 'Current bid' : 'Starting bid';
  return {
    label,
    amount: formatMoney(auction.currentPrice),
    bids: auction.bidCount === 0 ? 'No bids yet' : auction.bidCount === 1 ? '1 bid' : `${auction.bidCount} bids`,
    timeLeft: ended ? null : left.text,
    soon: left.soon,
    ended,
    endsAt: auction.endsAt,
    shipping: listing.shippingCost === '0.00' ? 'Free shipping' : `${formatMoney(listing.shippingCost)} shipping`,
    status: auctionStatus(listing, auction, ended),
  };
}

function auctionStatus(listing: ListingDetail, auction: AuctionSummary, ended: boolean): AuctionStatus | null {
  if (listing.viewer.relation === 'Seller') {
    if (listing.status === 'Removed') {
      return {
        tone: 'neutral',
        heading: 'This auction ended with no bids',
        text: 'You can relist it from Your listings.',
      };
    }
    if (auction.bidCount > 0) {
      return {
        tone: 'neutral',
        heading: 'This auction has bids, so it can’t be changed',
        text: 'We’ll email you when it ends.',
      };
    }
    return null;
  }
  if (auction.viewerIsWinner && auction.payBy) {
    return {
      tone: 'success',
      heading: 'You won',
      text: `Pay by ${auctionTime(auction.payBy)}: your bid plus shipping and tax.`,
    };
  }
  if (ended) {
    return auction.bidCount > 0
      ? {
          tone: 'neutral',
          heading: 'This auction has ended',
          text: `It ended with a winning bid of ${formatMoney(auction.currentPrice)}.`,
        }
      : { tone: 'neutral', heading: 'This auction has ended', text: 'It ended with no bids.' };
  }
  if (auction.viewerIsHighestBidder) {
    return { tone: 'info', heading: 'You’re the highest bidder', text: 'We’ll email you if someone bids higher.' };
  }
  if (auction.viewerHasBid) {
    return {
      tone: 'warning',
      heading: 'You’ve been outbid',
      text: `Bid ${formatMoney(auction.minimumBid)} or more to get back in.`,
    };
  }
  return null;
}
