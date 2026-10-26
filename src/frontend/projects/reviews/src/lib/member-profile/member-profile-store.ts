import { Injectable, inject, signal } from '@angular/core';
import { ListingCard, MembersApi, PublicProfile, shortRef } from '@tutumuch/api';

export type MemberProfileView = 'loading' | 'loaded' | 'unavailable' | 'error';

/**
 * Signal state of one public profile (view-member-profile). The profile and the first page of
 * listings load in parallel; a 404 is the unavailable view and any other failure the error view
 * with its reference. loadMore() appends the next 24 listings.
 */
@Injectable()
export class MemberProfileStore {
  private readonly api = inject(MembersApi);
  private displayName = '';
  private page = 1;

  readonly profile = signal<PublicProfile | null>(null);
  readonly listings = signal<ListingCard[]>([]);
  readonly totalListings = signal(0);
  readonly view = signal<MemberProfileView>('loading');
  readonly errorRef = signal('');
  readonly loadingMore = signal(false);
  readonly moreErrorRef = signal<string | null>(null);

  async load(displayName: string): Promise<void> {
    this.displayName = displayName;
    this.page = 1;
    this.view.set('loading');
    this.moreErrorRef.set(null);
    const [profile, listings] = await Promise.all([this.api.profile(displayName), this.api.listings(displayName, 1)]);
    const failed = !profile.ok ? profile.problem : !listings.ok ? listings.problem : null;
    if (failed || !profile.ok || !listings.ok) {
      this.profile.set(null);
      this.errorRef.set(shortRef(failed?.correlationId ?? null));
      this.view.set(failed?.status === 404 ? 'unavailable' : 'error');
      return;
    }
    this.profile.set(profile.value);
    this.listings.set(listings.value.items);
    this.totalListings.set(listings.value.totalCount);
    this.view.set('loaded');
  }

  retry(): Promise<void> {
    return this.load(this.displayName);
  }

  async loadMore(): Promise<void> {
    if (this.loadingMore()) return;
    this.loadingMore.set(true);
    this.moreErrorRef.set(null);
    const result = await this.api.listings(this.displayName, this.page + 1);
    this.loadingMore.set(false);
    if (!result.ok) {
      this.moreErrorRef.set(shortRef(result.problem.correlationId));
      return;
    }
    this.page += 1;
    const shown = new Set(this.listings().map((card) => card.reference));
    this.listings.update((cards) => [...cards, ...result.value.items.filter((card) => !shown.has(card.reference))]);
    this.totalListings.set(result.value.totalCount);
  }
}
