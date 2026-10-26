import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { SizeTagComponent } from '../size-tag/size-tag';

/**
 * Read-only size set by the category: the size tag, "Size: Men's US 12" and "Set by category"
 * (design system: Fixed size, L2-012). There is never a size input.
 */
@Component({
  selector: 'tm-fixed-size',
  imports: [SizeTagComponent],
  templateUrl: './fixed-size.html',
  styleUrl: './fixed-size.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FixedSizeComponent {
  /** Accepted size, for example "Men's US 12". */
  readonly size = input.required<string>();
  /** Short label for the tag, for example "US 12". */
  readonly sizeLabel = input.required<string>();
}
