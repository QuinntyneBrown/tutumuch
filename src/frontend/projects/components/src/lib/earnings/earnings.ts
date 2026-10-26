import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** "You earn after the {fee}% fee" with the amount, or a dash while there is none (design system: Earnings, L2-031). */
@Component({
  selector: 'tm-earnings',
  templateUrl: './earnings.html',
  styleUrl: './earnings.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EarningsComponent {
  readonly feePercent = input.required<number>();
  /** Formatted amount, for example "$61.20"; null shows "—". */
  readonly amount = input<string | null>(null);
}
