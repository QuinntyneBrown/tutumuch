import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * A blush placeholder block shaped like the content it stands for (design system: Skeleton). The
 * element itself is the block, so a screen can size it with its own classes. Page loads use
 * skeletons, never a spinner; the region that holds them sets `aria-busy="true"`.
 */
@Component({
  selector: 'tl-skeleton',
  template: '',
  host: {
    class: 'skeleton',
    'aria-hidden': 'true',
    '[class.skeleton--text]': "shape() === 'text'",
    '[class.skeleton--title]': "shape() === 'title'",
    '[class.skeleton--photo]': "shape() === 'photo'",
    '[class.skeleton--rect]': "shape() === 'rect'",
    '[class.skeleton--circle]': "shape() === 'circle'",
    '[style.width]': 'width()',
    '[style.height]': 'height()',
  },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SkeletonComponent {
  readonly shape = input<'text' | 'title' | 'photo' | 'rect' | 'circle'>('text');
  /** A CSS width such as "40%"; empty fills the line. */
  readonly width = input<string | null>(null);
  readonly height = input<string | null>(null);
}
