import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { IconComponent } from '../icon/icon';

/**
 * "{carrier} tracking" and the number, grouped in fours, linking to the carrier's tracking page
 * in a new tab (design system: Tracking link, L2-033). The URL comes from TuTuMuch API.
 */
@Component({
  selector: 'tl-tracking-link',
  imports: [IconComponent],
  templateUrl: './tracking-link.html',
  styleUrl: './tracking-link.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TrackingLinkComponent {
  readonly carrier = input.required<string>();
  readonly number = input.required<string>();
  readonly url = input.required<string>();

  readonly grouped = computed(() => this.number().replace(/(.{4})(?=.)/g, '$1 '));
}
