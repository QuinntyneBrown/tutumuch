import { Locator, Page } from '@playwright/test';

/** The order confirmation at /orders/{id}/confirmed (mocks pages/order-confirmation). */
export class OrderConfirmationPage {
  readonly heading: Locator;
  readonly reference: Locator;
  readonly item: Locator;
  readonly nextSteps: Locator;
  readonly viewOrder: Locator;
  readonly keepShopping: Locator;
  readonly centerState: Locator;

  constructor(private readonly page: Page) {
    this.heading = page.locator('main').getByRole('heading', { level: 1 });
    this.reference = page.locator('.confirm .eyebrow');
    this.item = page.locator('.confirm__item');
    this.nextSteps = page.locator('ol.next-steps > li');
    this.viewOrder = page.getByRole('link', { name: 'View order' });
    this.keepShopping = page.getByRole('link', { name: 'Keep shopping' });
    this.centerState = page.locator('.center-state');
  }

  async goto(orderId = 'o-1'): Promise<void> {
    await this.page.goto(`/orders/${orderId}/confirmed`);
  }
}
