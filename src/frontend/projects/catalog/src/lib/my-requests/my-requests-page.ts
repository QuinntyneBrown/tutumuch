import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { ApiProblem, ItemRequest, ItemRequestsApi, MyItemRequests, shortRef } from '@tutumuch/api';
import {
  ButtonDirective,
  DialogService,
  EmptyStateComponent,
  IconComponent,
  ListingGridComponent,
  SkeletonComponent,
  StatusComponent,
  ToastService,
  type ListingCardView,
} from '@tutumuch/components';

import { toCardView } from '../card-view';
import { DeleteRequestData, DeleteRequestDialog } from '../delete-request/delete-request-dialog';
import { needsSignIn, requestDay, requestFacts } from './request-view';

/** One request block as the page shows it. */
interface RequestRow {
  request: ItemRequest;
  facts: string;
  open: boolean;
  day: string;
  cards: ListingCardView[];
  note: string | null;
}

/**
 * The member's item requests at /account/requests (L2-098, L2-099, L2-102, mocks
 * pages/my-requests): newest first, each with its facts, "Open until" or Expired with Renew, Edit
 * and Delete, and its matched listings still Active. At the limit the header action gives way to
 * a line. A visitor is sent to sign in and back (L2-105).
 */
@Component({
  selector: 'tl-my-requests-page',
  imports: [
    RouterLink,
    ButtonDirective,
    EmptyStateComponent,
    IconComponent,
    ListingGridComponent,
    SkeletonComponent,
    StatusComponent,
  ],
  templateUrl: './my-requests-page.html',
  styleUrl: './my-requests-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyRequestsPage {
  private readonly api = inject(ItemRequestsApi);
  private readonly router = inject(Router);
  private readonly dialogs = inject(DialogService);
  private readonly toasts = inject(ToastService);

  readonly view = signal<'loading' | 'loaded' | 'error'>('loading');
  readonly errorRef = signal('');
  readonly data = signal<MyItemRequests | null>(null);
  readonly renewing = signal<string | null>(null);

  readonly rows = computed<RequestRow[]>(() => (this.data()?.items ?? []).map(toRow));
  readonly atLimit = computed(() => {
    const data = this.data();
    return !!data && data.openCount >= data.limit;
  });
  readonly subtitle = computed(() => {
    const open = this.data()?.openCount ?? 0;
    return `${open} open ${open === 1 ? 'request' : 'requests'}`;
  });

  constructor() {
    void this.load();
  }

  retry(): Promise<void> {
    return this.load();
  }

  /** Saves an expired request unchanged, which renews it for 90 days (L2-098). */
  async renew(request: ItemRequest): Promise<void> {
    if (this.renewing()) return;
    this.renewing.set(request.id);
    const result = await this.api.update(request.id, {
      category: request.category,
      keywords: request.keywords,
      maxPrice: request.maxPrice,
      minimumCondition: request.minimumCondition,
    });
    this.renewing.set(null);
    if (result.ok) {
      this.toasts.show({ tone: 'success', title: 'Request saved', text: 'We’ll email you when it’s listed.' });
      await this.load(false);
      return;
    }
    if (this.toSignIn(result.problem)) return;
    if (result.problem.code === 'REQUEST_LIMIT_REACHED') {
      this.toasts.show({ tone: 'warning', title: result.problem.title, text: 'Delete one to renew this one.' });
      return;
    }
    this.toasts.show({
      tone: 'danger',
      title: 'Something went wrong',
      text: shortRef(result.problem.correlationId),
      action: { label: 'Try again', run: () => void this.renew(request) },
    });
  }

  async remove(request: ItemRequest): Promise<void> {
    const ref = this.dialogs.open<DeleteRequestDialog, DeleteRequestData, boolean>(DeleteRequestDialog, {
      id: request.id,
      keywords: request.keywords,
    });
    if (!(await ref.closed)) return;
    this.toasts.show({ tone: 'info', title: 'Request deleted' });
    await this.load(false);
  }

  private async load(showLoading = true): Promise<void> {
    if (showLoading) this.view.set('loading');
    const result = await this.api.list();
    if (!result.ok) {
      if (this.toSignIn(result.problem)) return;
      this.errorRef.set(shortRef(result.problem.correlationId));
      this.view.set('error');
      return;
    }
    this.data.set(result.value);
    this.view.set('loaded');
  }

  private toSignIn(problem: ApiProblem): boolean {
    if (!needsSignIn(problem)) return false;
    void this.router.navigate(['/sign-in'], { queryParams: { returnUrl: '/account/requests' } });
    return true;
  }
}

function toRow(request: ItemRequest): RequestRow {
  const open = request.status === 'Open';
  const cards = request.matches.map(toCardView);
  let note: string | null = null;
  if (!cards.length) {
    if (!open) note = 'Renew it to keep looking for 90 more days.';
    else if (request.matchCount > 0) note = 'Your match is no longer available. We’ll keep looking.';
    else note = 'No matches yet. We’ll email you when one is listed.';
  }
  return { request, facts: requestFacts(request), open, day: requestDay(request.expiresAt), cards, note };
}
