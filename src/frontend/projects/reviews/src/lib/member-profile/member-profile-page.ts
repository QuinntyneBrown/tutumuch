import { ChangeDetectionStrategy, Component, computed, effect, inject, input, untracked } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { ListingCard } from '@tutumuch/api';
import {
  AlertComponent,
  ButtonDirective,
  CenterStateComponent,
  EmptyStateComponent,
  IconComponent,
  ListingCardView,
  ListingGridComponent,
  LoadMoreComponent,
  RatingSummaryComponent,
  SkeletonComponent,
  StatComponent,
} from '@tutumuch/components';

import { MemberProfileStore } from './member-profile-store';

/** "$85" for whole dollars, "$18.50" otherwise, as the catalog cards show prices. */
function formatPrice(price: string): string {
  const value = Number(price);
  const cents = Math.round(value * 100) % 100 !== 0;
  return `$${value.toLocaleString('en-US', { minimumFractionDigits: cents ? 2 : 0, maximumFractionDigits: cents ? 2 : 0 })}`;
}

/** A listing card as tl-listing-card shows it, linking to the listing detail. */
function toCardView(card: ListingCard): ListingCardView {
  return {
    href: `/items/${card.reference}`,
    title: card.title,
    brand: card.brand,
    sizeLabel: card.sizeLabel,
    condition: card.condition,
    price: formatPrice(card.price),
    reserved: card.status === 'Reserved',
    photo: card.coverPhoto,
  };
}

/** "Jan 2025" from the first day of the joining month, read in UTC so no time zone shifts it. */
export function memberSince(iso: string): string {
  return new Date(`${iso}T00:00:00Z`).toLocaleDateString('en-US', { month: 'short', year: 'numeric', timeZone: 'UTC' });
}

/**
 * The public profile at /members/{displayName} (mocks pages/profile): avatar, name, location,
 * rating summary and bio, the Sales and Member since stats, the Active listings 24 at a time and
 * the Reviews section, which shows "No reviews yet" while review authoring is deferred. Public
 * fields only (L2-041, L2-054); all member text is bound as text, never as HTML.
 */
@Component({
  selector: 'tl-member-profile-page',
  imports: [
    RouterLink,
    AlertComponent,
    ButtonDirective,
    CenterStateComponent,
    EmptyStateComponent,
    IconComponent,
    ListingGridComponent,
    LoadMoreComponent,
    RatingSummaryComponent,
    StatComponent,
    SkeletonComponent,
  ],
  providers: [MemberProfileStore],
  templateUrl: './member-profile-page.html',
  styleUrl: './member-profile-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MemberProfilePage {
  readonly store = inject(MemberProfileStore);
  private readonly title = inject(Title);

  /** From the route parameter. */
  readonly displayName = input.required<string>();

  readonly cards = computed(() => this.store.listings().map(toCardView));
  readonly initials = computed(() =>
    (this.store.profile()?.displayName ?? '')
      .replace(/[^a-z0-9]/gi, '')
      .slice(0, 2)
      .toUpperCase(),
  );
  readonly since = computed(() => {
    const profile = this.store.profile();
    return profile ? memberSince(profile.memberSince) : '';
  });

  constructor() {
    effect(() => {
      const displayName = this.displayName();
      untracked(() => void this.store.load(displayName));
    });
    effect(() => {
      const profile = this.store.profile();
      if (profile) this.title.setTitle(`${profile.displayName} · TuTuMuch`);
    });
  }
}
