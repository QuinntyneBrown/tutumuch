import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { IconComponent } from '../icon/icon';

/**
 * A selected filter value with a remove button labelled "Remove filter {label}" (design system:
 * Chip). The application removes the value on (remove).
 */
@Component({
  selector: 'tm-chip',
  imports: [IconComponent],
  templateUrl: './chip.html',
  styleUrl: './chip.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChipComponent {
  readonly label = input.required<string>();
  readonly remove = output<void>();
}
