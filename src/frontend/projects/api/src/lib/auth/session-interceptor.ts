import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, catchError, shareReplay, switchMap, throwError } from 'rxjs';

import { API_BASE_URL } from '../api-base-url';
import { AuthState } from './auth-state';

let refreshing: Observable<unknown> | null = null;

/**
 * Sends the session cookies with every call to Tutu Much API and, when the 15-minute access token has
 * expired (401), renews the session once through POST /api/auth/refresh and retries (L2-008).
 */
export const sessionInterceptor: HttpInterceptorFn = (request, next) => {
  const baseUrl = inject(API_BASE_URL);
  const auth = inject(AuthState);
  if (!request.url.startsWith(baseUrl)) return next(request);

  const withCookies = request.clone({ withCredentials: true });
  if (request.url.startsWith(`${baseUrl}/auth/`) && !request.url.endsWith('/auth/me')) return next(withCookies);

  return next(withCookies).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401) return throwError(() => error);
      refreshing ??= next(
        request.clone({ url: `${baseUrl}/auth/refresh`, method: 'POST', body: null, withCredentials: true }),
      ).pipe(shareReplay(1));
      return refreshing.pipe(
        catchError((refreshError: unknown) => {
          refreshing = null;
          auth.clear();
          return throwError(() => (refreshError instanceof HttpErrorResponse ? error : refreshError));
        }),
        switchMap(() => {
          refreshing = null;
          return next(withCookies);
        }),
      );
    }),
  );
};
