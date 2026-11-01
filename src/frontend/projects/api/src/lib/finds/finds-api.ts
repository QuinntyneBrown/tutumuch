import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs';

import { API_BASE_URL } from '../api-base-url';
import { ApiResult, toResult } from '../api-result';

/** One card on Shelf finds (L2-114). Money is a two-place decimal string; seenOn is "YYYY-MM-DD". */
export interface FindCard {
  reference: string;
  title: string;
  category: string;
  shelfPrice: string;
  storeName: string;
  seenOn: string;
  /** A grab is offered and none is open. */
  grabAvailable: boolean;
}

export interface FindPage {
  items: FindCard[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export type FindStatus = 'Active' | 'Gone' | 'Expired';

/** One find (L2-115). grabOpen says only that a grab is open, never who asked (L2-121); mine is true for the poster. */
export interface FindDetail {
  reference: string;
  title: string;
  category: string;
  shelfPrice: string;
  storeName: string;
  storeAddress: string;
  seenOn: string;
  expiresOn: string;
  note: string | null;
  status: FindStatus;
  posterDisplayName: string;
  grabFee: string | null;
  grabOpen: boolean;
  mine: boolean;
}

/** A find as the poster sends it (L2-113). grabFee is for administrators only. */
export interface FindBody {
  title: string;
  category: string;
  shelfPrice: number | null;
  storeName: string;
  storeAddress: string;
  seenOn: string;
  note: string | null;
  grabFee: number | null;
}

/** The maps search for a store (L2-115): a plain link, no map SDK (ADR catalog/0009). */
export function mapsUrl(storeName: string, storeAddress: string): string {
  return `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(`${storeName}, ${storeAddress}`)}`;
}

/**
 * Typed client for shelf finds (L1-025). Browsing and reading are public; posting answers 403
 * SELLING_NOT_OPEN or EMAIL_NOT_VERIFIED under the launch-phase rule (L2-091); marking gone and
 * withdrawing answer 404 FIND_NOT_FOUND for anyone but the poster and 409 GRAB_IN_PROGRESS while a grab is open.
 */
@Injectable({ providedIn: 'root' })
export class FindsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  list(page = 1): Promise<ApiResult<FindPage>> {
    return toResult(this.http.get<FindPage>(`${this.baseUrl}/finds`, { params: { page } }));
  }

  get(reference: string): Promise<ApiResult<FindDetail>> {
    return toResult(this.http.get<FindDetail>(`${this.baseUrl}/finds/${encodeURIComponent(reference)}`));
  }

  /** 422 with field errors, GRAB_OFFER_NOT_ALLOWED or CATEGORY_NOT_SUPPORTED; 429 with retryAfter after 20 finds today. */
  post(body: FindBody): Promise<ApiResult<FindDetail>> {
    return toResult(this.http.post<FindDetail>(`${this.baseUrl}/finds`, body));
  }

  markGone(reference: string): Promise<ApiResult<FindDetail>> {
    return toResult(this.http.post<FindDetail>(`${this.baseUrl}/finds/${encodeURIComponent(reference)}/gone`, null));
  }

  withdraw(reference: string): Promise<ApiResult<true>> {
    return toResult(
      this.http.delete(`${this.baseUrl}/finds/${encodeURIComponent(reference)}`).pipe(map(() => true as const)),
    );
  }
}
