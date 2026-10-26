import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * The purchase actions of a listing (design system: Buy bar): the total with "incl. shipping"
 * and the projected actions. Sticky at the bottom of the screen below 992 px, inline from 992 px
 * (L2-063). With two actions the total is hidden on small screens.
 */
@Component({
  selector: 'tl-buy-bar',
  templateUrl: './buy-bar.html',
  styleUrl: './buy-bar.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BuyBarComponent {
  /** The formatted total, for example "$94.00". */
  readonly total = input.required<string>();
}
