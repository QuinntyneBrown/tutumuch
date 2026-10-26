import { Locator, Page } from '@playwright/test';

/** Checkout at /checkout/{reference} (mocks pages/checkout and notifications/reservation-timer). */
export class CheckoutPage {
  readonly heading: Locator;
  readonly timer: Locator;
  readonly timerBanner: Locator;
  readonly savedAddress: Locator;
  readonly newAddress: Locator;
  readonly fullName: Locator;
  readonly street: Locator;
  readonly apartment: Locator;
  readonly city: Locator;
  readonly state: Locator;
  readonly zip: Locator;
  readonly country: Locator;
  readonly errorSummary: Locator;
  readonly formAlert: Locator;
  readonly card: Locator;
  readonly testCard: Locator;
  readonly secureNote: Locator;
  readonly pay: Locator;
  readonly paying: Locator;
  readonly cancel: Locator;
  readonly summary: Locator;
  readonly loading: Locator;
  readonly centerState: Locator;

  constructor(private readonly page: Page) {
    this.heading = page.locator('main').getByRole('heading', { level: 1 });
    this.timer = page.getByRole('timer');
    this.timerBanner = page.locator('.checkout__banner');
    this.savedAddress = page.getByRole('radio', { name: /Marcus Bell/ });
    this.newAddress = page.getByRole('radio', { name: 'New address' });
    this.fullName = page.getByLabel('Full name');
    this.street = page.getByLabel('Street address');
    this.apartment = page.getByLabel('Apartment, suite');
    this.city = page.getByLabel('City');
    this.state = page.getByLabel('State');
    this.zip = page.getByLabel('ZIP code');
    this.country = page.getByLabel('Country');
    this.errorSummary = page.locator('.form-summary');
    this.formAlert = page.locator('form .alert').first();
    this.card = page.getByRole('group', { name: 'Card' });
    this.testCard = page.getByLabel('Test card');
    this.secureNote = page.locator('.secure-note');
    this.pay = page.getByRole('button', { name: /^Pay|^Continue/ });
    this.paying = page.getByText('Confirming with your bank…');
    this.cancel = page.getByRole('button', { name: 'Cancel and release item' });
    this.summary = page.getByRole('complementary', { name: 'Summary' });
    this.loading = page.locator('[aria-busy="true"][aria-label="Loading checkout"]');
    this.centerState = page.locator('.center-state');
  }

  async goto(reference = 'L-10001'): Promise<void> {
    await this.page.goto(`/checkout/${reference}`);
  }
}
