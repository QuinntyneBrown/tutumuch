import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

import { API_BASE_URL } from '../api-base-url';
import { ApiResult, toResult } from '../api-result';
import { CatalogPage } from '../catalog/catalog-api';

/**
 * A public profile (L2-041): public fields only, never an email, phone, full name or address
 * (L2-054). memberSince is the first day of the joining month, such as "2025-01-01".
 */
export interface PublicProfile {
  memberId: string;
  displayName: string;
  avatarUrl: string | null;
  bio: string | null;
  publicLocation: string | null;
  memberSince: string;
  rating: { average: number | null; count: number };
  completedSales: number;
  activeListingCount: number;
}

/** Typed client for public member profiles; anyone may call it, signed in or not. */
@Injectable({ providedIn: 'root' })
export class MembersApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  /** The profile header and stats; 404 MEMBER_UNAVAILABLE for an unknown or closed member. */
  profile(displayName: string): Promise<ApiResult<PublicProfile>> {
    return toResult(this.http.get<PublicProfile>(`${this.baseUrl}/members/${encodeURIComponent(displayName)}`));
  }

  /** One page of the member's Active listings, newest first, 24 per page. */
  listings(displayName: string, page: number): Promise<ApiResult<CatalogPage>> {
    let params = new HttpParams();
    if (page > 1) params = params.set('page', page);
    return toResult(
      this.http.get<CatalogPage>(`${this.baseUrl}/members/${encodeURIComponent(displayName)}/listings`, { params }),
    );
  }
}
