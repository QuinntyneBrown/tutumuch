import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** A projected input with a leading addon such as "$" (design system: Input group). */
@Component({
  selector: 'tl-input-group',
  templateUrl: './input-group.html',
  styleUrl: './input-group.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InputGroupComponent {
  readonly addon = input.required<string>();
}
