import { Locator, Page } from '@playwright/test';

/** The "Place a bid" dialog of an auction's detail page (mocks dialogs/place-bid). */
export class PlaceBidDialog {
  readonly dialog: Locator;
  readonly amount: Locator;
  readonly amountError: Locator;
  readonly placeBid: Locator;
  readonly cancel: Locator;
  readonly close: Locator;
  readonly alert: Locator;

  constructor(page: Page) {
    this.dialog = page.getByRole('dialog', { name: 'Place a bid' });
    this.amount = this.dialog.getByLabel('Your bid');
    this.amountError = this.dialog.locator('#bid-amount-error');
    this.placeBid = this.dialog.getByRole('button', { name: 'Place bid' });
    this.cancel = this.dialog.getByRole('button', { name: 'Cancel' });
    this.close = this.dialog.getByRole('button', { name: 'Close' });
    this.alert = this.dialog.locator('.alert');
  }
}
