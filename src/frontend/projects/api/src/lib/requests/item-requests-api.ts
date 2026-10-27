import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

import { API_BASE_URL } from '../api-base-url';
import { ApiResult, toResult } from '../api-result';
import { ListingCard } from '../catalog/catalog-api';

/**
 * The fields of an item request (L2-097). There is no size: TuTuMuch API sets it from the
 * category. A missing highest price or lowest condition means any.
 */
export interface ItemRequestBody {
  category: string;
  keywords: string;
  maxPrice?: number | null;
  minimumCondition?: string | null;
}

/**
 * One of the member's item requests (L2-099). `matchCount` counts every match ever recorded;
 * `matches` holds the matched listings still Active, newest match first (L2-102).
 */
export interface ItemRequest {
  id: string;
  category: string;
  size: string;
  keywords: string;
  maxPrice: number | null;
  minimumCondition: string | null;
  createdAt: string;
  updatedAt: string;
  expiresAt: string;
  status: 'Open' | 'Expired';
  matchCount: number;
  matches: ListingCard[];
}

/** GET /api/me/item-requests: newest first, how many are open and the limit (L2-098). */
export interface MyItemRequests {
  items: ItemRequest[];
  openCount: number;
  limit: number;
}

/**
 * Typed client for the signed-in member's item requests (L1-023). Every call answers 401 for a
 * visitor (L2-105) and 404 REQUEST_NOT_FOUND for another member's request (L2-099).
 */
@Injectable({ providedIn: 'root' })
export class ItemRequestsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  list(): Promise<ApiResult<MyItemRequests>> {
    return toResult(this.http.get<MyItemRequests>(`${this.baseUrl}/me/item-requests`));
  }

  get(id: string): Promise<ApiResult<ItemRequest>> {
    return toResult(this.http.get<ItemRequest>(`${this.baseUrl}/me/item-requests/${encodeURIComponent(id)}`));
  }

  /** 422 with field errors; 409 REQUEST_LIMIT_REACHED at 20 open requests (L2-098). */
  create(body: ItemRequestBody): Promise<ApiResult<ItemRequest>> {
    return toResult(this.http.post<ItemRequest>(`${this.baseUrl}/me/item-requests`, body));
  }

  /** Saves and renews the request for 90 days; renewing an expired one may answer 409 REQUEST_LIMIT_REACHED. */
  update(id: string, body: ItemRequestBody): Promise<ApiResult<ItemRequest>> {
    return toResult(this.http.put<ItemRequest>(`${this.baseUrl}/me/item-requests/${encodeURIComponent(id)}`, body));
  }

  remove(id: string): Promise<ApiResult<null>> {
    return toResult(this.http.delete<null>(`${this.baseUrl}/me/item-requests/${encodeURIComponent(id)}`));
  }
}
