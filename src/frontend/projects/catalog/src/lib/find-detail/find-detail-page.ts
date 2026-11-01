import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { Router, RouterLink } from '@angular/router';
import { AuthState, FindDetail, FindsApi, mapsUrl, shortRef } from '@tutumuch/api';
import {
  AlertComponent,
  ButtonDirective,
  CenterStateComponent,
  DialogService,
  Fact,
  FactsComponent,
  IconComponent,
  SkeletonComponent,
  SoldNoticeComponent,
  ToastService,
} from '@tutumuch/components';

import { MarkFindGoneDialog } from '../find-dialogs/mark-find-gone-dialog';
import { WithdrawFindDialog } from '../find-dialogs/withdraw-find-dialog';
import { findDay, shelfPrice } from '../shelf-finds/find-format';

/**
 * One shelf find at /finds/{reference} (L2-115, L2-116, mocks pages/find-detail): the store, its
 * address with "Open in maps", the shelf price and the day it was seen, the "Grab it for me" box
 * when TuTuMuch offers a grab, and "Mark as gone" and "Withdraw" for the poster. A gone or expired
 * find shows the Gone notice; a withdrawn or unknown one the not-found state. Nothing names who
 * asked for a grab (L2-121).
 */
@Component({
  selector: 'tl-find-detail-page',
  imports: [
    RouterLink,
    AlertComponent,
    ButtonDirective,
    CenterStateComponent,
    FactsComponent,
    IconComponent,
    SkeletonComponent,
    SoldNoticeComponent,
  ],
  templateUrl: './find-detail-page.html',
  styleUrl: './find-detail-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FindDetailPage {
  private readonly api = inject(FindsApi);
  private readonly auth = inject(AuthState);
  private readonly title = inject(Title);
  private readonly dialogs = inject(DialogService);
  private readonly router = inject(Router);
  private readonly toasts = inject(ToastService);

  /** From the route parameter. */
  readonly reference = input.required<string>();

  readonly view = signal<'loading' | 'loaded' | 'not-found' | 'error'>('loading');
  readonly find = signal<FindDetail | null>(null);
  readonly errorRef = signal('');
  readonly price = shelfPrice;
  readonly day = findDay;

  readonly maps = computed(() => {
    const find = this.find();
    return find ? mapsUrl(find.storeName, find.storeAddress) : '';
  });
  readonly facts = computed<Fact[]>(() => {
    const find = this.find();
    if (!find) return [];
    return [
      { label: 'Store', value: find.storeName },
      { label: 'Address', value: find.storeAddress },
      { label: 'Seen', value: `Seen in store on ${findDay(find.seenOn)}` },
    ];
  });
  /** A visitor signs in first and comes back to the grab page (L2-117 AC9). */
  readonly grabLink = computed(() => {
    const path = `/finds/${this.reference()}/grab`;
    return this.auth.member() ? { path, query: null } : { path: '/sign-in', query: { returnUrl: path } };
  });

  constructor() {
    effect(() => {
      const reference = this.reference();
      untracked(() => void this.load(reference));
    });
  }

  async load(reference = this.reference()): Promise<void> {
    this.view.set('loading');
    void this.auth.load();
    const result = await this.api.get(reference);
    if (result.ok) {
      this.find.set(result.value);
      this.title.setTitle(`${result.value.title} · Shelf finds · TuTuMuch`);
      this.view.set('loaded');
      return;
    }
    if (result.problem.status === 404) {
      this.view.set('not-found');
      return;
    }
    this.errorRef.set(shortRef(result.problem.correlationId));
    this.view.set('error');
  }

  async markGone(): Promise<void> {
    const ref = this.dialogs.open<MarkFindGoneDialog, string, FindDetail>(MarkFindGoneDialog, this.reference());
    const gone = await ref.closed;
    if (gone) {
      this.find.set(gone);
      this.toasts.show({ tone: 'info', title: 'Marked as gone' });
    }
  }

  async withdraw(): Promise<void> {
    const ref = this.dialogs.open<WithdrawFindDialog, string, boolean>(WithdrawFindDialog, this.reference());
    if (await ref.closed) {
      this.toasts.show({ tone: 'info', title: 'Find withdrawn' });
      await this.router.navigate(['/finds']);
    }
  }
}
