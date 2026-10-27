import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { IconComponent, IconName } from '../icon/icon';
import { TillyComponent, TillyPose } from '../tilly/tilly';

/**
 * A whole-page state centred in main: Tilly, an icon or a status code, heading, projected text and
 * an actions row (design system: Center state). `tilly` is for big moments only, such as the "It's
 * live" new-listing confirmation with her Show & Tell pose (page guidelines 6). Project the actions
 * with the `actions` attribute.
 */
@Component({
  selector: 'tl-center-state',
  imports: [IconComponent, TillyComponent],
  templateUrl: './center-state.html',
  styleUrl: './center-state.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CenterStateComponent {
  readonly icon = input<IconName>();
  /** Tilly in place of the icon, for a big moment. */
  readonly tilly = input<TillyPose>();
  /** A status code shown large in place of the icon, for example "404". */
  readonly code = input<string>();
  readonly heading = input.required<string>();
}
