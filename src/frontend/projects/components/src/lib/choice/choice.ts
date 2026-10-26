import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/**
 * A checkbox with its label to the right and an optional error line (design system: Choice).
 * Project the native input first, then the label content.
 */
@Component({
  selector: 'tm-choice',
  templateUrl: './choice.html',
  styleUrl: './choice.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChoiceComponent {
  readonly inputId = input.required<string>();
  readonly error = input<string | null>();

  readonly invalid = computed(() => (this.error() ? 'true' : null));
  readonly describedBy = computed(() => (this.error() ? `${this.inputId()}-error` : null));
}
