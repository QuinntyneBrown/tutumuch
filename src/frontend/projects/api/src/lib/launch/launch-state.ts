import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';

import { API_BASE_URL } from '../api-base-url';
import { toResult } from '../api-result';
import { AuthState } from '../auth/auth-state';

/** The launch phase as GET /api/launch returns it (L2-090, ADR platform/0014). */
export interface Launch {
  phase: 1 | 2 | 3;
  categoryMenusVisible: boolean;
  memberSellingOpen: boolean;
}

/** Phase 1, the most closed phase, used until the API answers and when it cannot be reached. */
const PHASE_ONE: Launch = { phase: 1, categoryMenusVisible: false, memberSellingOpen: false };

/**
 * The launch phase of the site. The app loads it once before the first render; the shell, the
 * catalog, the listings routes and the marketing page read it. canSell is true when the phase
 * opens selling to members (Phase 3) or the signed-in member is an administrator (L2-094).
 */
@Injectable({ providedIn: 'root' })
export class LaunchState {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);
  private readonly auth = inject(AuthState);
  private readonly current = signal<Launch>(PHASE_ONE);
  private loading: Promise<void> | null = null;

  readonly phase = computed(() => this.current().phase);
  readonly categoryMenusVisible = computed(() => this.current().categoryMenusVisible);
  readonly memberSellingOpen = computed(() => this.current().memberSellingOpen);
  readonly canSell = computed(() => this.memberSellingOpen() || this.auth.member()?.isAdministrator === true);

  /** Loads the phase once; a failed call keeps Phase 1 (L2-090 AC5). */
  load(): Promise<void> {
    this.loading ??= toResult(this.http.get<Launch>(`${this.baseUrl}/launch`)).then((result) => {
      if (result.ok) this.current.set(result.value);
    });
    return this.loading;
  }
}
