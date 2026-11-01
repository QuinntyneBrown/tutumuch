import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AdminGrab, AdminGrabsApi, grabStatusLabel, shortRef } from '@tutumuch/api';
import {
  ButtonDirective,
  CenterStateComponent,
  DialogService,
  EmptyStateComponent,
  IconComponent,
  SkeletonComponent,
  StatusComponent,
  ToastService,
} from '@tutumuch/components';

import { DeclineGrabDialog, GrabUnavailableDialog } from '../grab-dialogs/grab-dialogs';
import { grabTone } from '../my-grabs/grab-text';
import { formatMoney } from '../money';
import { formatDay } from '../order-dates';

const OPEN = new Set(['Requested', 'Accepted', 'Bought']);

/**
 * TuTuMuch's grabs at /admin/grabs in the admin shell (L2-118, mocks pages/admin-grabs): open grabs
 * first by their next deadline, then ended ones. Requested grabs are accepted at once or declined
 * after a confirmation; Accepted grabs are bought at once or reported as unavailable with a reason.
 * The shipping address shows only for Accepted and Bought grabs (L2-121). A 403 is the forbidden state.
 */
@Component({
  selector: 'tl-admin-grabs-page',
  imports: [
    RouterLink,
    ButtonDirective,
    CenterStateComponent,
    EmptyStateComponent,
    IconComponent,
    SkeletonComponent,
    StatusComponent,
  ],
  templateUrl: './admin-grabs-page.html',
  styleUrl: './admin-grabs-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminGrabsPage implements OnInit {
  private readonly api = inject(AdminGrabsApi);
  private readonly router = inject(Router);
  private readonly dialogs = inject(DialogService);
  private readonly toasts = inject(ToastService);

  readonly view = signal<'loading' | 'loaded' | 'error' | 'forbidden'>('loading');
  readonly grabs = signal<AdminGrab[]>([]);
  readonly acting = signal<string | null>(null);
  readonly errorRef = signal('');
  readonly money = formatMoney;
  readonly day = formatDay;
  readonly label = grabStatusLabel;
  readonly tone = grabTone;
  readonly subtitle = computed(() => {
    const open = this.grabs().filter((g) => OPEN.has(g.status)).length;
    return `${open} open · ${this.grabs().length - open} ended`;
  });

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.view.set('loading');
    const result = await this.api.list();
    if (result.ok) {
      this.grabs.set(result.value);
      this.view.set('loaded');
      return;
    }
    const { status, correlationId } = result.problem;
    if (status === 401) {
      await this.router.navigate(['/sign-in'], { queryParams: { returnUrl: '/admin/grabs' } });
    } else if (status === 403) {
      this.view.set('forbidden');
    } else {
      this.errorRef.set(shortRef(correlationId));
      this.view.set('error');
    }
  }

  async accept(grab: AdminGrab): Promise<void> {
    if (this.acting()) return;
    this.acting.set(grab.id);
    const result = await this.api.act(grab.id, 'accept');
    this.acting.set(null);
    if (result.ok) {
      this.replace(result.value);
      this.toasts.show({
        tone: 'success',
        title: 'Grab accepted',
        text: `We emailed ${grab.shopperDisplayName}. Buy it by ${result.value.deadline ? formatDay(result.value.deadline) : 'the deadline'}.`,
      });
    } else {
      this.refused();
    }
  }

  async bought(grab: AdminGrab): Promise<void> {
    if (this.acting()) return;
    this.acting.set(grab.id);
    const result = await this.api.act(grab.id, 'bought');
    this.acting.set(null);
    if (result.ok) {
      this.replace(result.value);
      this.toasts.show({ tone: 'success', title: 'Marked as bought' });
    } else {
      this.refused();
    }
  }

  async decline(grab: AdminGrab): Promise<void> {
    const declined = await this.dialogs.open<DeclineGrabDialog, AdminGrab, AdminGrab>(DeclineGrabDialog, grab).closed;
    if (declined) this.replace(declined);
  }

  async unavailable(grab: AdminGrab): Promise<void> {
    const ended = await this.dialogs.open<GrabUnavailableDialog, AdminGrab, AdminGrab>(GrabUnavailableDialog, grab)
      .closed;
    if (ended) this.replace(ended);
  }

  private replace(grab: AdminGrab): void {
    this.grabs.update((grabs) => grabs.map((g) => (g.id === grab.id ? grab : g)));
  }

  private refused(): void {
    this.toasts.show({ tone: 'danger', title: 'This grab has changed', text: 'Refresh to see where it is.' });
    void this.load();
  }
}
