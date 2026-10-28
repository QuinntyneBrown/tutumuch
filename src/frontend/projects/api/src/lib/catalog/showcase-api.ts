import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs';

import { API_BASE_URL } from '../api-base-url';
import { ApiResult, toResult } from '../api-result';

/** The member's own request for one showcase listing (L2-109). */
export interface ShowcaseRequest {
  listingReference: string;
  note: string | null;
  /** When it was first sent. */
  requestedAt: string;
  updatedAt: string;
}

/** A request sent: `created` is true for a new one (201), false when it updated the member's request (200). */
export interface SentShowcaseRequest {
  request: ShowcaseRequest;
  created: boolean;
}

/** One request on the administrator's list (L2-110); `requestedAt` is when it was last sent. */
export interface AdminShowcaseRequest {
  listingReference: string;
  listingTitle: string;
  memberDisplayName: string;
  memberEmail: string;
  note: string | null;
  requestedAt: string;
}

/**
 * Typed client for showcase listings and their requests (L1-024). The member's calls answer 401
 * for a visitor (L2-109); the administrator's answer 403 ADMINISTRATOR_REQUIRED for anyone else
 * (L2-010).
 */
@Injectable({ providedIn: 'root' })
export class ShowcaseApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  /** 404 SHOWCASE_REQUEST_NOT_FOUND when the member has not requested this listing. */
  myRequest(reference: string): Promise<ApiResult<ShowcaseRequest>> {
    return toResult(
      this.http.get<ShowcaseRequest>(`${this.baseUrl}/me/showcase-requests/${encodeURIComponent(reference)}`),
    );
  }

  /** 422 SHOWCASE_NOTE_LENGTH; 404 LISTING_NOT_FOUND; 429 with retryAfter after 20 new requests today. */
  sendRequest(reference: string, note: string | null): Promise<ApiResult<SentShowcaseRequest>> {
    return toResult(
      this.http
        .put<ShowcaseRequest>(
          `${this.baseUrl}/me/showcase-requests/${encodeURIComponent(reference)}`,
          { note },
          { observe: 'response' },
        )
        .pipe(map((response) => ({ request: response.body!, created: response.status === 201 }))),
    );
  }

  /** Marks or unmarks the administrator's own Draft or Active listing (L2-107). */
  setShowcase(reference: string, showcase: boolean): Promise<ApiResult<{ reference: string; showcase: boolean }>> {
    return toResult(
      this.http.put<{ reference: string; showcase: boolean }>(
        `${this.baseUrl}/admin/listings/${encodeURIComponent(reference)}/showcase`,
        { showcase },
      ),
    );
  }

  /** Every request, most recently sent first (L2-110). */
  adminRequests(): Promise<ApiResult<AdminShowcaseRequest[]>> {
    return toResult(this.http.get<AdminShowcaseRequest[]>(`${this.baseUrl}/admin/showcase-requests`));
  }
}
