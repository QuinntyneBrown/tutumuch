import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

import { API_BASE_URL } from '../api-base-url';
import { ApiResult, toResult } from '../api-result';
import type { ShippingAddress } from '../orders/checkout-api';

/** The amounts a shopper sees before paying (L2-117). Money is two-place decimal strings. */
export interface GrabQuote {
  findReference: string;
  findTitle: string;
  storeName: string;
  shelfPrice: string;
  grabFee: string;
  salesTax: string;
  total: string;
  deposit: string;
  refundAfterAcceptance: string;
}

export type GrabStatus =
  | 'PaymentPending'
  | 'PaymentFailed'
  | 'Requested'
  | 'Accepted'
  | 'Bought'
  | 'Received'
  | 'Declined'
  | 'Unavailable'
  | 'Expired'
  | 'Cancelled';

export interface GrabAddress {
  fullName: string;
  line1: string;
  line2: string | null;
  city: string;
  state: string;
  zip: string;
}

/** The shopper's grab (L2-119). refundIfCancelled is null once it can no longer be cancelled. */
export interface Grab {
  id: string;
  reference: string;
  status: GrabStatus;
  findReference: string;
  findTitle: string;
  category: string;
  storeName: string;
  storeAddress: string;
  shelfPrice: string;
  grabFee: string;
  salesTax: string;
  total: string;
  deposit: string;
  refundIfCancelled: string | null;
  refundedAmount: string | null;
  unavailableReason: string | null;
  paymentDeclineReason: string | null;
  answerBy: string | null;
  buyBy: string | null;
  boughtAt: string | null;
  createdAt: string;
  address: GrabAddress;
}

/** One row of the shopper's grabs (L2-119). */
export interface GrabRow {
  id: string;
  reference: string;
  status: GrabStatus;
  findReference: string;
  findTitle: string;
  storeName: string;
  total: string;
  deadline: string | null;
  createdAt: string;
}

/** One grab on the administrator's list (L2-118); the address only for Accepted and Bought grabs. */
export interface AdminGrab {
  id: string;
  reference: string;
  status: GrabStatus;
  findReference: string;
  findTitle: string;
  storeName: string;
  shopperDisplayName: string;
  shopperEmail: string;
  total: string;
  deposit: string;
  refundedAmount: string | null;
  unavailableReason: string | null;
  deadline: string | null;
  address: GrabAddress | null;
  createdAt: string;
}

export type GrabAction = 'accept' | 'decline' | 'bought';
export type GrabUnavailableReason = 'Gone' | 'PriceWentUp';

/** What the shopper sends to pay (L2-117): only the provider's payment method token, never card data (L2-030). */
export interface GrabPayment {
  findReference: string;
  address: ShippingAddress;
  depositAcknowledged: boolean;
  paymentMethodToken: string;
  expectedTotal: number;
}

/**
 * Typed client for "Grab it for me" (L1-025, ADR orders/0009). The shopper's calls need a verified
 * member (401, 403 EMAIL_NOT_VERIFIED); another member's grab answers 404 GRAB_NOT_FOUND (L2-121).
 */
@Injectable({ providedIn: 'root' })
export class GrabsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  /** 404 FIND_NOT_FOUND, 409 GRAB_NOT_OFFERED or GRAB_IN_PROGRESS, 422 CANNOT_GRAB_OWN_FIND, NON_US_ADDRESS or ADDRESS_INVALID. */
  quote(findReference: string, address: ShippingAddress): Promise<ApiResult<GrabQuote>> {
    return toResult(this.http.post<GrabQuote>(`${this.baseUrl}/grabs/quote`, { findReference, address }));
  }

  /** Charges the total once per idempotency key; the result comes from the provider's webhook. */
  start(payment: GrabPayment, idempotencyKey: string): Promise<ApiResult<Grab>> {
    return toResult(
      this.http.post<Grab>(`${this.baseUrl}/grabs`, payment, {
        headers: new HttpHeaders({ 'Idempotency-Key': idempotencyKey }),
      }),
    );
  }

  mine(): Promise<ApiResult<GrabRow[]>> {
    return toResult(this.http.get<GrabRow[]>(`${this.baseUrl}/me/grabs`));
  }

  get(id: string): Promise<ApiResult<Grab>> {
    return toResult(this.http.get<Grab>(`${this.baseUrl}/me/grabs/${encodeURIComponent(id)}`));
  }

  /** 409 GRAB_STATE_CHANGED once TuTuMuch has bought it. */
  cancel(id: string): Promise<ApiResult<Grab>> {
    return toResult(this.http.post<Grab>(`${this.baseUrl}/me/grabs/${encodeURIComponent(id)}/cancel`, null));
  }

  received(id: string): Promise<ApiResult<Grab>> {
    return toResult(this.http.post<Grab>(`${this.baseUrl}/me/grabs/${encodeURIComponent(id)}/received`, null));
  }
}

/** TuTuMuch handles grabs (L2-118): 403 ADMINISTRATOR_REQUIRED for anyone but an administrator. */
@Injectable({ providedIn: 'root' })
export class AdminGrabsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  list(): Promise<ApiResult<AdminGrab[]>> {
    return toResult(this.http.get<AdminGrab[]>(`${this.baseUrl}/admin/grabs`));
  }

  /** 409 GRAB_STATE_CHANGED when the grab has moved on. */
  act(id: string, action: GrabAction): Promise<ApiResult<AdminGrab>> {
    return toResult(this.http.post<AdminGrab>(`${this.baseUrl}/admin/grabs/${encodeURIComponent(id)}/${action}`, null));
  }

  unavailable(id: string, reason: GrabUnavailableReason): Promise<ApiResult<AdminGrab>> {
    return toResult(
      this.http.post<AdminGrab>(`${this.baseUrl}/admin/grabs/${encodeURIComponent(id)}/unavailable`, { reason }),
    );
  }
}

/** The status words of L2-119 for the shopper. */
export function grabStatusWords(
  grab: Pick<Grab, 'status' | 'answerBy' | 'buyBy' | 'refundedAmount' | 'unavailableReason'>,
  day: (iso: string) => string,
): string {
  const refunded = grab.refundedAmount ? ` You've been refunded $${grab.refundedAmount}.` : '';
  switch (grab.status) {
    case 'Requested':
      return `Waiting for TuTuMuch to accept${grab.answerBy ? ` (by ${day(grab.answerBy)})` : ''}`;
    case 'Accepted':
      return `TuTuMuch is going to the store${grab.buyBy ? ` (by ${day(grab.buyBy)})` : ''}`;
    case 'Bought':
      return 'TuTuMuch has it and is sending it to you';
    case 'Received':
      return "You've got it";
    case 'Declined':
      return `TuTuMuch couldn't take this grab.${refunded}`;
    case 'Unavailable':
      return `TuTuMuch couldn't get it (${grab.unavailableReason ?? 'Gone from the shelf'}).${refunded}`;
    case 'Expired':
      return `This grab ran out of time.${refunded}`;
    case 'Cancelled':
      return `You cancelled.${refunded}`;
    case 'PaymentFailed':
      return 'The payment didn’t go through';
    default:
      return 'Confirming with your bank…';
  }
}

/** The short status label for lists (design system: Status). */
export function grabStatusLabel(status: GrabStatus): string {
  return status === 'Unavailable'
    ? 'Couldn’t get it'
    : status === 'PaymentPending'
      ? 'Confirming'
      : status === 'PaymentFailed'
        ? 'Payment failed'
        : status;
}
