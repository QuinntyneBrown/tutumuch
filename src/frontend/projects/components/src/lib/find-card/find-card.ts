import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

import { IconComponent } from '../icon/icon';

/** What one find card shows. price is formatted for display, for example "$40.00"; seenOn is "YYYY-MM-DD". */
export interface FindCardView {
  href: string;
  title: string;
  category: string;
  store: string;
  seenOn: string;
  /** "Seen Sat 24 Oct", formatted by the caller. */
  seenLabel: string;
  price: string;
  grabAvailable: boolean;
}

/**
 * One shelf find in a grid (design system: Find card, L2-114, ADR catalog/0009): a find has no
 * photo, so a sunken 4:3 well holds the map-pin; then the title on one line, the category, the
 * store, "Seen {date}" and the shelf price, pushed down so the price rows of a grid row line up,
 * with the "Grab available" badge when TuTuMuch offers a grab. Text is bound as text, never as HTML.
 */
@Component({
  selector: 'tl-find-card',
  imports: [RouterLink, IconComponent],
  templateUrl: './find-card.html',
  styles: ':host { display: flex; flex: 1; min-width: 0; }',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FindCardComponent {
  readonly card = input.required<FindCardView>();
}
