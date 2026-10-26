import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { IconComponent } from '../icon/icon';

/** A member's rating: the average to one decimal place, or null, and the published review count (L2-040). */
export interface RatingSummaryView {
  average: number | null;
  count: number;
}

/**
 * Stars, the average to one decimal place and "({count} reviews)", named as one image; the muted
 * "No reviews yet" when there are none (design system: Rating summary, L2-040).
 */
@Component({
  selector: 'tm-rating-summary',
  imports: [DecimalPipe, IconComponent],
  templateUrl: './rating-summary.html',
  styleUrl: './rating-summary.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RatingSummaryComponent {
  readonly rating = input.required<RatingSummaryView>();

  readonly reviews = computed(() => (this.rating().count === 1 ? 'review' : 'reviews'));
  readonly label = computed(() => {
    const { average, count } = this.rating();
    return `Rated ${(average ?? 0).toFixed(1)} out of 5 from ${count} ${this.reviews()}`;
  });
  readonly stars = [1, 2, 3, 4, 5];
}
