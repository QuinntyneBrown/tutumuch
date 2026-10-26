import { ChangeDetectionStrategy, Component, booleanAttribute, input, model } from '@angular/core';

/**
 * An on or off setting: a native checkbox with role="switch" (design system: Switch). Without a
 * hint its label is visually hidden, for a row that shows the title itself; with a hint the
 * label and hint are shown beside the switch. A locked setting is checked and disabled.
 */
@Component({
  selector: 'tm-switch',
  templateUrl: './switch.html',
  styleUrl: './switch.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SwitchComponent {
  readonly label = input.required<string>();
  /** A short visible description under a visible label, as in the Cookie settings dialog. */
  readonly hint = input<string>();
  readonly checked = model(false);
  readonly disabled = input(false, { transform: booleanAttribute });
  /** Takes the first focus when its dialog opens. */
  readonly autofocus = input(false, { transform: booleanAttribute });
}
