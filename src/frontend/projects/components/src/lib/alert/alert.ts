import { ChangeDetectionStrategy, Component, booleanAttribute, computed, input, output } from '@angular/core';

import { IconComponent, IconName } from '../icon/icon';

export type AlertTone = 'info' | 'success' | 'warning' | 'danger';

const ICONS: Record<AlertTone, IconName> = {
  info: 'info',
  success: 'check-circle',
  warning: 'alert',
  danger: 'x-circle',
};

/**
 * Inline message with an icon, a title and projected text (design system: Alert). Danger alerts
 * are announced at once (role="alert"); pass live="polite" for a status message such as success.
 * Project links or buttons with the `actions` attribute; a dismissible alert has a Dismiss button.
 */
@Component({
  selector: 'tm-alert',
  imports: [IconComponent],
  templateUrl: './alert.html',
  styleUrl: './alert.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AlertComponent {
  readonly tone = input<AlertTone>('info');
  readonly heading = input<string>();
  /** Full-width page banner above the top bar, without a title. */
  readonly banner = input(false, { transform: booleanAttribute });
  readonly icon = input<IconName>();
  readonly live = input<'assertive' | 'polite' | 'off'>();
  /** Shows the Dismiss button; the owner hides the alert on (dismissed). */
  readonly dismissible = input(false, { transform: booleanAttribute });
  readonly dismissed = output<void>();

  readonly iconName = computed(() => this.icon() ?? ICONS[this.tone()]);
  readonly ariaRole = computed(() => {
    const live = this.live() ?? (this.tone() === 'danger' ? 'assertive' : 'off');
    return live === 'assertive' ? 'alert' : live === 'polite' ? 'status' : null;
  });
}
