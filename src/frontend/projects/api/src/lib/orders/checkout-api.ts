import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

import type { OrderProblem } from './problems-api';
import { API_BASE_URL } from '../api-base-url';
import { ApiResult, toResult } from '../api-result';
import { CardPhoto } from '../catalog/catalog-api';
import type { Cancellation, CancellationRequest, OrderTimelineStep, Tracking } from './orders-api';

/** A US shipping address. country is the two-letter code; TuTuMuch ships within the US only (L2-027). */
export interface ShippingAddress {
  fullName: string;
  line1: string;
  line2: string | null;
  city: string;
  state: string;
  zip: string;
  country: string;
}

/** The item being bought, as the order summary shows it. */
export interface CheckoutItem {
  reference: string;
  title: string;
  brand: string;
  size: string;
  condition: string;
  photo: CardPhoto | null;
}

export type CheckoutStatus = 'Open' | 'Processing' | 'Paid' | 'Released' | 'Expired';

/**
 * One checkout as its buyer sees it. Money is two-place decimal strings; salesTax and total are
 * null until an address is set. expiresAt is when the 10-minute reservation ends (L2-028).
 */
export interface Checkout {
  id: string;
  status: CheckoutStatus;
  expiresAt: string;
  item: CheckoutItem;
  itemPrice: string;
  shippingCost: string;
  salesTax: string | null;
  total: string | null;
  address: ShippingAddress | null;
  savedAddress: ShippingAddress | null;
  lastDeclineReason: string | null;
  orderId: string | null;
}

/** A payment accepted for processing; the result arrives through the provider's webhook. */
export interface PaymentAccepted {
  id: string;
  status: string;
}

/**
 * One order as the buyer's confirmation and order pages show it: what was paid, the ship-by
 * date, the timeline and the tracking once shipped.
 */
export interface Purchase {
  id: string;
  reference: string;
  status: string;
  item: CheckoutItem;
  total: string;
  cardBrand: string | null;
  cardLast4: string | null;
  sellerDisplayName: string;
  paidAt: string;
  shipBy: string;
  itemPrice: string;
  shippingCost: string;
  salesTax: string;
  timeline: OrderTimelineStep[];
  tracking: Tracking | null;
  /** When a Delivered order completes by itself, 3 days after delivery (L2-034); null otherwise. */
  autoCompletesAt: string | null;
  /** Why the seller or the deadline cancelled it, with the seller's note (L2-035). */
  cancellation: Cancellation | null;
  /** The buyer's request to cancel, if they made one. */
  cancellationRequest: CancellationRequest | null;
  /** Whether the buyer may report a problem now: Shipped, or Delivered within 3 days (L2-036). */
  canReportProblem: boolean;
  /** When a Delivered order stops taking problem reports; null otherwise. */
  problemDeadline: string | null;
  problem: OrderProblem | null;
}

/** Typed client for /api/checkouts and /api/purchases (checkout-and-pay). */
@Injectable({ providedIn: 'root' })
export class CheckoutApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  /** Reserves the listing for 10 minutes; 409 LISTING_RESERVED when someone else holds it. */
  start(listingReference: string): Promise<ApiResult<Checkout>> {
    return toResult(this.http.post<Checkout>(`${this.baseUrl}/checkouts`, { listingReference }));
  }

  get(id: string): Promise<ApiResult<Checkout>> {
    return toResult(this.http.get<Checkout>(`${this.baseUrl}/checkouts/${id}`));
  }

  /** Sets the address and prices tax; 422 NON_US_ADDRESS or ADDRESS_INVALID with field errors. */
  setAddress(id: string, address: ShippingAddress): Promise<ApiResult<Checkout>> {
    return toResult(this.http.put<Checkout>(`${this.baseUrl}/checkouts/${id}/address`, address));
  }

  /**
   * Charges the provider's payment method token for the total the buyer saw. The idempotency key
   * belongs to one attempt: a retry of the same attempt sends the same key, so it charges once.
   */
  pay(
    id: string,
    idempotencyKey: string,
    paymentMethodToken: string,
    expectedTotal: string,
  ): Promise<ApiResult<PaymentAccepted>> {
    return toResult(
      this.http.post<PaymentAccepted>(
        `${this.baseUrl}/checkouts/${id}/payments`,
        { paymentMethodToken, expectedTotal: Number(expectedTotal) },
        { headers: new HttpHeaders({ 'Idempotency-Key': idempotencyKey }) },
      ),
    );
  }

  /** "Cancel and release item": the listing is back on sale at once. */
  release(id: string): Promise<ApiResult<unknown>> {
    return toResult(this.http.delete(`${this.baseUrl}/checkouts/${id}`));
  }

  purchase(orderId: string): Promise<ApiResult<Purchase>> {
    return toResult(this.http.get<Purchase>(`${this.baseUrl}/purchases/${orderId}`));
  }
}
