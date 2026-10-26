import { ChangeDetectionStrategy, Component, input } from '@angular/core';

let nextId = 0;

/**
 * A required choice of one: a fieldset with its legend, the projected `tm-choice` radios and an
 * error line under them that the fieldset references (design system: Radio group). Set
 * aria-invalid on the radios while `error` is shown.
 */
@Component({
  selector: 'tm-radio-group',
  templateUrl: './radio-group.html',
  styleUrl: './radio-group.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RadioGroupComponent {
  readonly legend = input.required<string>();
  readonly error = input<string | null>();

  readonly errorId = `tm-radio-group-${nextId++}-error`;
}
