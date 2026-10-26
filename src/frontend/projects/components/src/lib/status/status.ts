import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export type StatusTone = 'draft' | 'active' | 'reserved' | 'sold' | 'removed' | 'info';

/** A status word after a coloured square, such as an order's status (design system: Status). */
@Component({
  selector: 'tl-status',
  templateUrl: './status.html',
  styleUrl: './status.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatusComponent {
  readonly tone = input<StatusTone>('info');
}
