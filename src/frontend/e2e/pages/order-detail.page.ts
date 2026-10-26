import { Locator, Page } from '@playwright/test';

/** The buyer's order page at /orders/{id} and its Confirm receipt dialog (mocks pages/order-detail, dialogs/confirm-receipt). */
export class OrderDetailPage {
  readonly heading: Locator;
  readonly eyebrow: Locator;
  readonly timeline: Locator;
  readonly currentStep: Locator;
  readonly shipNote: Locator;
  readonly tracking: Locator;
  readonly alert: Locator;
  readonly summary: Locator;
  readonly loading: Locator;
  readonly centerState: Locator;
  readonly completesNote: Locator;
  readonly confirmReceipt: Locator;
  readonly dialog: Locator;
  readonly dialogAlert: Locator;
  readonly completeOrder: Locator;
  readonly notYet: Locator;
  readonly askToCancel: Locator;
  readonly requestNote: Locator;
  readonly quote: Locator;
  readonly shopSimilar: Locator;

  constructor(private readonly page: Page) {
    const main = page.locator('main');
    this.heading = main.getByRole('heading', { level: 1 });
    this.eyebrow = main.locator('.eyebrow');
    this.timeline = main.getByRole('list', { name: 'Order progress' });
    this.currentStep = this.timeline.locator('[aria-current="step"]');
    this.shipNote = main.locator('.order__note');
    this.tracking = main.locator('.facts-row');
    this.alert = main.locator('.alert');
    this.summary = main.getByRole('complementary', { name: 'Order summary' });
    this.loading = main.locator('[aria-busy="true"]');
    this.centerState = main.locator('.center-state');
    this.completesNote = main.locator('.order__completes');
    this.confirmReceipt = main.getByRole('button', { name: 'I received it' });
    this.dialog = page.getByRole('dialog', { name: 'Got it and all good?' });
    this.dialogAlert = this.dialog.getByRole('alert');
    this.completeOrder = this.dialog.locator('.dialog__footer .btn--primary');
    this.notYet = this.dialog.getByRole('button', { name: 'Not yet' });
    this.askToCancel = main.getByRole('button', { name: 'Ask to cancel' });
    this.requestNote = main.locator('.order__request');
    this.quote = main.locator('blockquote');
    this.shopSimilar = main.getByRole('link', { name: 'Shop similar items' });
  }

  async goto(id = 'o-1'): Promise<void> {
    await this.page.goto(`/orders/${id}`);
  }
}
