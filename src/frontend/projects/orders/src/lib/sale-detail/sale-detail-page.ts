import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { RouterLink } from '@angular/router';
import { OrdersApi, ProblemsApi, Sale, shortRef } from '@tutumuch/api';
import {
  AlertComponent,
  ButtonDirective,
  CenterStateComponent,
  DialogService,
  FormFieldComponent,
  IconComponent,
  OrderSummaryComponent,
  OrderSummaryItem,
  OrderTimelineComponent,
  QuoteComponent,
  ThreadComponent,
  TrackingLinkComponent,
} from '@tutumuch/components';

import { AddTrackingData, AddTrackingDialog } from '../add-tracking/add-tracking-dialog';
import { CancelOrderData, CancelOrderDialog } from '../cancel-order/cancel-order-dialog';
import { RelistData, RelistDialog } from '../relist/relist-dialog';
import {
  RespondCancellationData,
  RespondCancellationDialog,
} from '../respond-cancellation/respond-cancellation-dialog';
import { formatMoney } from '../money';
import { formatDay, timelineSteps } from '../order-dates';
import { OrderPartyComponent } from '../order-party/order-party';
import { problemThread } from '../problem-thread';

const HEADINGS: Record<string, string> = {
  Paid: 'You made a sale',
  Shipped: 'Shipped',
  Delivered: 'Delivered',
  Completed: 'Completed',
  Cancelled: 'Cancelled',
  Refunded: 'Refunded',
};

/**
 * The seller's sale at /sales/{id} (mocks pages/sale-detail, L2-033): the ship-by date and the
 * buyer's address while Paid, with Add tracking and Cancel order; the buyer's request to cancel
 * with Respond; the timeline and tracking link once shipped; the payout after the TuTuMuch fee once
 * Completed (L2-031); Relist item once Cancelled (L2-035); and the buyer's open problem with the
 * payout on hold and one reply (L2-036).
 */
