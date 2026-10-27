import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { IconComponent, IconName } from '../icon/icon';
import { TillyComponent } from '../tilly/tilly';

/**
 * An empty or failed region inside a page (design system: Empty state, L2-096): Tilly waving
 * hello, a heading, one short projected line and one action projected with `actions`. The error
 * tone shows its raspberry icon instead of Tilly and is compact; so does `[tilly]="false"`, for a
 * screen that already shows Tilly. `compact` makes any tone compact, for a section inside a page.
 */
@Component({
  selector: 'tl-empty-state',
  imports: [IconComponent, TillyComponent],
  templateUrl: './empty-state.html',
  styleUrl: './empty-state.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmptyStateComponent {
  readonly icon = input.required<IconName>();
  readonly heading = input.required<string>();
  readonly tone = input<'default' | 'error'>('default');
  readonly compact = input(false);
  /** Tilly's "Hello, friend" pose; off for errors and for screens that already show Tilly. */
  readonly tilly = input(true);
}
