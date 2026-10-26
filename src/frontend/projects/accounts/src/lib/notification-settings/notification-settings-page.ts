import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ApiProblem, NotificationPreferencesApi, VersionedNotificationPreferences, shortRef } from '@tutumuch/api';
import {
  AlertComponent,
  ButtonDirective,
  EmptyStateComponent,
  IconComponent,
  SwitchComponent,
  ToastService,
} from '@tutumuch/components';

/**
 * The member's email settings at /account/emails (mocks pages/notification-settings, L2-043):
 * account security and order emails locked on, review and marketing emails the member's choice,
 * saved with the ETag they were read with. A visitor is sent to sign in.
 */
@Component({
  selector: 'tl-notification-settings-page',
  imports: [AlertComponent, ButtonDirective, EmptyStateComponent, IconComponent, SwitchComponent],
  templateUrl: './notification-settings-page.html',
  styleUrl: './notification-settings-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationSettingsPage {
  private readonly api = inject(NotificationPreferencesApi);
  private readonly router = inject(Router);
  private readonly toasts = inject(ToastService);
  private etag = '';

  readonly view = signal<'loading' | 'loaded' | 'error'>('loading');
  readonly errorRef = signal('');
  readonly reviews = signal(true);
  readonly marketing = signal(false);
  readonly saving = signal(false);
  readonly saved = signal(false);

  constructor() {
    void this.load();
  }

  retry(): Promise<void> {
    return this.load();
  }

  async save(event?: Event): Promise<void> {
    event?.preventDefault();
    if (this.saving()) return;
    this.saving.set(true);
    this.saved.set(false);
    const result = await this.api.update(
      { reviewEmails: this.reviews(), marketingEmails: this.marketing() },
      this.etag,
    );
    this.saving.set(false);
    if (result.ok) {
      this.apply(result.value);
      this.saved.set(true);
      return;
    }
    if (this.toSignIn(result.problem)) return;
    if (result.problem.code === 'NOTIFICATION_PREFERENCES_CHANGED') {
      this.toasts.show({ tone: 'warning', title: result.problem.title, text: 'Check them and save again.' });
      await this.load(false);
      return;
    }
    this.toasts.show({
      tone: 'danger',
      title: 'Something went wrong',
      text: shortRef(result.problem.correlationId),
      action: { label: 'Try again', run: () => void this.save() },
    });
  }

  /** Reads the settings; a reload after a conflict keeps the page on screen. */
  private async load(showLoading = true): Promise<void> {
    if (showLoading) this.view.set('loading');
    const result = await this.api.get();
    if (!result.ok) {
      if (this.toSignIn(result.problem)) return;
      this.errorRef.set(shortRef(result.problem.correlationId));
      this.view.set('error');
      return;
    }
    this.apply(result.value);
    this.view.set('loaded');
  }

  private apply({ preferences, etag }: VersionedNotificationPreferences): void {
    this.etag = etag;
    const enabled = (key: string) => preferences.categories.find((category) => category.key === key)?.enabled ?? false;
    this.reviews.set(enabled('reviews'));
    this.marketing.set(enabled('marketing'));
  }

  private toSignIn(problem: ApiProblem): boolean {
    if (problem.status !== 401) return false;
    void this.router.navigate(['/sign-in'], { queryParams: { returnUrl: '/account/emails' } });
    return true;
  }
}
