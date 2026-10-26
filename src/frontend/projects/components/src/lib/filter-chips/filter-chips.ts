import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

/** One choice of a filter chip group: the value it selects, its label and the count after the label. */
export interface FilterChipOption {
  value: string;
  label: string;
  count: number;
}

/**
 * A group of toggle chips that picks one value, each with a count, such as the status filter of
 * "Your listings" (design system: Filter chips). The pressed chip has `aria-pressed="true"`; the
 * application keeps the value and changes it on (selectedChange).
 */
@Component({
  selector: 'tl-filter-chips',
  templateUrl: './filter-chips.html',
  styleUrl: './filter-chips.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FilterChipsComponent {
  readonly label = input.required<string>();
  readonly options = input.required<FilterChipOption[]>();
  readonly selected = input.required<string>();
  readonly selectedChange = output<string>();
}
