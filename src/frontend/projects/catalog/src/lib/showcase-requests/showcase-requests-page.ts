import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AdminShowcaseRequest, ShowcaseApi, shortRef } from '@tutumuch/api';
import {
  ButtonDirective,
  CenterStateComponent,
  EmptyStateComponent,
  IconComponent,
  SkeletonComponent,
} from '@tutumuch/components';

/** "Today 09:12", "Yesterday 18:40" or "Fri 23 Oct": when a request was last sent. */
export function sentWhen(iso: string, now = new Date()): string {
  const date = new Date(iso);
  const day = (d: Date) => Date.UTC(d.getUTCFullYear(), d.getUTCMonth(), d.getUTCDate());
  const days = Math.round((day(now) - day(date)) / 86_400_000);
  const time = date.toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit', timeZone: 'UTC' });
  if (days === 0) return `Today ${time}`;
  if (days === 1) return `Yesterday ${time}`;
  return date.toLocaleDateString('en-GB', { weekday: 'short', day: 'numeric', month: 'short', timeZone: 'UTC' });
}

/**
 * The administrator's showcase requests at /admin/requests in the admin shell (L2-110, mocks
 * pages/showcase-requests): one block per request, most recently sent first, with the listing,
 * who asked, their email and their note as plain text (L2-111). A 403 is the forbidden state; a
 * visitor is sent to sign in by the route guard.
 */
@Component({
  selector: 'tl-showcase-requests-page',
  imports: [RouterLink, ButtonDirective, CenterStateComponent, EmptyStateComponent, IconComponent, SkeletonComponent],
  templateUrl: './showcase-requests-page.html',
  styleUrl: './showcase-requests-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShowcaseRequestsPage implements OnInit {
  private readonly api = inject(ShowcaseApi);
  private readonly router = inject(Router);

  readonly view = signal<'loading' | 'loaded' | 'error' | 'forbidden'>('loading');
  readonly requests = signal<AdminShowcaseRequest[]>([]);
  readonly errorRef = signal('');
  readonly subtitle = computed(() => {
    const count = this.requests().length;
    return count === 1 ? '1 request' : `${count} requests`;
  });
  readonly when = sentWhen;

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.view.set('loading');
    const result = await this.api.adminRequests();
    if (result.ok) {
      this.requests.set(result.value);
      this.view.set('loaded');
      return;
    }
    const { status, correlationId } = result.problem;
    if (status === 401) {
      await this.router.navigate(['/sign-in'], { queryParams: { returnUrl: '/admin/requests' } });
    } else if (status === 403) {
      this.view.set('forbidden');
    } else {
      this.errorRef.set(shortRef(correlationId));
      this.view.set('error');
    }
  }
}
