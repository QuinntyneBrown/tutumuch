import { Injectable, Type, inject } from '@angular/core';

import { DialogRef } from '../dialog/dialog-ref';
import { DialogService } from '../dialog/dialog-service';

/**
 * Opens a component whose template is a tl-sheet (design system: Sheet). Sheets behave as modal
 * dialogs: the page becomes inert, Escape closes and focus returns to the opener on close.
 */
@Injectable({ providedIn: 'root' })
export class SheetService {
  private readonly dialogs = inject(DialogService);

  open<C, D = unknown, R = unknown>(component: Type<C>, data?: D): DialogRef<R> {
    return this.dialogs.open<C, D, R>(component, data);
  }
}
