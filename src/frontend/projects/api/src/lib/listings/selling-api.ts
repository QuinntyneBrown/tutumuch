import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

import { API_BASE_URL } from '../api-base-url';
import { ApiResult, toResult } from '../api-result';

/** One accepted category of L2-011 with its only accepted size. */
export interface Category {
  name: string;
  subcategories: string[];
  /** For example "Men's US 12". */
  acceptedSize: string;
  /** Short label for the size tag, for example "US 12". */
  sizeLabel: string;
}

/** GET /api/selling/options: the platform fee shown before publishing (L2-031) and form choices. */
export interface SellingOptions {
  feePercent: number;
  conditions: string[];
  suggestedBrands: string[];
  maxPhotos: number;
}

/** Categories and selling options, loaded once per session (the categories endpoint is cacheable). */
@Injectable({ providedIn: 'root' })
export class SellingApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);
  private cached: Promise<ApiResult<{ categories: Category[]; options: SellingOptions }>> | null = null;

  /** Both lists, or the first failure. A failure is not cached, so "Try again" asks again. */
  load(): Promise<ApiResult<{ categories: Category[]; options: SellingOptions }>> {
    this.cached ??= Promise.all([
      toResult(this.http.get<Category[]>(`${this.baseUrl}/categories`)),
      toResult(this.http.get<SellingOptions>(`${this.baseUrl}/selling/options`)),
    ]).then(([categories, options]) => {
      if (!categories.ok) return categories;
      if (!options.ok) return options;
      return { ok: true, value: { categories: categories.value, options: options.value } };
    });
    const result = this.cached;
    void result.then((r) => {
      if (!r.ok) this.cached = null;
    });
    return result;
  }
}
