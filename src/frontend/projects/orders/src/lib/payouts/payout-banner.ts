import { ChangeDetectionStrategy, Component, computed, effect, inject, untracked } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink } from '@angular/router';
import { filter, map } from 'rxjs';
import { AuthState, LaunchState, PayoutsApi } from '@tutumuch/api';
import { AlertComponent } from '@tutumuch/components';

import { formatMoney } from '../money';
import { PayoutBannerState } from './payout-banner-state';
import { sentAmount } from './payout-text';

type BannerKind = 'info' | 'success' | 'warning' | 'danger';

/**
 * The payout banner above the top bar (mocks notifications/payout-banner, L2-031): info while a
 * payout is on its way, success once on return from completed onboarding, warning while money waits
 * for onboarding, danger when the last payout was returned. Info and success can be dismissed.
 * On the payouts page only success shows, because the page itself carries the rest. Nothing shows,
 * and nothing is requested, while the launch phase does not let the member sell (L2-094).
 */
@Component({
  selector: 'tl-payout-banner',
  imports: [RouterLink, AlertComponent],
  templateUrl: './payout-banner.html',
  styleUrl: './payout-banner.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PayoutBannerComponent {
  private readonly api = inject(PayoutsApi);
  private readonly auth = inject(AuthState);
  private readonly launch = inject(LaunchState);
  private readonly router = inject(Router);
  private readonly state = inject(PayoutBannerState);
  private requested = false;

  private readonly url = toSignal(
    this.router.events.pipe(
      filter((event) => event instanceof NavigationEnd),
      map((event) => event.urlAfterRedirects),
    ),
    { initialValue: null },
  );
  private readonly onPayoutsPage = computed(() => this.url()?.startsWith('/account/payouts') ?? false);

  readonly kind = computed<BannerKind | null>(() => {
    const summary = this.state.summary();
    if (!summary || !this.auth.isVerified() || !this.launch.canSell()) return null;
    const kind: BannerKind | null = this.onPayoutsPage()
      ? (this.url()?.includes('onboarding=return') ?? false) && summary.onboarding === 'Complete'
        ? 'success'
        : null
      : summary.banner;
    return kind === this.state.dismissed() ? null : kind;
  });
  readonly sent = computed(() => sentAmount(this.state.summary()?.history ?? []));
  readonly pending = computed(() => formatMoney(this.state.summary()?.pending ?? '0'));
  readonly bank = computed(() => this.state.summary()?.bankDisplay ?? 'your bank');

  constructor() {
    effect(() => {
      // Waits for the first navigation, so a page that loads the summary itself is not asked twice.
      if (
        this.url() !== null &&
        this.auth.isVerified() &&
        this.launch.canSell() &&
        !this.onPayoutsPage() &&
        !this.requested
      ) {
        this.requested = true;
        untracked(() => void this.load());
      }
    });
  }

  dismiss(kind: BannerKind): void {
    this.state.dismissed.set(kind);
  }

  private async load(): Promise<void> {
    const result = await this.api.summary();
    if (result.ok && !this.state.summary()) this.state.summary.set(result.value);
  }
}
