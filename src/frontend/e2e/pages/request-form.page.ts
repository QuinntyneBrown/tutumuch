import { Locator, Page } from '@playwright/test';

/** Request an item at /account/requests/new and edit one at /account/requests/{id}/edit (mocks pages/request-form). */
export class RequestFormPage {
  readonly heading: Locator;
  readonly category: Locator;
  readonly size: Locator;
  readonly sizeInputs: Locator;
  readonly keywords: Locator;
  readonly maxPrice: Locator;
  readonly condition: Locator;
  readonly save: Locator;
  readonly cancel: Locator;
  readonly summary: Locator;
  readonly state: Locator;

  constructor(private readonly page: Page) {
    const main = page.locator('main');
    this.heading = main.getByRole('heading', { level: 1 });
    this.category = main.getByLabel('Category');
    this.size = main.locator('.fixed-size');
    this.sizeInputs = main.getByLabel(/size/i);
    this.keywords = main.getByLabel('Keywords');
    this.maxPrice = main.getByLabel('Highest price');
    this.condition = main.getByLabel('Lowest condition');
    this.save = main.getByRole('button', { name: 'Save request' });
    this.cancel = main.getByRole('link', { name: 'Cancel' });
    this.summary = main.locator('.form-summary');
    this.state = main.locator('.center-state');
  }

  async goto(query = ''): Promise<void> {
    await this.page.goto(`/account/requests/new${query}`);
  }

  async gotoEdit(id: string): Promise<void> {
    await this.page.goto(`/account/requests/${id}/edit`);
  }

  /** The inline error of a field, by its id: cat, keywords, max or cond. */
  error(fieldId: string): Locator {
    return this.page.locator(`#${fieldId}-error`);
  }
}
