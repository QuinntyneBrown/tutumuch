import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { ButtonDirective } from '../button/button';

/**
 * "Showing {shown} of {total}" with a Load more button that is busy while the next page loads
 * and hidden once everything is shown (design system: Load more).
 */
@Component({
  selector: 'tl-load-more',
  imports: [ButtonDirective, DecimalPipe],
  templateUrl: './load-more.html',
  styleUrl: './load-more.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoadMoreComponent {
  readonly shown = input.required<number>();
  readonly total = input.required<number>();
  readonly busy = input(false);
  /** Size of the next page, for the hidden note on the button. */
  readonly pageSize = input(24);
  readonly loadMore = output();
}
