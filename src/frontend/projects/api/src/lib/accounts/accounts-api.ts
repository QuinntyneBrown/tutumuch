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

/** Which third-party sign-ins TuTuMuch API has a client for (L2-088). */
export interface SignInProviders {
  google: boolean;
}

/** The Google identity waiting on the finish-joining page (L2-088). */
export interface PendingGoogleJoin {
  email: string;
}

/** Typed client for registration, email verification and the Google join steps (L2-001, L2-002, L2-088). */
@Injectable({ providedIn: 'root' })
export class AccountsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  providers(): Promise<ApiResult<SignInProviders>> {
    return toResult(this.http.get<SignInProviders>(`${this.baseUrl}/auth/providers`));
  }

  /**
   * Where "Continue with Google" sends the browser: the API's start endpoint, a full navigation,
   * never an XHR. The API keeps only a same-site returnUrl.
   */
  googleStartUrl(returnUrl: string): string {
    return `${this.baseUrl}/auth/google/start?returnUrl=${encodeURIComponent(returnUrl || '/')}`;
  }

  /** The email of the pending Google join held in the join cookie; 410 once it expired or was used. */
  googleJoinPending(): Promise<ApiResult<PendingGoogleJoin>> {
    return toResult(this.http.get<PendingGoogleJoin>(`${this.baseUrl}/auth/google/pending`));
  }

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
