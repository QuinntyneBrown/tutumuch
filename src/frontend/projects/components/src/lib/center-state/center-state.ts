import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { IconComponent, IconName } from '../icon/icon';

/**
 * A whole-page state centred in main: icon or status code, heading, projected text and an actions row
 * (design system: Center state). Project the actions with the `actions` attribute.
 */
@Component({
  selector: 'tl-center-state',
  imports: [IconComponent],
  templateUrl: './center-state.html',
  styleUrl: './center-state.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CenterStateComponent {
  readonly icon = input<IconName>();
  /** A status code shown large in place of the icon, for example "404". */
  readonly code = input<string>();
  readonly heading = input.required<string>();
}
