import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';

import { API_BASE_URL } from '../api-base-url';
import { ApiResult, toResult } from '../api-result';

/** The signed-in member as GET /api/auth/me returns it. */
export interface CurrentMember {
  displayName: string;
  email: string;
  emailVerified: boolean;
}

/**
 * Session state of the app (L2-008). The session lives in HttpOnly cookies, so the app only
 * knows who is signed in by asking TuTuMuch API; nothing is kept in web storage.
 */
@Injectable({ providedIn: 'root' })
export class AuthState {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);
  private readonly current = signal<CurrentMember | null>(null);
  private readonly loaded = signal(false);
  private loading: Promise<void> | null = null;

  /** The signed-in member, or null for a visitor. */
  readonly member = this.current.asReadonly();
  /** True once the first GET /api/auth/me has answered. */
  readonly ready = this.loaded.asReadonly();
  readonly isVerified = computed(() => this.current()?.emailVerified === true);

  /** Loads the member once; later calls reuse the first answer. */
  load(): Promise<void> {
    this.loading ??= toResult(this.http.get<CurrentMember>(`${this.baseUrl}/auth/me`)).then((result) => {
      this.current.set(result.ok ? result.value : null);
      this.loaded.set(true);
    });
    return this.loading;
  }

  /** Asks TuTuMuch API again, for example after the member verified their email. */
  reload(): Promise<void> {
    this.loading = null;
    return this.load();
  }

  async signIn(email: string, password: string): Promise<ApiResult<CurrentMember>> {
    const result = await toResult(this.http.post<CurrentMember>(`${this.baseUrl}/auth/sign-in`, { email, password }));
    if (result.ok) {
      this.current.set(result.value);
      this.loaded.set(true);
      this.loading = Promise.resolve();
    }
    return result;
  }

  async signOut(): Promise<void> {
    await toResult(this.http.post(`${this.baseUrl}/auth/sign-out`, null));
    this.current.set(null);
  }

  /** Called by the interceptor when the session could not be renewed. */
  clear(): void {
    this.current.set(null);
  }
}
