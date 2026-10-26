import { Injectable, signal } from '@angular/core';

/** The visitor's recorded choice: no choice yet, necessary cookies only, or analytics too (L2-056). */
export type ConsentState = 'unknown' | 'necessary' | 'analytics';

/** First-party cookie that records the choice in this browser. */
export const CONSENT_COOKIE = 'tm_consent';

/** How long the choice is kept before the banner asks again: 180 days. */
export const CONSENT_MAX_AGE_SECONDS = 180 * 24 * 60 * 60;

function read(): ConsentState {
  const value = document.cookie
    .split('; ')
    .find((cookie) => cookie.startsWith(`${CONSENT_COOKIE}=`))
    ?.slice(CONSENT_COOKIE.length + 1);
  return value === 'v1.necessary' ? 'necessary' : value === 'v1.analytics' ? 'analytics' : 'unknown';
}

/**
 * Holds the consent state in a signal and the tm_consent cookie (`v1.necessary` or
 * `v1.analytics`, Path=/, Secure, SameSite=Strict). An unknown or malformed value counts as no
 * choice. Nothing is sent to Tutu Much API, so saving cannot fail.
 */
@Injectable({ providedIn: 'root' })
export class CookieConsentService {
  private readonly state = signal<ConsentState>(read());
  readonly consent = this.state.asReadonly();

  acceptAnalytics(): void {
    this.save(true);
  }

  necessaryOnly(): void {
    this.save(false);
  }

  save(analytics: boolean): void {
    const state: ConsentState = analytics ? 'analytics' : 'necessary';
    document.cookie = `${CONSENT_COOKIE}=v1.${state}; Path=/; Max-Age=${CONSENT_MAX_AGE_SECONDS}; Secure; SameSite=Strict`;
    this.state.set(state);
  }
}
