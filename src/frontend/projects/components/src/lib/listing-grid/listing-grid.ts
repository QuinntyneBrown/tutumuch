import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { ListingCardComponent, ListingCardView } from '../listing-card/listing-card';

/**
 * The responsive grid of listing cards: 2 columns below 768 px, 3 from 768 px and 4 from 992 px
 * (L2-061, design system: Listing card). While loading it shows 8 skeleton cards in the same shape.
 */
@Component({
  selector: 'tl-listing-grid',
  imports: [ListingCardComponent],
  templateUrl: './listing-grid.html',
  styleUrl: './listing-grid.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ListingGridComponent {
  readonly cards = input<ListingCardView[]>([]);
  readonly loading = input(false);
  /** Leading cards treated as above the first screen. */
  readonly eagerCount = input(4);

  readonly skeletons = Array.from({ length: 8 }, (_, i) => i);
}
