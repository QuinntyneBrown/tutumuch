import { ChangeDetectionStrategy, Component, ElementRef, afterRenderEffect, input, viewChild } from '@angular/core';

import { IconComponent } from '../icon/icon';

/** One problem in the summary: the id of the invalid control and its message. */
export interface SummaryError {
  fieldId: string;
  message: string;
}

/**
 * Lists every problem of a refused form at the top, each a link to its control (design system:
 * Error summary, L2-065). It takes focus whenever a new list is shown and is announced as an alert.
 */
@Component({
  selector: 'tm-error-summary',
  imports: [IconComponent],
  templateUrl: './error-summary.html',
  styleUrl: './error-summary.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ErrorSummaryComponent {
  readonly errors = input.required<readonly SummaryError[]>();
  readonly heading = input('Fix these to continue');

  private readonly box = viewChild<ElementRef<HTMLElement>>('box');

  constructor() {
    afterRenderEffect(() => {
      if (this.errors().length) this.box()?.nativeElement.focus();
    });
  }

  jump(event: Event, fieldId: string): void {
    event.preventDefault();
    const target = document.getElementById(fieldId);
    target?.focus();
    target?.scrollIntoView({ block: 'center' });
  }
}
