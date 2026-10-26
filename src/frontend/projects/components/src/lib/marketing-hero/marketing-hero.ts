import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { ShapesComponent } from '../shapes/shapes';

/**
 * Ink showcase band of the marketing page (design system: Marketing hero): shapes, eyebrow, a
 * headline projected as `[title]` (h1 for `hero`, h2 for `closing`), one lede line, `[actions]`
 * and, in the hero, `[media]` in the second column.
 */
@Component({
  selector: 'tl-marketing-hero',
  imports: [NgTemplateOutlet, ShapesComponent],
  templateUrl: './marketing-hero.html',
  styleUrl: './marketing-hero.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MarketingHeroComponent {
  readonly variant = input<'hero' | 'closing'>('hero');
  readonly eyebrow = input('');
  readonly lede = input('');
  /** Id of the headline, which labels the section. */
  readonly headingId = input.required<string>();
}
