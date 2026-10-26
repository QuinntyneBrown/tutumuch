import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** A member's own words in curly quotes with their display name below, such as a seller's note (design system: Quote). */
@Component({
  selector: 'tm-quote',
  templateUrl: './quote.html',
  styleUrl: './quote.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QuoteComponent {
  readonly text = input.required<string>();
  /** The display name of who wrote it. */
  readonly cite = input.required<string>();
}
