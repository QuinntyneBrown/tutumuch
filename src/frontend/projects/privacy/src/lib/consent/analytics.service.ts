import {
  EnvironmentProviders,
  Injectable,
  InjectionToken,
  effect,
  inject,
  makeEnvironmentProviders,
  provideEnvironmentInitializer,
} from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { Subscription, filter } from 'rxjs';

import { CookieConsentService } from './cookie-consent.service';

/** The analytics provider: its script and the cookies it sets. The provider itself is not chosen yet. */
export interface AnalyticsConfig {
  scriptUrl: string;
  cookieNames: readonly string[];
}

export const ANALYTICS_CONFIG = new InjectionToken<AnalyticsConfig>('ANALYTICS_CONFIG');

/** What the provider's script exposes once loaded (ADR privacy/0001). */
interface AnalyticsGlobal {
  pageView?(url: string): void;
  optOut?(): void;
}

type AnalyticsWindow = Window & { tutumuchAnalytics?: AnalyticsGlobal };

/**
 * The only code that talks to the analytics provider. It follows the consent signal: on
 * `analytics` it appends the provider's script and sends page views; on `necessary` it opts out,
 * stops page views and deletes the provider's cookies. Before a choice nothing loads (L2-056).
 */
@Injectable({ providedIn: 'root' })
export class AnalyticsService {
  private readonly config = inject(ANALYTICS_CONFIG);
  private readonly consent = inject(CookieConsentService).consent;
  private readonly router = inject(Router);
  private script?: HTMLScriptElement;
  private pageViews?: Subscription;

  constructor() {
    effect(() => {
      const state = this.consent();
      if (state === 'analytics') this.start();
      else if (state === 'necessary') this.stop();
    });
  }

  trackPageView(url: string): void {
    if (this.consent() !== 'analytics') return;
    (window as AnalyticsWindow).tutumuchAnalytics?.pageView?.(url);
  }

  private start(): void {
    if (!this.config.scriptUrl) return;
    if (!this.script) {
      this.script = document.createElement('script');
      this.script.src = this.config.scriptUrl;
      this.script.async = true;
      document.head.appendChild(this.script);
    }
    this.pageViews ??= this.router.events
      .pipe(filter((event) => event instanceof NavigationEnd))
      .subscribe((event) => this.trackPageView(event.urlAfterRedirects));
  }

  private stop(): void {
    (window as AnalyticsWindow).tutumuchAnalytics?.optOut?.();
    this.pageViews?.unsubscribe();
    this.pageViews = undefined;
    const parent = location.hostname.split('.').slice(-2).join('.');
    for (const name of this.config.cookieNames) {
      document.cookie = `${name}=; Path=/; Max-Age=0`;
      document.cookie = `${name}=; Path=/; Max-Age=0; Domain=${parent}`;
    }
  }
}

/** Registers the analytics provider and starts following the visitor's consent from start-up. */
export function provideAnalytics(config: AnalyticsConfig): EnvironmentProviders {
  return makeEnvironmentProviders([
    { provide: ANALYTICS_CONFIG, useValue: config },
    provideEnvironmentInitializer(() => inject(AnalyticsService)),
  ]);
}
