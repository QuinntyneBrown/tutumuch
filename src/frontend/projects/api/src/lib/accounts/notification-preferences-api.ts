import { HttpClient, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs';

import { API_BASE_URL } from '../api-base-url';
import { ApiResult, toResult } from '../api-result';

export type NotificationCategoryKey = 'accountSecurity' | 'orders' | 'reviews' | 'marketing';

/** One row of the Emails page; a locked category is always on (L2-043). */
export interface NotificationCategory {
  key: NotificationCategoryKey;
  enabled: boolean;
  locked: boolean;
}

/** GET /api/me/notification-preferences: the 4 categories in page order. */
export interface NotificationPreferences {
  categories: NotificationCategory[];
}

/** The settings with the ETag TuTuMuch API sent, to send back in If-Match when saving. */
export interface VersionedNotificationPreferences {
  preferences: NotificationPreferences;
  etag: string;
}

/** Body of PUT /api/me/notification-preferences: only the two optional categories. */
export interface NotificationPreferencesUpdate {
  reviewEmails: boolean;
  marketingEmails: boolean;
}

/** Typed client for email settings and the one-click marketing unsubscribe (L2-043). */
@Injectable({ providedIn: 'root' })
export class NotificationPreferencesApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  get(): Promise<ApiResult<VersionedNotificationPreferences>> {
    return toResult(
      this.http
        .get<NotificationPreferences>(`${this.baseUrl}/me/notification-preferences`, { observe: 'response' })
        .pipe(map(versioned)),
    );
  }

  /** Saves under the ETag the settings were read with; 412 NOTIFICATION_PREFERENCES_CHANGED when they changed since. */
  update(update: NotificationPreferencesUpdate, etag: string): Promise<ApiResult<VersionedNotificationPreferences>> {
    return toResult(
      this.http
        .put<NotificationPreferences>(`${this.baseUrl}/me/notification-preferences`, update, {
          observe: 'response',
          headers: { 'If-Match': etag },
        })
        .pipe(map(versioned)),
    );
  }

  /** Turns marketing email off with the token from the email link, without a session. */
  unsubscribe(token: string): Promise<ApiResult<{ category: string; unsubscribed: boolean }>> {
    return toResult(
      this.http.post<{ category: string; unsubscribed: boolean }>(`${this.baseUrl}/notifications/unsubscribe`, {
        token,
      }),
    );
  }
}

function versioned(response: HttpResponse<NotificationPreferences>): VersionedNotificationPreferences {
  return { preferences: response.body!, etag: response.headers.get('ETag') ?? '' };
}
