import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * One item on one line: optional projected thumbnail, title and a muted meta line (design
 * system: Item row). Project the thumbnail with the `thumb` attribute;
 * other content, such as a `tm-status`, goes under the title.
 */
@Component({
  selector: 'tm-item-row',
  templateUrl: './item-row.html',
  styleUrl: './item-row.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ItemRowComponent {
  readonly title = input.required<string>();
  readonly meta = input<string>('');
}
