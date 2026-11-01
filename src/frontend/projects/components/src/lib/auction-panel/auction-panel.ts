import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { IconComponent, IconName } from '../icon/icon';
import { auctionTime } from './time-left';

/** The tone of the viewer's status line: highest bidder, outbid, won, or a plain notice. */
export type AuctionStatusTone = 'info' | 'warning' | 'success' | 'neutral';

/** The viewer's own standing in the auction; never another bidder (L2-120). */
export interface AuctionStatus {
  tone: AuctionStatusTone;
  heading: string;
  text: string;
}

const ICONS: Record<AuctionStatusTone, IconName> = {
  info: 'info',
  warning: 'alert',
  success: 'check-circle',
  neutral: 'clock',
};

/**
 * The bid block of an auction's detail page (design system: Auction panel, L2-114): the label
 * ("Current bid", "Starting bid" or "Winning bid") with the amount, the bid count and time left,
 * the end date and time in the viewer's time zone with the shipping, and the viewer's status line,
 * announced politely. It takes the price block's place; Place bid and Pay now stay in the buy bar.
 */
@Component({
  selector: 'tl-auction-panel',
  imports: [IconComponent],
  templateUrl: './auction-panel.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuctionPanelComponent {
  readonly label = input.required<string>();
  /** The formatted amount, for example "$18.00". */
  readonly amount = input.required<string>();
  /** "No bids yet", "1 bid" or "3 bids". */
  readonly bids = input.required<string>();
  /** "Ends in 2d 4h"; null once ended. */
  readonly timeLeft = input<string | null>(null);
  /** Under an hour left: the time left is in the warning colour. */
  readonly soon = input(false);
  readonly endsAt = input.required<string>();
  readonly ended = input(false);
  /** "$6.00 shipping" or "Free shipping". */
  readonly shipping = input.required<string>();
  readonly status = input<AuctionStatus | null>(null);

  readonly endText = computed(() => auctionTime(this.endsAt()));
  readonly icon = computed(() => ICONS[this.status()?.tone ?? 'neutral']);
}
