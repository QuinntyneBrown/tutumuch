import { Locator, Page } from '@playwright/test';

/** The Request this item dialog over a showcase listing (mocks dialogs/request-showcase-item). */
export class RequestShowcaseItemDialog {
  readonly dialog: Locator;
  readonly note: Locator;
  readonly send: Locator;
  readonly update: Locator;
  readonly tryAgain: Locator;
  readonly cancel: Locator;
  readonly summary: Locator;
  readonly error: Locator;
  readonly alert: Locator;
  readonly requested: Locator;

  constructor(page: Page) {
    this.dialog = page.getByRole('dialog', { name: 'Request this item' });
    this.note = this.dialog.getByLabel('Note');
    this.send = this.dialog.getByRole('button', { name: 'Send request' });
    this.update = this.dialog.getByRole('button', { name: 'Update request' });
    this.tryAgain = this.dialog.getByRole('button', { name: 'Try again' });
    this.cancel = this.dialog.getByRole('button', { name: 'Cancel' });
    this.summary = this.dialog.locator('.form-summary');
    this.error = this.dialog.locator('#rq-note-error');
    this.alert = this.dialog.locator('.alert');
    this.requested = this.dialog.getByText(/^You asked for this on/);
  }
}
