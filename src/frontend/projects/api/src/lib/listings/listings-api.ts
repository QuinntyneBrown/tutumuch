import { HttpClient, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs';

import { API_BASE_URL } from '../api-base-url';
import { ApiResult, toResult } from '../api-result';
import { Photo } from './photos-api';

/**
 * The editable fields of a listing. There is no size: TuTuMuch API sets it from the category
 * (L2-013). Empty fields are left out of the request.
 */
export interface ListingDetails {
  category?: string;
  subcategory?: string;
  title?: string;
  brand?: string;
  condition?: string;
  flaws?: string;
  description?: string;
  price?: number;
  shippingCost?: number;
  /** Uploaded photo IDs in display order; the first is the cover (L2-017). */
  photoIds?: string[];
  /** The seller ticked the size confirmation again; required to save a published listing (L2-014). */
  sizeConfirmed?: boolean;
}

/** A listing as its seller sees it. */
export interface MyListing {
  reference: string;
  status: 'Draft' | 'Active' | 'Reserved' | 'Sold' | 'Removed';
  category: string;
  subcategory: string | null;
  size: string;
  title: string | null;
  description: string | null;
  brand: string | null;
  condition: string | null;
  flaws: string | null;
  price: number | null;
  shippingCost: number | null;
  createdAt: string;
  updatedAt: string;
  publishedAt: string | null;
  /** Why a Removed listing is off the catalog; a withdrawn one cannot be edited or relisted. */
  removalReason: 'Withdrawn' | 'OrderCancelled' | null;
  /** Cover first. */
  photos: Photo[];
}

/** A listing with the ETag TuTuMuch API sent for it, to send back in If-Match when saving (L2-020). */
export interface VersionedListing {
  listing: MyListing;
  etag: string;
}

export type ListingStatus = MyListing['status'];

/** One row of "My listings" (L2-021); `title` is null for an untitled draft, `saleOrderId` set for a Sold listing. */
export interface MyListingRow {
  reference: string;
  status: ListingStatus;
  title: string | null;
  category: string;
  price: number | null;
  photo: { width: number; height: number; src: string; webp: string; jpeg: string } | null;
  createdAt: string;
  saleOrderId: string | null;
}

/** GET /api/me/listings: the rows newest first and how many listings are in each status. */
export interface MyListings {
  items: MyListingRow[];
  counts: { all: number; draft: number; active: number; reserved: number; sold: number; removed: number };
}

/** POST /api/listings/{reference}/publish: whether to prompt for payout onboarding (L2-031). */
export interface PublishResult {
  reference: string;
  status: string;
  payoutOnboardingRequired: boolean;
}

/** Typed client for the seller's listings (create-listing). */
@Injectable({ providedIn: 'root' })
export class ListingsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  createDraft(details: ListingDetails): Promise<ApiResult<MyListing>> {
    return toResult(this.http.post<MyListing>(`${this.baseUrl}/listings`, details));
  }

  updateDraft(reference: string, details: ListingDetails): Promise<ApiResult<MyListing>> {
    return toResult(this.http.put<MyListing>(`${this.baseUrl}/listings/${encodeURIComponent(reference)}`, details));
  }

  publish(reference: string, sizeConfirmed: boolean): Promise<ApiResult<PublishResult>> {
    return toResult(
      this.http.post<PublishResult>(`${this.baseUrl}/listings/${encodeURIComponent(reference)}/publish`, {
        sizeConfirmed,
      }),
    );
  }

  /** The seller's listings in every status, or in `status` only, newest first (L2-021). */
  mine(status?: ListingStatus): Promise<ApiResult<MyListings>> {
    const params: Record<string, string> = status ? { status } : {};
    return toResult(this.http.get<MyListings>(`${this.baseUrl}/me/listings`, { params }));
  }

  getMine(reference: string): Promise<ApiResult<MyListing>> {
    return toResult(this.http.get<MyListing>(`${this.baseUrl}/me/listings/${encodeURIComponent(reference)}`));
  }

  /** The listing to edit with its ETag (edit-and-withdraw-listing). */
  getForEdit(reference: string): Promise<ApiResult<VersionedListing>> {
    const url = `${this.baseUrl}/me/listings/${encodeURIComponent(reference)}`;
    return toResult(this.http.get<MyListing>(url, { observe: 'response' }).pipe(map(versioned)));
  }

  /**
   * Saves a published listing under the ETag it was read with. 412 LISTING_CHANGED means someone
   * changed it since; 409 LISTING_RESERVED, LISTING_SOLD or LISTING_NOT_EDITABLE that it is locked.
   */
  updateListing(reference: string, details: ListingDetails, etag: string): Promise<ApiResult<VersionedListing>> {
    const url = `${this.baseUrl}/listings/${encodeURIComponent(reference)}`;
    return toResult(
      this.http
        .put<MyListing>(url, details, { observe: 'response', headers: { 'If-Match': etag } })
        .pipe(map(versioned)),
    );
  }

  /** Takes an Active listing off the catalog for good (L2-020). */
  withdraw(reference: string): Promise<ApiResult<{ reference: string; status: ListingStatus }>> {
    return toResult(
      this.http.post<{ reference: string; status: ListingStatus }>(
        `${this.baseUrl}/listings/${encodeURIComponent(reference)}/withdraw`,
        {},
      ),
    );
  }
}

function versioned(response: HttpResponse<MyListing>): VersionedListing {
  return { listing: response.body!, etag: response.headers.get('ETag') ?? '' };
}
