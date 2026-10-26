import { Locator, Page } from '@playwright/test';

import { SellPage } from './sell.page';

/** Edit listing at /items/{reference}/edit (mocks pages/edit-listing); the fields are those of the sell form. */
export class EditListingPage {
  readonly form: SellPage;
  readonly heading: Locator;
  readonly listingLink: Locator;
  readonly status: Locator;
  readonly saveChanges: Locator;
  readonly cancel: Locator;
  readonly savedAlert: Locator;
  readonly changedAlert: Locator;
  readonly reload: Locator;
  readonly loading: Locator;
  readonly centerState: Locator;
  readonly tryAgain: Locator;

  constructor(private readonly page: Page) {
    const main = page.locator('main');
    this.form = new SellPage(page);
    this.heading = main.getByRole('heading', { level: 1 });
    this.listingLink = main.locator('.page-header a').first();
    this.status = main.locator('.page-header .status');
    this.saveChanges = main.getByRole('button', { name: 'Save changes' });
    this.cancel = main.getByRole('link', { name: 'Cancel' });
    this.savedAlert = main.locator('.alert').filter({ hasText: 'Changes saved' });
    this.changedAlert = main.locator('.alert').filter({ hasText: 'This listing changed since you opened it' });
    this.reload = main.getByRole('button', { name: 'Reload' });
    this.loading = main.locator('[aria-busy="true"][aria-label="Loading listing"]');
    this.centerState = main.locator('.center-state');
    this.tryAgain = main.getByRole('button', { name: 'Try again' });
  }

  async goto(reference = 'L-10001'): Promise<void> {
    await this.page.goto(`/items/${reference}/edit`);
  }
}

/** "Withdraw “{title}”?" (mocks dialogs/withdraw-listing), opened from the seller's own listing. */
export class WithdrawListingDialog {
  readonly root: Locator;
  readonly alert: Locator;
  readonly keep: Locator;
  readonly withdraw: Locator;

  constructor(page: Page, title: string) {
    this.root = page.getByRole('dialog', { name: `Withdraw “${title}”?` });
    this.alert = this.root.getByRole('alert');
    this.keep = this.root.getByRole('button', { name: 'Keep listing' });
    this.withdraw = this.root.locator('.dialog__footer .btn--danger');
  }
}
