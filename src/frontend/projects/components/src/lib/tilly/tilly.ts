import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/** Tilly's approved poses (page guidelines 6, design system: Tilly). */
export type TillyPose =
  'hero' | 'dance' | 'soccer' | 'volleyball' | 'gymnastics' | 'swimming' | 'show-and-tell' | 'hello';

interface TillyArt {
  readonly file: string;
  readonly alt: string;
  readonly width: number;
  readonly height: number;
}

/** The artwork in public/tilly/ (copied from docs/print/characters/) and the alt text that names what she does. */
const POSES: Record<TillyPose, TillyArt> = {
  hero: {
    file: 'tilly-twirl.webp',
    alt: 'Tilly twirling in a raspberry tutu with a shopping bag',
    width: 442,
    height: 600,
  },
  dance: { file: 'tilly-dance.webp', alt: 'Tilly dancing an arabesque in her tutu', width: 418, height: 600 },
  soccer: { file: 'tilly-soccer.webp', alt: 'Tilly kicking a soccer ball', width: 418, height: 600 },
  volleyball: { file: 'tilly-volleyball.webp', alt: 'Tilly bumping a volleyball', width: 414, height: 600 },
  gymnastics: {
    file: 'tilly-gymnastics.webp',
    alt: 'Tilly landing a gymnastics move on a mat',
    width: 465,
    height: 600,
  },
  swimming: { file: 'tilly-swimming-tile.webp', alt: 'Tilly ready to dive into the pool', width: 600, height: 600 },
  'show-and-tell': {
    file: 'tilly-show-and-tell.webp',
    alt: 'Tilly holding up a yellow dress to pass it on',
    width: 391,
    height: 600,
  },
  hello: {
    file: 'tilly-hello.webp',
    alt: 'Tilly waving hello from a stack of folded clothes',
    width: 355,
    height: 600,
  },
};

/**
 * Tilly, TuTuMuch's character, in one approved pose (design system: Tilly). At most one per
 * screen; never cropped, recoloured, flipped or stretched. The alt text names her and what she is
 * doing. The swimming pose exists only as a framed tile.
 */
@Component({
  selector: 'tl-tilly',
  templateUrl: './tilly.html',
  styleUrl: './tilly.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TillyComponent {
  readonly pose = input.required<TillyPose>();
  /** hero: the marketing hero; header: a category header; empty: an empty state; confirm: a big moment. */
  readonly size = input<'hero' | 'header' | 'empty' | 'confirm'>('empty');

  readonly art = computed(() => POSES[this.pose()]);
}