@Component({
  selector: 'tl-sale-detail-page',
  imports: [
    RouterLink,
    AlertComponent,
    ButtonDirective,
    CenterStateComponent,
    FormFieldComponent,
    IconComponent,
    OrderPartyComponent,
    OrderSummaryComponent,
    OrderTimelineComponent,
    QuoteComponent,
    ThreadComponent,
    TrackingLinkComponent,
  ],
  templateUrl: './sale-detail-page.html',
  styleUrl: './sale-detail-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SaleDetailPage {
  private readonly api = inject(OrdersApi);
  private readonly dialogs = inject(DialogService);
  private readonly problems = inject(ProblemsApi);

  /** The order id, from the route parameter. */
  readonly id = input.required<string>();

  readonly sale = signal<Sale | null>(null);
  readonly view = signal<'loading' | 'loaded' | 'error'>('loading');
  readonly errorRef = signal('');

  /** A Paid sale the buyer asked to cancel and the seller has not answered (mocks sale-detail/cancel-requested). */
  readonly requested = computed(
    () => this.sale()?.status === 'Paid' && this.sale()?.cancellationRequest?.status === 'Pending',
  );
  /** The buyer's problem waiting for TuTuMuch (mocks sale-detail/problem). */
  readonly problemOpen = computed(() => this.sale()?.problem?.status === 'Open');
  readonly heading = computed(() =>
    this.requested()
      ? 'Buyer asked to cancel'
      : this.problemOpen()
        ? 'Problem reported'
        : HEADINGS[this.sale()?.status ?? 'Paid'],
  );
  readonly thread = computed(() => {
    const sale = this.sale();
    return sale?.problem ? problemThread(sale.problem, sale.buyerDisplayName, 'You') : [];
  });
  readonly reply = signal('');
  readonly replyState = signal<'default' | 'busy' | 'failed'>('default');
  readonly replyError = signal<string | null>(null);
  readonly endedNote = computed(() => {
    const sale = this.sale();
    if (sale?.status === 'Refunded')
      return sale.problem?.resolutionReason ?? 'Our team decided the problem for the buyer.';
    if (sale?.canRelist || sale?.status !== 'Cancelled')
      return 'The listing stays off the catalog until you relist it.';
    return 'The listing is back in the catalog.';
  });
  readonly ended = computed(() => ['Cancelled', 'Refunded'].includes(this.sale()?.status ?? ''));
  readonly steps = computed(() => timelineSteps(this.sale()?.timeline ?? []));
  readonly summaryItem = computed<OrderSummaryItem | null>(() => {
    const item = this.sale()?.item;
    return item ? { title: item.title, meta: `${item.brand} · ${item.size}`, photo: item.photo } : null;
  });

  /** Where the payout of a Completed sale is (mocks sale-detail/completed). */
  readonly payoutNote = computed(() => {
    const payout = this.sale()?.payout;
    if (!payout) return '';
    const bank = payout.bankDisplay ?? 'your bank';
    if (payout.status === 'Paid' || (payout.status === 'Sent' && payout.sentAt)) {
      return `Sent ${formatDay(payout.sentAt!)} to ${bank}.`;
    }
    if (payout.status === 'Failed') return 'Your bank returned this payout.';
    return payout.status === 'Held' ? 'Held until you set up payouts.' : 'On its way soon.';
  });

  readonly day = formatDay;
  readonly money = formatMoney;

  constructor() {
    effect(() => {
      const id = this.id();
      untracked(() => void this.load(id));
    });
  }

  retry(): Promise<void> {
    return this.load(this.id());
  }

  async addTracking(): Promise<void> {
    const sale = this.sale();
    if (!sale) return;
    const ref = this.dialogs.open<AddTrackingDialog, AddTrackingData, Sale>(AddTrackingDialog, {
      orderId: sale.id,
      buyerDisplayName: sale.buyerDisplayName,
    });
    const shipped = await ref.closed;
    if (shipped) this.sale.set(shipped);
  }

  async cancelOrder(): Promise<void> {
    const sale = this.sale();
    if (!sale) return;
    const ref = this.dialogs.open<CancelOrderDialog, CancelOrderData, Sale>(CancelOrderDialog, {
      orderId: sale.id,
      buyerDisplayName: sale.buyerDisplayName,
      total: sale.total,
    });
    const cancelled = await ref.closed;
    if (cancelled) this.sale.set(cancelled);
  }

  async respond(): Promise<void> {
    const sale = this.sale();
    if (!sale) return;
    const ref = this.dialogs.open<RespondCancellationDialog, RespondCancellationData, Sale>(RespondCancellationDialog, {
      orderId: sale.id,
      buyerDisplayName: sale.buyerDisplayName,
      message: sale.cancellationRequest?.message ?? null,
      total: sale.total,
      shipBy: sale.shipBy,
    });
    const answered = await ref.closed;
    if (answered) this.sale.set(answered);
  }

  async relist(): Promise<void> {
    const sale = this.sale();
    if (!sale) return;
    const ref = this.dialogs.open<RelistDialog, RelistData, boolean>(RelistDialog, {
      item: sale.item,
      itemPrice: sale.itemPrice,
      shippingCost: sale.shippingCost,
    });
    if (await ref.closed) this.sale.set({ ...sale, canRelist: false });
  }

  /** The seller's one reply to an open problem (L2-036 AC5); it then shows in the thread. */
  async sendReply(): Promise<void> {
    const sale = this.sale();
    if (!sale || this.replyState() === 'busy') return;
    const message = this.reply().trim();
    if (!message) {
      this.replyError.set('Write your reply');
      return;
    }
    this.replyError.set(null);
    this.replyState.set('busy');
    const result = await this.problems.respondToProblem(sale.id, message);
    if (result.ok) {
      this.sale.set(result.value);
      this.replyState.set('default');
      return;
    }
    const error = result.problem.errors['message'];
    this.replyError.set(error?.message ?? null);
    this.replyState.set(error ? 'default' : 'failed');
  }

  private async load(id: string): Promise<void> {
    this.view.set('loading');
    const result = await this.api.sale(id);
    if (!result.ok) {
      this.errorRef.set(shortRef(result.problem.correlationId));
      this.view.set('error');
      return;
    }
    this.sale.set(result.value);
    this.view.set('loaded');
  }
}
