import { Locator, Page } from '@playwright/test';

/** Verify email at /verify?token=… (mocks pages/verify-email). */
export class VerifyEmailPage {
  readonly heading: Locator;
  readonly sellOutgrownGear: Locator;
  readonly signIn: Locator;

  constructor(private readonly page: Page) {
    this.heading = page.getByRole('heading', { level: 1 });
    this.sellOutgrownGear = page.getByRole('link', { name: 'Sell outgrown gear' });
    this.signIn = page.getByRole('link', { name: 'Sign in' });
  }

  async goto(token: string): Promise<void> {
    await this.page.goto(`/verify?token=${encodeURIComponent(token)}`);
  }
}
