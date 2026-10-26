import { ChangeDetectionStrategy, Component, OnInit, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NotificationPreferencesApi, shortRef } from '@tutumuch/api';
import { ButtonDirective, IconComponent, SpinnerComponent } from '@tutumuch/components';

type State = 'working' | 'unsubscribed' | 'invalid' | 'error';

/**
 * The marketing email's unsubscribe link at /emails/unsubscribe?token=… (L2-043), public. The
 * token is posted once from script on load, because mail scanners prefetch links with GET.
 */
@Component({
  selector: 'tl-unsubscribe-page',
  imports: [RouterLink, ButtonDirective, IconComponent, SpinnerComponent],
  templateUrl: './unsubscribe-page.html',
  styleUrl: './unsubscribe-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UnsubscribePage implements OnInit {
  private readonly api = inject(NotificationPreferencesApi);

  /** The token query parameter, bound by the router. */
  readonly token = input<string>('');

  readonly state = signal<State>('working');
  readonly ref = signal('');

  ngOnInit(): void {
    void this.unsubscribe();
  }

  async unsubscribe(): Promise<void> {
    this.state.set('working');
    const result = await this.api.unsubscribe(this.token());
    if (result.ok) {
      this.state.set('unsubscribed');
      return;
    }
    if (result.problem.code === 'UNSUBSCRIBE_LINK_INVALID') {
      this.state.set('invalid');
      return;
    }
    this.ref.set(shortRef(result.problem.correlationId));
    this.state.set('error');
  }
}
