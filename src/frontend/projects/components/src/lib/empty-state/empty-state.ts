import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { IconComponent, IconName } from '../icon/icon';

/**
 * An empty or failed region inside a page: icon, heading, projected text and an actions row
 * (design system: Empty state). The error tone is compact; `compact` makes any tone compact,
 * for an empty section inside a page. Project the actions with `actions`.
 */
@Component({
  selector: 'tm-empty-state',
  imports: [IconComponent],
  templateUrl: './empty-state.html',
  styleUrl: './empty-state.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmptyStateComponent {
  readonly icon = input.required<IconName>();
  readonly heading = input.required<string>();
  readonly tone = input<'default' | 'error'>('default');
  readonly compact = input(false);
}
