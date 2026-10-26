import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PayoutsApi, PayoutSummary, shortRef } from '@tutumuch/api';
import {
  AlertComponent,
  ButtonDirective,
  EmptyStateComponent,
  IconComponent,
  ItemRowComponent,
  StatComponent,
} from '@tutumuch/components';

import { formatMoney } from '../money';
import { payoutCaption, payoutMeta } from './payout-text';
import { PayoutBannerState } from './payout-banner-state';

/**
 * The seller's payouts at /account/payouts (mocks pages/payouts, L2-031): paid out and pending
 * totals, the bank, and each sale's payout after the fee. Until onboarding is complete it asks the
 * seller to set up payouts and sends them to the provider-hosted onboarding.
 */
@Component({
  selector: 'tm-payouts-page',
  imports: [
    RouterLink,
    AlertComponent,
    ButtonDirective,
    EmptyStateComponent,
    IconComponent,
    ItemRowComponent,
    StatComponent,
  ],
  templateUrl: './payouts-page.html',
  styleUrl: './payouts-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PayoutsPage {
  private readonly api = inject(PayoutsApi);
  private readonly bannerState = inject(PayoutBannerState);

  readonly summary = signal<PayoutSummary | null>(null);
  readonly view = signal<'loading' | 'loaded' | 'error'>('loading');
  readonly errorRef = signal('');
  readonly settingUp = signal(false);
  readonly setupError = signal<string | null>(null);

  readonly onboarded = computed(() => this.summary()?.onboarding === 'Complete');
  readonly empty = computed(() => (this.summary()?.history.length ?? 0) === 0);

  readonly money = formatMoney;
  readonly meta = payoutMeta;
  readonly caption = payoutCaption;

  constructor() {
    void this.load();
  }

  retry(): Promise<void> {
    return this.load();
  }

  /** Requests the onboarding link and leaves Tutu Much for the provider's hosted pages. */
  async setUp(): Promise<void> {
    this.settingUp.set(true);
    this.setupError.set(null);
    const result = await this.api.onboardingLink();
    if (!result.ok) {
      this.settingUp.set(false);
      this.setupError.set(result.problem.title);
      return;
    }
    window.location.assign(result.value.url);
  }

  private async load(): Promise<void> {
    this.view.set('loading');
    const result = await this.api.summary();
    if (!result.ok) {
      this.errorRef.set(shortRef(result.problem.correlationId));
      this.view.set('error');
      return;
    }
    this.summary.set(result.value);
    this.bannerState.summary.set(result.value);
    this.view.set('loaded');
  }
}
