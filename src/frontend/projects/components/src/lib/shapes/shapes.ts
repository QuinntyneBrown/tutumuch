import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * Decorative chamfered blocks and a striped band on the showcase (design system: Shapes). Hidden
 * from assistive technology; prefers-reduced-motion stops the drift. `quiet` keeps the tone block
 * and the stripes only.
 */
@Component({
  selector: 'tl-shapes',
  templateUrl: './shapes.html',
  styleUrl: './shapes.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShapesComponent {
  readonly density = input<'full' | 'quiet'>('full');
}
