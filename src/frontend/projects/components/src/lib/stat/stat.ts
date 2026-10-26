import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** One figure with its label, such as "Paid out $33.30"; group several in a `.stats` row (design system: Stat). */
@Component({
  selector: 'tm-stat',
  templateUrl: './stat.html',
  styleUrl: './stat.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatComponent {
  readonly label = input.required<string>();
  readonly value = input.required<string>();
}
