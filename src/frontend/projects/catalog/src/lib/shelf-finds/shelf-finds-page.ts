import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthState, FindCard, FindsApi, LaunchState, shortRef } from '@tutumuch/api';
import {
  ButtonDirective,
  EmptyStateComponent,
  FindCardComponent,
  IconComponent,
  LoadMoreComponent,
  SkeletonComponent,
} from '@tutumuch/components';

import { toFindCardView } from './find-format';

/**
 * Shelf finds at /finds (L2-114, mocks pages/shelf-finds): live finds newest first, 24 at a time
 * with Load more. "Post a shelf find" shows to a signed-in member the launch phase lets sell
 * (L2-113, L2-091). Finds are not listings and never appear in the catalog (ADR catalog/0009).
 */
@Component({
  selector: 'tl-shelf-finds-page',
  imports: [
    RouterLink,
    ButtonDirective,
    EmptyStateComponent,
    FindCardComponent,
    IconComponent,
    LoadMoreComponent,
    SkeletonComponent,
  ],
  templateUrl: './shelf-finds-page.html',
  styleUrl: './shelf-finds-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShelfFindsPage implements OnInit {
  private readonly api = inject(FindsApi);
  private readonly auth = inject(AuthState);
  private readonly launch = inject(LaunchState);

  readonly view = signal<'loading' | 'loaded' | 'error'>('loading');
  readonly finds = signal<FindCard[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly loadingMore = signal(false);
  readonly errorRef = signal('');
  readonly cards = computed(() => this.finds().map(toFindCardView));
  readonly canPost = computed(() => !!this.auth.member() && this.launch.canSell());
  readonly placeholders = [1, 2, 3, 4, 5, 6, 7, 8];

  ngOnInit(): void {
    void this.auth.load();
    void this.load();
  }

  async load(): Promise<void> {
    this.view.set('loading');
    const result = await this.api.list(1);
    if (!result.ok) {
      this.errorRef.set(shortRef(result.problem.correlationId));
      this.view.set('error');
      return;
    }
    this.finds.set(result.value.items);
    this.total.set(result.value.totalCount);
    this.page.set(1);
    this.view.set('loaded');
  }

  async loadMore(): Promise<void> {
    if (this.loadingMore()) return;
    this.loadingMore.set(true);
    const result = await this.api.list(this.page() + 1);
    this.loadingMore.set(false);
    if (!result.ok) return;
    this.finds.update((finds) => [...finds, ...result.value.items]);
    this.total.set(result.value.totalCount);
    this.page.update((page) => page + 1);
  }
}
