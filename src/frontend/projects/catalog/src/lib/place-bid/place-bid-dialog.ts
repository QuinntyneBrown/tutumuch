import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { AuctionSummary, BidsApi, CardPhoto, CatalogApi, shortRef } from '@tutumuch/api';
import {
  AlertComponent,
  ButtonDirective,
  DIALOG_DATA,
  DialogComponent,
  DialogRef,
  FormFieldComponent,
  InputGroupComponent,
  ItemRowComponent,
  timeLeft,
} from '@tutumuch/components';

/** What Place bid needs: the listing, its shipping and the auction as the page last read it. */
export interface PlaceBidData {
  reference: string;
  title: string;
  photo: CardPhoto | null;
  /** "$6.00" or "free". */
  shipping: string;
  auction: AuctionSummary;
  /** False for a member who has not verified their email (L2-002): bidding is refused with 403. */
  verified: boolean;
}

/** How the dialog closed: the bid was placed, the auction ended, or the session had ended. */
export type PlaceBidResult = AuctionSummary | 'ended' | 'signed-out';

/** "$19.00". */
export function bidMoney(value: string | number): string {
  return `$${Number(value).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

/** "Current bid $18.00 · 3 bids · Ends in 2d 4h", or "Starting bid $12.00 · No bids yet · ...". */
export function auctionMeta(auction: AuctionSummary, now = Date.now()): string {
  const left = timeLeft(auction.endsAt, now).text;
  return auction.bidCount > 0
    ? `Current bid ${bidMoney(auction.currentPrice)} · ${auction.bidCount === 1 ? '1 bid' : `${auction.bidCount} bids`} · ${left}`
    : `Starting bid ${bidMoney(auction.startingBid)} · No bids yet · ${left}`;
}

const MONEY = /^\d+(\.\d{1,2})?$/;

/**
 * "Place a bid" (L2-115, L2-116, mocks dialogs/place-bid): the listing, one "Your bid" field filled
 * with the lowest acceptable bid, and Place bid. A refusal keeps the amount and shows TuTuMuch
 * API's message on the field; OUTBID reloads the auction and fills in the new lowest bid;
 * AUCTION_ENDED says so and closes with 'ended'. Closes with the auction as the bidder now sees it.
 */
@Component({
  selector: 'tl-place-bid-dialog',
  imports: [
    AlertComponent,
    ButtonDirective,
    DialogComponent,
    FormFieldComponent,
    InputGroupComponent,
    ItemRowComponent,
  ],
  templateUrl: './place-bid-dialog.html',
  styleUrl: './place-bid-dialog.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlaceBidDialog {
  private readonly bids = inject(BidsApi);
  private readonly catalog = inject(CatalogApi);
  readonly ref = inject<DialogRef<PlaceBidResult>>(DialogRef);
  readonly data = inject<PlaceBidData>(DIALOG_DATA);

  readonly auction = signal(this.data.auction);
  readonly amount = signal(Number(this.data.auction.minimumBid).toFixed(2));
  readonly state = signal<'default' | 'busy' | 'conflict' | 'ended' | 'failed'>('default');
  readonly error = signal<string | null>(null);
  readonly failureRef = signal('');
  readonly money = bidMoney;

  readonly meta = computed(() => auctionMeta(this.auction()));
  readonly help = computed(() => `Bid ${bidMoney(this.auction().minimumBid)} or more`);
  readonly description = computed(
    () => `If you win, you pay your bid plus ${this.data.shipping} shipping and tax within 48 hours.`,
  );

  input(event: Event): void {
    this.amount.set((event.target as HTMLInputElement).value);
  }

  async submit(): Promise<void> {
    if (this.state() === 'busy') return;
    if (this.state() === 'ended') {
      this.ref.close('ended');
      return;
    }
    const text = this.amount().trim().replace(/^\$/, '');
    if (!MONEY.test(text)) {
      this.error.set(`Enter a bid of ${bidMoney(this.auction().minimumBid)} or more`);
      return;
    }
    this.error.set(null);
    this.state.set('busy');
    const result = await this.bids.placeBid(this.data.reference, Number(text));
    if (result.ok) {
      this.ref.close(result.value);
      return;
    }
    const { status, code, title, errors, correlationId } = result.problem;
    if (status === 401) {
      this.ref.close('signed-out');
    } else if (code === 'OUTBID') {
      await this.reload();
      this.state.set('conflict');
    } else if (code === 'AUCTION_ENDED') {
      this.state.set('ended');
    } else if (status === 403) {
      this.state.set('default');
      this.error.set('Verify your email to bid. Check your inbox for the link we sent.');
    } else if (status === 422 || status === 409) {
      this.state.set('default');
      this.error.set(errors['amount']?.message ?? title);
    } else {
      this.failureRef.set(shortRef(correlationId));
      this.state.set('failed');
    }
  }

  /** Reads the auction again after someone bid higher and fills in the new lowest bid (L2-116 AC2). */
  private async reload(): Promise<void> {
    const listing = await this.catalog.listing(this.data.reference);
    if (listing.ok && listing.value.auction) {
      this.auction.set(listing.value.auction);
      this.amount.set(Number(listing.value.auction.minimumBid).toFixed(2));
    }
  }
}
