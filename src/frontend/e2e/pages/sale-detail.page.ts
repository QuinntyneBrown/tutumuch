import { Locator, Page } from '@playwright/test';

/** The seller's sale page at /sales/{id} and its Add tracking dialog (mocks pages/sale-detail, dialogs/add-tracking). */
export class SaleDetailPage {
  readonly heading: Locator;
  readonly eyebrow: Locator;
  readonly shipBy: Locator;
  readonly shipTo: Locator;
  readonly timeline: Locator;
  readonly currentStep: Locator;
  readonly tracking: Locator;
  readonly summary: Locator;
  readonly addTracking: Locator;
  readonly alert: Locator;
  readonly loading: Locator;
  readonly centerState: Locator;
  readonly dialog: Locator;
  readonly carrier: Locator;
  readonly trackingNumber: Locator;
  readonly trackingError: Locator;
  readonly dialogAlert: Locator;
  readonly markShipped: Locator;
  readonly cancel: Locator;
  readonly cancelOrder: Locator;
  readonly quote: Locator;
  readonly respond: Locator;
  readonly relistItem: Locator;

  constructor(private readonly page: Page) {
    const main = page.locator('main');
    this.heading = main.getByRole('heading', { level: 1 });
    this.eyebrow = main.locator('.eyebrow');
    this.shipBy = main.locator('.lead');
    this.shipTo = main.locator('address');
    this.timeline = main.getByRole('list', { name: 'Order progress' });
    this.currentStep = this.timeline.locator('[aria-current="step"]');
    this.tracking = main.locator('.facts-row');
    this.summary = main.getByRole('complementary', { name: 'Order summary' });
    this.addTracking = main.getByRole('button', { name: 'Add tracking' });
    this.alert = main.locator('.alert');
    this.loading = main.locator('[aria-busy="true"]');
    this.centerState = main.locator('.center-state');
    this.dialog = page.getByRole('dialog', { name: 'Add tracking' });
    this.carrier = this.dialog.getByLabel('Carrier');
    this.trackingNumber = this.dialog.getByLabel('Tracking number');
    this.trackingError = this.dialog.locator('.field__error');
    this.dialogAlert = this.dialog.getByRole('alert');
    this.markShipped = this.dialog.locator('.dialog__footer .btn--primary');
    this.cancel = this.dialog.getByRole('button', { name: 'Cancel' });
    this.cancelOrder = main.getByRole('button', { name: 'Cancel order' });
    this.quote = main.locator('blockquote');
    this.respond = main.getByRole('button', { name: 'Respond' });
    this.relistItem = main.getByRole('button', { name: 'Relist item' });
  }

  async goto(id = 'o-1'): Promise<void> {
    await this.page.goto(`/sales/${id}`);
  }
}
