import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

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
  /** Sold out: the photo is greyed and stamped "Sold" (page guidelines 9). */
  sold?: boolean;
  photo: ListingCardPhoto | null;
}

/**
 * One listing in a grid (design system: Listing card, L2-096): the photo in a white 4:5 well, the
 * title on one line, the brand (L2-022), "{size} · {condition}" and the price, pushed to the bottom
 * so the price rows of a grid row line up. A "Reserved" stamp marks an item in a checkout; a sold
 * item is greyed and stamped "Sold". Text is bound as text, never as HTML.
 */
@Component({
  selector: 'tl-listing-card',
  imports: [RouterLink],
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
