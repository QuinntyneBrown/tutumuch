import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CheckoutApi, Purchase, shortRef } from '@tutumuch/api';
import {
  AlertComponent,
  ButtonDirective,
  CenterStateComponent,
  DialogService,
  IconComponent,
  OrderSummaryComponent,
  OrderSummaryItem,
  OrderTimelineComponent,
  QuoteComponent,
  SkeletonComponent,
  ThreadComponent,
  TrackingLinkComponent,
} from '@tutumuch/components';

import { formatMoney } from '../money';
import { formatDay, timelineSteps } from '../order-dates';
import { ConfirmReceiptData, ConfirmReceiptDialog } from '../confirm-receipt/confirm-receipt-dialog';
import { OrderPartyComponent } from '../order-party/order-party';
import { problemThread } from '../problem-thread';
import { ReportProblemData, ReportProblemDialog } from '../report-problem/report-problem-dialog';
import {
  RequestCancellationData,
  RequestCancellationDialog,
} from '../request-cancellation/request-cancellation-dialog';

const HEADINGS: Record<string, string> = {
  Paid: 'Waiting to ship',
  Shipped: 'On its way',
  Delivered: 'Delivered',
  Completed: 'Completed',
  Cancelled: 'Cancelled',
  Refunded: 'Refunded',
};

/**
 * The buyer's order at /orders/{id} (mocks pages/order-detail, L2-032, L2-033): waiting to ship
 * with the ship-by date, on its way with the tracking link, delivered with the automatic completion
 * date, completed, or cancelled and refunded with the seller's note. A Shipped or Delivered order
 * offers "I received it" (L2-034) and "Report a problem" within its window; an open problem shows
 * the thread and holds the order, and a problem decided for the buyer shows the refund (L2-036). A
 * Paid order offers "Ask to cancel" once (L2-035).
 */
@Component({
  selector: 'tl-order-detail-page',
  imports: [
    RouterLink,
    AlertComponent,
    ButtonDirective,
    CenterStateComponent,
    IconComponent,
    OrderPartyComponent,
    OrderSummaryComponent,
    OrderTimelineComponent,
    QuoteComponent,
    ThreadComponent,
    TrackingLinkComponent,
    SkeletonComponent,
  ],
  templateUrl: './order-detail-page.html',
  styleUrl: './order-detail-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrderDetailPage {
  private readonly api = inject(CheckoutApi);
  private readonly dialogs = inject(DialogService);

  /** The order id, from the route parameter. */
  readonly id = input.required<string>();

  readonly order = signal<Purchase | null>(null);
  readonly view = signal<'loading' | 'loaded' | 'error'>('loading');
  readonly errorRef = signal('');

  /** A problem waiting for TuTuMuch (mocks order-detail/problem). */
  readonly problemOpen = computed(() => this.order()?.problem?.status === 'Open');
  /** Refunded because TuTuMuch decided the problem for the buyer (mocks order-detail/refunded). */
  readonly refundedOnProblem = computed(
    () => this.order()?.status === 'Refunded' && this.order()?.problem?.outcome === 'FullRefund',
  );
  readonly heading = computed(() => (this.problemOpen() ? 'We’re on it' : HEADINGS[this.order()?.status ?? 'Paid']));
  readonly canConfirm = computed(
    () => ['Shipped', 'Delivered'].includes(this.order()?.status ?? '') && !this.problemOpen(),
  );
  readonly thread = computed(() => {
    const order = this.order();
    return order?.problem ? problemThread(order.problem, 'You', order.sellerDisplayName) : [];
  });
  readonly ended = computed(() => ['Cancelled', 'Refunded'].includes(this.order()?.status ?? ''));
  readonly canAskToCancel = computed(() => this.order()?.status === 'Paid' && !this.order()?.cancellationRequest);
  /** Where the buyer's request to cancel a Paid order stands (mocks order-detail/cancel-requested). */
  readonly requestNote = computed(() => {
    const order = this.order();
    if (order?.status !== 'Paid' || !order.cancellationRequest) return '';
    return order.cancellationRequest.status === 'Declined'
      ? `${order.sellerDisplayName} declined to cancel. They still ship by ${formatDay(order.shipBy)}.`
      : `You asked ${order.sellerDisplayName} to cancel. They haven’t answered yet.`;
  });
  readonly steps = computed(() => timelineSteps(this.order()?.timeline ?? []));
  readonly refund = computed(() => {
    const order = this.order();
    if (!order) return '';
    const card = order.cardBrand && order.cardLast4 ? ` to ${order.cardBrand} ending ${order.cardLast4}` : '';
    return `${formatMoney(order.total)} back${card}. Allow 5–10 days.`;
  });
  readonly problemRefund = computed(() => {
    const order = this.order();
    if (!order) return '';
    const card = order.cardBrand && order.cardLast4 ? `${order.cardBrand} ending ${order.cardLast4}` : 'your card';
    return `Our team reviewed the problem. ${formatMoney(order.total)} is on its way back to ${card}.`;
  });
  readonly summaryItem = computed<OrderSummaryItem | null>(() => {
    const item = this.order()?.item;
    return item ? { title: item.title, meta: `${item.brand} · ${item.size}`, photo: item.photo } : null;
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

  async confirmReceipt(): Promise<void> {
    const order = this.order();
    if (!order) return;
    const ref = this.dialogs.open<ConfirmReceiptDialog, ConfirmReceiptData, Purchase>(ConfirmReceiptDialog, {
      orderId: order.id,
      item: order.item,
      sellerDisplayName: order.sellerDisplayName,
    });
    const completed = await ref.closed;
    if (completed) this.order.set(completed);
  }

  async reportProblem(): Promise<void> {
    const order = this.order();
    if (!order) return;
    const ref = this.dialogs.open<ReportProblemDialog, ReportProblemData, unknown>(ReportProblemDialog, {
      orderId: order.id,
      deadline: order.problemDeadline ? formatDay(order.problemDeadline) : null,
    });
    if (await ref.closed) await this.load(order.id);
  }

  async askToCancel(): Promise<void> {
    const order = this.order();
    if (!order) return;
    const ref = this.dialogs.open<RequestCancellationDialog, RequestCancellationData, Purchase>(
      RequestCancellationDialog,
      { orderId: order.id, sellerDisplayName: order.sellerDisplayName },
    );
    const requested = await ref.closed;
    if (requested) this.order.set(requested);
  }

  private async load(id: string): Promise<void> {
    this.view.set('loading');
    const result = await this.api.purchase(id);
    if (!result.ok) {
      this.errorRef.set(shortRef(result.problem.correlationId));
      this.view.set('error');
      return;
    }
    this.order.set(result.value);
    this.view.set('loaded');
  }
}
