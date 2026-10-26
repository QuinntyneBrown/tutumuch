import { Injectable, signal } from '@angular/core';
import { PayoutSummary } from '@tutumuch/api';

/**
 * The payout summary the banner shows, shared with the payouts page so that page's load refreshes
 * the banner, and the banner the member dismissed this session.
 */
@Injectable({ providedIn: 'root' })
export class PayoutBannerState {
  readonly summary = signal<PayoutSummary | null>(null);
  readonly dismissed = signal<string | null>(null);
}
