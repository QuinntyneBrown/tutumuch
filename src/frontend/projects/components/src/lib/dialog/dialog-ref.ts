import { InjectionToken } from '@angular/core';

/** Data passed to the component a DialogService opens. */
export const DIALOG_DATA = new InjectionToken<unknown>('DIALOG_DATA');

/** Handle to an open dialog; the dialog's component injects it to close itself. */
export class DialogRef<R = unknown> {
  private resolve!: (result: R | undefined) => void;

  /** Resolves with the result once the dialog has closed. */
  readonly closed = new Promise<R | undefined>((resolve) => (this.resolve = resolve));

  constructor(private readonly onClose: () => void) {}

  close(result?: R): void {
    this.onClose();
    this.resolve(result);
  }
}
