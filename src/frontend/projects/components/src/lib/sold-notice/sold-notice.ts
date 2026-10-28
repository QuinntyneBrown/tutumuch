import { ChangeDetectionStrategy, Component, ElementRef, input, viewChild } from '@angular/core';

let nextId = 0;

/**
 * Takes the buy bar's place after Buy now on a showcase listing (design system: Sold notice,
 * L2-108): the Sold label, a heading, projected text and an actions row. It is announced politely;
 * call focus() to move focus to the heading once the button that had it is gone. Project the
 * action with the `actions` attribute.
 */
@Component({
  selector: 'tl-sold-notice',
  templateUrl: './sold-notice.html',
  styleUrl: './sold-notice.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SoldNoticeComponent {
  readonly heading = input('This one has already found a home');

  readonly id = `tl-sold-notice-${nextId++}`;
  private readonly title = viewChild.required<ElementRef<HTMLElement>>('title');

  focus(): void {
    this.title().nativeElement.focus();
  }
}
