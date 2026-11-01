import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

import { API_BASE_URL } from '../api-base-url';
import { ApiResult, toResult } from '../api-result';
import { AuctionSummary } from '../catalog/catalog-api';

/**
 * Typed client for bids (L2-115, L2-116). A refusal carries the problem: 422 BID_TOO_LOW or
 * BID_INVALID with the message on `amount`, 409 OUTBID with the new lowest bid on `amount`, 409
 * AUCTION_ENDED, ALREADY_HIGHEST_BIDDER or NOT_AN_AUCTION, 422 CANNOT_BID_OWN_LISTING.
 */
@Injectable({ providedIn: 'root' })
export class BidsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  /** Places a bid in dollars; resolves to the auction as the bidder now sees it. */
  placeBid(reference: string, amount: number): Promise<ApiResult<AuctionSummary>> {
    return toResult(
      this.http.post<AuctionSummary>(`${this.baseUrl}/listings/${encodeURIComponent(reference)}/bids`, { amount }),
    );
  }
}
