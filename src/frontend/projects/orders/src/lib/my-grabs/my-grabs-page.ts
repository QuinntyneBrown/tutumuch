import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { GrabRow, GrabsApi, grabStatusLabel, shortRef } from '@tutumuch/api';
import {
  ButtonDirective,
  EmptyStateComponent,
  IconComponent,
  ItemRowComponent,
  SkeletonComponent,
  StatusComponent,
} from '@tutumuch/components';

import { formatMoney } from '../money';
import { grabTone } from './grab-text';

/**
 * The shopper's grabs at /account/grabs (L2-119, mocks pages/my-grabs), newest first, each linking
 * to the grab. A visitor is sent to sign in and back.
 */
@Component({
  selector: 'tl-my-grabs-page',
  imports: [
    RouterLink,
    ButtonDirective,
    EmptyStateComponent,
    IconComponent,
    ItemRowComponent,
    SkeletonComponent,
    StatusComponent,
  ],
  templateUrl: './my-grabs-page.html',
  styleUrl: './my-grabs-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyGrabsPage implements OnInit {
  private readonly api = inject(GrabsApi);
  private readonly router = inject(Router);

  readonly view = signal<'loading' | 'loaded' | 'error'>('loading');
  readonly grabs = signal<GrabRow[]>([]);
  readonly errorRef = signal('');
  readonly subtitle = computed(() => (this.grabs().length === 1 ? '1 grab' : `${this.grabs().length} grabs`));
  readonly label = grabStatusLabel;
  readonly tone = grabTone;

  ngOnInit(): void {
    void this.load();
  }

  meta(row: GrabRow): string {
    return `${row.reference} · ${row.storeName} · ${formatMoney(row.total)}`;
  }

  async load(): Promise<void> {
    this.view.set('loading');
    const result = await this.api.mine();
    if (result.ok) {
      this.grabs.set(result.value);
      this.view.set('loaded');
      return;
    }
    if (result.problem.status === 401) {
      await this.router.navigate(['/sign-in'], { queryParams: { returnUrl: '/account/grabs' } });
      return;
    }
    this.errorRef.set(shortRef(result.problem.correlationId));
    this.view.set('error');
  }
}
