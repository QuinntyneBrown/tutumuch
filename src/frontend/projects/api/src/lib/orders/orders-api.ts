import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

import { API_BASE_URL } from '../api-base-url';
import { ApiResult, toResult } from '../api-result';
import { CheckoutItem, Purchase, ShippingAddress } from './checkout-api';
import { SalePayout } from './payouts-api';
import type { OrderProblem } from './problems-api';

export type OrderStatus = 'Paid' | 'Shipped' | 'Delivered' | 'Completed' | 'Cancelled' | 'Refunded';

/** One step of the order timeline (Paid, Shipped, Delivered, Completed) and when it was reached. */
export interface OrderTimelineStep {
  status: string;
  at: string | null;
}

/** The carrier, the tracking number as stored and the carrier's tracking page. */
export interface Tracking {
  carrier: string;
  number: string;
  url: string;
}

/** The reasons a seller can give for cancelling (L2-035), with the labels the Cancel order dialog shows. */
export const CANCELLATION_REASONS = [
  { value: 'ItemDamaged', label: 'Item damaged' },
  { value: 'NoLongerHave', label: 'No longer have it' },
  { value: 'ListedByMistake', label: 'Listed by mistake' },
  { value: 'BuyerAskedToCancel', label: 'Buyer asked me to cancel' },
] as const;

/** Why a cancelled order was cancelled, and the seller's note to the buyer. */
export interface Cancellation {
  reason: string;
  reasonLabel: string;
  note: string | null;
}

/** The buyer's one request to cancel a Paid order (L2-035). */
export interface CancellationRequest {
  message: string | null;
  status: 'Pending' | 'Accepted' | 'Declined';
  requestedAt: string;
}

/**
 * One order as its seller's sale page shows it (L2-033): ship-by date, buyer's address, tracking,
 * the payout (L2-031), and the cancellation, the buyer's request and whether the item can be relisted (L2-035).
 */
export interface Sale {
  id: string;
  reference: string;
  status: OrderStatus;
  paidAt: string;
  shipBy: string;
  item: CheckoutItem;
  itemPrice: string;
  shippingCost: string;
  salesTax: string;
  total: string;
  buyerDisplayName: string;
  /** Null 30 days after the order ended: the address is no longer shown (L2-037). */
  shipTo: ShippingAddress | null;
  timeline: OrderTimelineStep[];
  tracking: Tracking | null;
  payout: SalePayout | null;
  cancellation: Cancellation | null;
  cancellationRequest: CancellationRequest | null;
  canRelist: boolean;
  /** The buyer's problem with the seller's reply (L2-036). */
  problem: OrderProblem | null;
}

/** One row of "Purchases" or "Sales" (L2-037); shipBy only on a Paid sale. */
export interface OrderRow {
  id: string;
  reference: string;
  status: OrderStatus;
  item: CheckoutItem;
  total: string;
  otherParty: string;
  paidAt: string;
  shipBy: string | null;
}

/** The member's purchases or sales newest first, with the counts the page subtitle reads. */
export interface OrderList {
  items: OrderRow[];
  counts: { onTheWay: number; toConfirm: number; toShip: number };
}

/** The carriers a seller can choose in Add tracking. */
export const CARRIERS = ['USPS', 'UPS', 'FedEx', 'DHL'] as const;

/**
 * Typed client for order history (purchases and sales lists) and order actions: the seller's sale and tracking (ship-order), the buyer's receipt
 * (complete-order), cancelling, asking to cancel and answering a request (cancel-order).
 */
@Injectable({ providedIn: 'root' })
export class OrdersApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  /** The signed-in member's purchases, newest first (L2-037). */
  purchases(): Promise<ApiResult<OrderList>> {
    return toResult(this.http.get<OrderList>(`${this.baseUrl}/purchases`));
  }

  /** The signed-in member's sales, newest first (L2-037). */
  sales(): Promise<ApiResult<OrderList>> {
    return toResult(this.http.get<OrderList>(`${this.baseUrl}/sales`));
  }

  /** 404 ORDER_NOT_FOUND for anyone but the order's seller. */
  sale(id: string): Promise<ApiResult<Sale>> {
    return toResult(this.http.get<Sale>(`${this.baseUrl}/sales/${id}`));
  }

  /**
   * Ships a Paid order. 422 TRACKING_INVALID with a carrier or trackingNumber field error; 409
   * ORDER_INVALID_TRANSITION when the order is no longer Paid.
   */
  addTracking(id: string, carrier: string, trackingNumber: string): Promise<ApiResult<Sale>> {
    return toResult(this.http.post<Sale>(`${this.baseUrl}/orders/${id}/shipment`, { carrier, trackingNumber }));
  }

  /**
   * The buyer confirms receipt of a Shipped or Delivered order, which becomes Completed (L2-034).
   * 409 ORDER_INVALID_TRANSITION in any other status; 404 for anyone but the buyer.
   */
  confirmReceipt(id: string): Promise<ApiResult<Purchase>> {
    return toResult(this.http.post<Purchase>(`${this.baseUrl}/orders/${id}/receipt`, null));
  }

  /**
   * The seller cancels a Paid order; the buyer is refunded in full (L2-035). 422 CANCELLATION_INVALID
   * with a reason or note field error; 409 ORDER_INVALID_TRANSITION when it is no longer Paid.
   */
  cancelOrder(id: string, reason: string, note: string): Promise<ApiResult<Sale>> {
    return toResult(this.http.post<Sale>(`${this.baseUrl}/orders/${id}/cancellation`, { reason, note }));
  }

  /** The buyer asks the seller to cancel, once. 409 CANCELLATION_ALREADY_REQUESTED or ORDER_INVALID_TRANSITION. */
  requestCancellation(id: string, message: string): Promise<ApiResult<Purchase>> {
    return toResult(this.http.post<Purchase>(`${this.baseUrl}/orders/${id}/cancellation-request`, { message }));
  }

  /** The seller accepts (cancel and refund) or declines the buyer's request. 409 CANCELLATION_NO_PENDING_REQUEST. */
  respondToCancellation(id: string, decision: 'Accept' | 'Decline'): Promise<ApiResult<Sale>> {
    return toResult(this.http.post<Sale>(`${this.baseUrl}/orders/${id}/cancellation-request/response`, { decision }));
  }

  /** Returns the listing of a cancelled order to the catalog (L2-035 AC4). 409 LISTING_NOT_RELISTABLE. */
  relist(reference: string): Promise<ApiResult<null>> {
    return toResult(this.http.post<null>(`${this.baseUrl}/listings/${reference}/relist`, null));
  }
}
