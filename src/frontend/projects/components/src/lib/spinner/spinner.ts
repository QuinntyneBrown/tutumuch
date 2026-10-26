import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** Indeterminate spinner with a status message read by screen readers (design system: Spinner). */
@Component({
  selector: 'tm-spinner',
  templateUrl: './spinner.html',
  styleUrl: './spinner.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SpinnerComponent {
  readonly label = input.required<string>();
}
