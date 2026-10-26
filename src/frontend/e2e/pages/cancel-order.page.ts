import { Locator, Page } from '@playwright/test';

/** "Cancel this sale?" (mocks dialogs/cancel-order): reason radios, optional note, Keep sale and Cancel sale. */
export class CancelOrderDialog {
  readonly root: Locator;
  readonly reasons: Locator;
  readonly reasonError: Locator;
  readonly note: Locator;
  readonly alert: Locator;
  readonly keep: Locator;
  readonly submit: Locator;

  constructor(page: Page) {
    this.root = page.getByRole('dialog', { name: 'Cancel this sale?' });
    this.reasons = this.root.getByRole('radio');
    this.reasonError = this.root.locator('.field__error');
    this.note = this.root.getByLabel('Note to buyer');
    this.alert = this.root.getByRole('alert');
    this.keep = this.root.getByRole('button', { name: 'Keep sale' });
    this.submit = this.root.locator('.dialog__footer .btn--danger');
  }

  reason(label: string): Locator {
    return this.root.getByRole('radio', { name: label });
  }
}

/** "{buyer} asked to cancel" (mocks dialogs/respond-cancellation): the quoted message, Decline and Accept and refund. */
export class RespondCancellationDialog {
  readonly root: Locator;
  readonly quote: Locator;
  readonly alert: Locator;
  readonly decline: Locator;
  readonly accept: Locator;

  constructor(page: Page, buyer: string) {
    this.root = page.getByRole('dialog', { name: `${buyer} asked to cancel` });
    this.quote = this.root.locator('blockquote');
    this.alert = this.root.getByRole('alert');
    this.decline = this.root.getByRole('button', { name: 'Decline' });
    this.accept = this.root.locator('.dialog__footer .btn--primary');
  }
}

/** "Relist this item?" (mocks dialogs/relist): the item with its price and shipping, Not now and Relist. */
export class RelistDialog {
  readonly root: Locator;
  readonly item: Locator;
  readonly alert: Locator;
  readonly notNow: Locator;
  readonly relist: Locator;

  constructor(page: Page) {
    this.root = page.getByRole('dialog', { name: 'Relist this item?' });
    this.item = this.root.locator('.item-row');
    this.alert = this.root.getByRole('alert');
    this.notNow = this.root.getByRole('button', { name: 'Not now' });
    this.relist = this.root.locator('.dialog__footer .btn--primary');
  }
}

/** "Ask to cancel?" (mocks dialogs/request-cancellation): optional message, Keep order and Send request. */
export class RequestCancellationDialog {
  readonly root: Locator;
  readonly message: Locator;
  readonly alert: Locator;
  readonly keep: Locator;
  readonly send: Locator;

  constructor(page: Page, seller: string) {
    this.root = page.getByRole('dialog', { name: 'Ask to cancel?' });
    this.message = this.root.getByLabel(`Message to ${seller}`);
    this.alert = this.root.getByRole('alert');
    this.keep = this.root.getByRole('button', { name: 'Keep order' });
    this.send = this.root.locator('.dialog__footer .btn--primary');
  }
}
