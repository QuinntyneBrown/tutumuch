import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

import { SizeTagComponent } from '../size-tag/size-tag';

/** A listing photo as a picture: WebP and JPEG srcsets with the original dimensions (L2-059). */
export interface ListingCardPhoto {
  width: number;
  height: number;
  alt: string;
  src: string;
  webp: string;
  jpeg: string;
}

/** What one card shows. price is already formatted for display, for example "$85". */
export interface ListingCardView {
  href: string;
  title: string;
  brand: string;
  sizeLabel: string;
  condition: string;
  price: string;
  reserved: boolean;
  photo: ListingCardPhoto | null;
}

/**
 * One listing in a grid: cover photo with the size tag, brand, title, condition and price, and a
 * "Reserved" stamp when the item is in a checkout (design system: Listing card). Text is bound
 * as text, never as HTML.
 */
@Component({
  selector: 'tl-listing-card',
  imports: [RouterLink, SizeTagComponent],
  templateUrl: './listing-card.html',
  styleUrl: './listing-card.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ListingCardComponent {
  readonly card = input.required<ListingCardView>();
  /** Above the first screen: load eagerly with high priority; otherwise lazily. */
  readonly priority = input(false);

  /** The width a card fills at each breakpoint, so a 375 px phone picks the 400 w version. */
  readonly sizes = '(min-width: 1200px) 270px, (min-width: 992px) 23vw, (min-width: 768px) 31vw, 50vw';
}
