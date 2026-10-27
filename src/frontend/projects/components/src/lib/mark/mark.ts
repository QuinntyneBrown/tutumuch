import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

let nextId = 0;

/**
 * The TuTuMuch mark (brand kit `markScene`, variant `tGingham`): a gingham "T" turned -6 degrees on
 * a blush circle, with an ink outline and a raspberry shadow (design system: Mark). The kit's
 * geometry on a 100-unit box: gingham cell 5, letter 76, baseline 46 + 0.35 x 76, shadow offset
 * 0.07 and 0.09 x 76, outline 0.1 x 76 with round joins. Decorative: the compact header puts it
 * inside the named home link, the footer and short waits show it beside their own text. Never
 * smaller than 16px.
 */
@Component({
  selector: 'tl-mark',
  templateUrl: './mark.html',
  styleUrl: './mark.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MarkComponent {
  /** Rendered width and height in CSS pixels; at least 16. */
  readonly size = input(36);

  readonly px = computed(() => Math.max(16, this.size()));
  /** One pattern id per instance: a pattern inside a hidden SVG does not paint for the others. */
  readonly patternId = `tl-mark-gingham-${nextId++}`;
}
