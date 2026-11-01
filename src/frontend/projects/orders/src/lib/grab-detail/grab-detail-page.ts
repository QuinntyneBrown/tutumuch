import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { Grab, GrabsApi, grabStatusLabel, grabStatusWords, shortRef } from '@tutumuch/api';
import {
  AlertComponent,
  AlertTone,
  ButtonDirective,
  CenterStateComponent,
  DialogService,
  Fact,
  FactsComponent,
  IconComponent,
  IconName,
  OrderSummaryComponent,
  OrderSummaryItem,
  SkeletonComponent,
  ToastService,
} from '@tutumuch/components';

import { CancelGrabDialog, GrabReceivedDialog } from '../grab-dialogs/grab-dialogs';
import { grabTone } from '../my-grabs/grab-text';
import { formatMoney } from '../money';
import { formatDay } from '../order-dates';

/**
 * One grab at /account/grabs/{id} (L2-119, mocks pages/grab-detail): the status in words with its
 * deadline, the find and the store, what was paid and the deposit, where it is sent, and "Cancel
 * grab" or "I've got it" when they fit. Another member's grab is the not-found state (L2-121).
 */
@Component({
  selector: 'tl-grab-detail-page',
  imports: [
    RouterLink,
    AlertComponent,
    ButtonDirective,
    CenterStateComponent,
    FactsComponent,
    IconComponent,
    OrderSummaryComponent,
    SkeletonComponent,
  ],
  templateUrl: './grab-detail-page.html',
  styleUrl: './grab-detail-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GrabDetailPage {
  private readonly api = inject(GrabsApi);
  private readonly router = inject(Router);
  private readonly dialogs = inject(DialogService);
  private readonly toasts = inject(ToastService);

  /** The grab id, from the route parameter. */
  readonly id = input.required<string>();

  readonly view = signal<'loading' | 'loaded' | 'not-found' | 'error'>('loading');
  readonly grab = signal<Grab | null>(null);
  readonly errorRef = signal('');
  readonly money = formatMoney;
  readonly day = formatDay;

  readonly words = computed(() => {
    const grab = this.grab();
    return grab ? grabStatusWords(grab, formatDay) : '';
  });
  readonly alert = computed<{ tone: AlertTone; icon: IconName }>(() => {
    switch (this.grab()?.status) {
      case 'Requested':
        return { tone: 'info', icon: 'clock' };
      case 'Accepted':
        return { tone: 'info', icon: 'map-pin' };
      case 'Bought':
        return { tone: 'success', icon: 'truck' };
      case 'Received':
        return { tone: 'success', icon: 'check-circle' };
      default:
        return { tone: 'warning', icon: 'alert' };
    }
  });
  readonly facts = computed<Fact[]>(() => {
    const grab = this.grab();
    if (!grab) return [];
    const facts: Fact[] = [
      { label: 'Store', value: `${grab.storeName}, ${grab.storeAddress}` },
      { label: 'Status', value: grabStatusLabel(grab.status), status: grabTone(grab.status) },
      { label: 'Deposit', value: formatMoney(grab.deposit), numeric: true },
    ];
    if (grab.refundedAmount) facts.push({ label: 'Refunded', value: formatMoney(grab.refundedAmount), numeric: true });
    return facts;
  });
  readonly summaryItem = computed<OrderSummaryItem | null>(() => {
    const grab = this.grab();
    return grab
      ? { title: grab.findTitle, meta: `${grab.storeName} · ${grab.findReference}`, photo: null, icon: 'map-pin' }
      : null;
  });

  constructor() {
    effect(() => {
      const id = this.id();
      untracked(() => void this.load(id));
    });
  }

  async load(id = this.id()): Promise<void> {
    this.view.set('loading');
    const result = await this.api.get(id);
    if (result.ok) {
      this.grab.set(result.value);
      this.view.set('loaded');
      return;
    }
    if (result.problem.status === 401) {
      await this.router.navigate(['/sign-in'], { queryParams: { returnUrl: `/account/grabs/${id}` } });
    } else if (result.problem.status === 404) {
      this.view.set('not-found');
    } else {
      this.errorRef.set(shortRef(result.problem.correlationId));
      this.view.set('error');
    }
  }

  async cancel(): Promise<void> {
    const grab = this.grab();
    if (!grab) return;
    const cancelled = await this.dialogs.open<CancelGrabDialog, Grab, Grab>(CancelGrabDialog, grab).closed;
    if (cancelled) {
      this.grab.set(cancelled);
      this.toasts.show({
        tone: 'info',
        title: 'Grab cancelled',
        text: `${formatMoney(cancelled.refundedAmount ?? '0')} is on its way back to your card.`,
      });
    } else {
      await this.load();
    }
  }

  async received(): Promise<void> {
    const grab = this.grab();
    if (!grab) return;
    const done = await this.dialogs.open<GrabReceivedDialog, Grab, Grab>(GrabReceivedDialog, grab).closed;
    if (done) this.grab.set(done);
  }
}
