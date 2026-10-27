import { Locator, Page } from '@playwright/test';

/** Finish joining with Google at /join/google (mocks pages/join-google). */
export class GoogleJoinPage {
  readonly heading: Locator;
  readonly email: Locator;
  readonly displayName: Locator;
  readonly age: Locator;
  readonly terms: Locator;
  readonly submit: Locator;
  readonly errorSummary: Locator;
  readonly alert: Locator;
  readonly startAgain: Locator;
  readonly backToSignIn: Locator;

  constructor(private readonly page: Page) {
    this.heading = page.getByRole('heading', { level: 1 });
    this.email = page.getByLabel('Google email');
    this.displayName = page.getByLabel('Display name');
    this.age = page.getByLabel('I’m 18 or older');
    this.terms = page.getByRole('checkbox', { name: /I accept the terms/ });
    this.submit = page.getByRole('button', { name: 'Join TuTuMuch' });
    this.errorSummary = page.getByRole('alert').filter({ hasText: 'Fix these to continue' });
    this.alert = page.getByRole('alert');
    this.startAgain = page.getByRole('link', { name: 'Continue with Google again' });
    this.backToSignIn = page.getByRole('link', { name: 'Back to sign in' });
  }

  async goto(returnUrl?: string): Promise<void> {
    await this.page.goto(returnUrl ? `/join/google?returnUrl=${encodeURIComponent(returnUrl)}` : '/join/google');
  }

  async fill(values: { displayName: string; age?: boolean; terms?: boolean }): Promise<void> {
    await this.displayName.fill(values.displayName);
    if (values.age ?? true) await this.age.check();
    if (values.terms ?? true) await this.terms.check();
  }

  fieldError(id: string): Locator {
    return this.page.locator(`#${id}-error`);
  }
}
