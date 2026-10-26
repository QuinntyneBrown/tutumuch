import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';

import { ButtonDirective } from '../button/button';
import { IconComponent, IconName } from '../icon/icon';
import { DialogRef } from './dialog-ref';

let nextId = 0;

/**
 * Modal dialog frame: backdrop, panel, header with optional icon, title, description and close
 * button, a top slot for alerts, the body and a footer (design system: Dialog). Open it through
 * DialogService. Project `[description]`, `[top]` and `[footer]` content.
 */
@Component({
  selector: 'tl-dialog',
  imports: [ButtonDirective, IconComponent],
  templateUrl: './dialog.html',
  styleUrl: './dialog.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DialogComponent {
  private readonly ref = inject(DialogRef, { optional: true });

  readonly heading = input.required<string>();
  readonly icon = input<IconName>();
  /** The icon's colour: `danger` for a destructive action such as cancelling a sale. */
  readonly tone = input<'info' | 'danger'>('info');
  /** Panel width: `sm` for a short confirmation, `lg` for wide content. */
  readonly size = input<'sm' | 'md' | 'lg'>('md');

  readonly id = `tl-dialog-${nextId++}`;

  close(): void {
    this.ref?.close();
  }
}
