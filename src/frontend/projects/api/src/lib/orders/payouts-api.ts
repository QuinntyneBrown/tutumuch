import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

import { API_BASE_URL } from '../api-base-url';
import { ApiResult, toResult } from '../api-result';
import { CheckoutItem } from './checkout-api';

export type PayoutStatus = 'Held' | 'Releasable' | 'Sent' | 'Paid' | 'Failed';

export type PayoutHoldReason = 'OrderNotCompleted' | 'OnboardingIncomplete';

/** Onboarding with the payment provider; only the provider's webhook makes it Complete. */
export type PayoutOnboarding = 'NotStarted' | 'Pending' | 'Complete' | 'Restricted';

/** The banner the API derives; the success banner is the browser's, on return from onboarding. */
export type PayoutBanner = 'info' | 'warning' | 'danger';

/** One sale in the payouts history: the amount after the fee and where the payout is. */
export interface PayoutRow {
  orderId: string;
  orderReference: string;
  item: CheckoutItem;
  amount: string;
  fee: string;
  status: PayoutStatus;
  holdReason: PayoutHoldReason | null;
  paidAt: string | null;
}

/** GET /api/payouts: totals, onboarding, the bank for display, the banner and the history, newest first (L2-031). */
export interface PayoutSummary {
  paidOut: string;
  pending: string;
  onboarding: PayoutOnboarding;
  bankDisplay: string | null;
  banner: PayoutBanner | null;
  history: PayoutRow[];
}

/** The payout panel of a sale: item and shipping, the TuTuMuch fee, the payout and where it is. */
export interface SalePayout {
  gross: string;
  feePercent: number;
  fee: string;
  net: string;
  status: PayoutStatus;
  sentAt: string | null;
  bankDisplay: string | null;
}

/** Typed client for seller payouts (L2-031): the summary and the provider-hosted onboarding link. */
@Injectable({ providedIn: 'root' })
export class PayoutsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  summary(): Promise<ApiResult<PayoutSummary>> {
    return toResult(this.http.get<PayoutSummary>(`${this.baseUrl}/payouts`));
  }

  /** The provider-hosted onboarding URL. 502 PAYOUT_PROVIDER_UNAVAILABLE when the provider fails. */
  onboardingLink(): Promise<ApiResult<{ url: string }>> {
    return toResult(this.http.post<{ url: string }>(`${this.baseUrl}/payouts/onboarding-link`, null));
  }
}
