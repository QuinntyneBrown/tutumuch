import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** One numbered step. */
export interface Step {
  title: string;
  text: string;
}

/** Numbered steps on a red top rule, titles as h3 (design system: Steps). */
@Component({
  selector: 'tl-steps',
  templateUrl: './steps.html',
  styleUrl: './steps.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StepsComponent {
  readonly steps = input.required<Step[]>();
}
