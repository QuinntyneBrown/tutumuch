import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * Showcase band of the marketing page (design system: Marketing hero), a solid blush panel with
 * no shapes or gradients (L2-096): eyebrow, a headline projected as `[title]` (h1 for `hero`, h2
 * for `closing`), one lede line, `[actions]` and, in the hero, `[media]` (Tilly and the video
 * poster) in the second column. `[backdrop]` sits behind the content: only `tl-tulle-field`, the
 * one decorative layer allowed (ADR general/0010).
 */
@Component({
  selector: 'tl-marketing-hero',
  imports: [NgTemplateOutlet],
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
