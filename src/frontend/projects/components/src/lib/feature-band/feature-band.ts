import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** One point of a feature band: a bold title and one muted line. */
export interface FeaturePoint {
  title: string;
  text: string;
}

/**
 * Generous marketing band (design system: Feature band): eyebrow, h2, `[actions]`, then up to
 * three points or the projected body (such as tl-steps). `surface` alternates the background;
 * `stacked` puts the body under the heading.
 */
@Component({
  selector: 'tl-feature-band',
  templateUrl: './feature-band.html',
  styleUrl: './feature-band.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FeatureBandComponent {
  readonly eyebrow = input('');
  readonly heading = input.required<string>();
  readonly headingId = input.required<string>();
  readonly points = input<FeaturePoint[]>([]);
  readonly tone = input<'default' | 'surface'>('default');
  readonly stacked = input(false);
}
