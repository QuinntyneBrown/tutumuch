import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

import { API_BASE_URL } from '../api-base-url';
import { ApiResult, toResult } from '../api-result';

/** Body of POST /api/auth/register (L2-001). */
export interface Registration {
  displayName: string;
  email: string;
  password: string;
  ageConfirmed: boolean;
  termsAccepted: boolean;
}

/** Typed client for registration and email verification (L2-001, L2-002). */
@Injectable({ providedIn: 'root' })
export class AccountsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  register(registration: Registration): Promise<ApiResult<unknown>> {
    return toResult(this.http.post(`${this.baseUrl}/auth/register`, registration));
  }

  verifyEmail(token: string): Promise<ApiResult<unknown>> {
    return toResult(this.http.post(`${this.baseUrl}/auth/verify-email`, { token }));
  }

  /** Sends a new link; earlier links stop working. 429 after 3 links in an hour. */
  requestVerificationEmail(email: string): Promise<ApiResult<unknown>> {
    return toResult(this.http.post(`${this.baseUrl}/auth/verification-email`, { email }));
  }
}
