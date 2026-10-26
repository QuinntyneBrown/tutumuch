import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';

import { ButtonDirective } from '../button/button';
import { DialogRef } from '../dialog/dialog-ref';
import { IconComponent } from '../icon/icon';

let nextId = 0;

/**
 * Full-screen modal sheet with a header, scrolling body and footer (design system: Sheet). With
 * `side`, it becomes a 24rem panel on the left from 768px. Open it through SheetService; project
 * the body and `[footer]` content.
 */
@Component({
  selector: 'tm-sheet',
  imports: [ButtonDirective, IconComponent],
  templateUrl: './sheet.html',
  styleUrl: './sheet.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SheetComponent {
  private readonly ref = inject(DialogRef, { optional: true });

  readonly heading = input.required<string>();
  readonly closeLabel = input('Close');
  /** A side panel from 768px instead of full screen (the filter sheet). */
  readonly side = input(false);

  readonly id = `tm-sheet-${nextId++}`;

  close(): void {
    this.ref?.close();
  }
}
