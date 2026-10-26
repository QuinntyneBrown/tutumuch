import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { StatusComponent, StatusTone } from '../status/status';

/** One fact: its label and value; a status tone shows the value as `tm-status`, numeric aligns figures. */
export interface Fact {
  label: string;
  value: string;
  status?: StatusTone;
  numeric?: boolean;
}

/** Label and value pairs in two columns, such as an order's facts on a side panel (design system: Facts). */
@Component({
  selector: 'tm-facts',
  imports: [StatusComponent],
  templateUrl: './facts.html',
  styleUrl: './facts.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FactsComponent {
  readonly facts = input.required<readonly Fact[]>();
}
