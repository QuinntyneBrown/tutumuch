import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';

import { IconComponent } from '../icon/icon';

/** The public reputation of a seller; ratingAverage is null until the seller has reviews. */
export interface SellerSummaryView {
  displayName: string;
  completedSalesCount: number;
  ratingAverage: number | null;
  publishedReviewCount: number;
}

/**
 * Avatar, display name linking to the profile, completed sales, and the rating, or "New seller"
 * when the seller has no completed sales (design system: Seller summary, L2-025).
 */
@Component({
  selector: 'tm-seller-summary',
  imports: [DecimalPipe, RouterLink, IconComponent],
  templateUrl: './seller-summary.html',
  styleUrl: './seller-summary.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SellerSummaryComponent {
  readonly seller = input.required<SellerSummaryView>();

  readonly initials = computed(() =>
    this.seller()
      .displayName.replace(/[^a-z0-9]/gi, '')
      .slice(0, 2)
      .toUpperCase(),
  );
  readonly isNew = computed(() => this.seller().completedSalesCount === 0);
  readonly stars = [1, 2, 3, 4, 5];
}
