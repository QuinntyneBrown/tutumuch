import { Locator, Page } from '@playwright/test';

/** Join TuTuMuch at /join (mocks pages/register). */
export class JoinPage {
  readonly heading: Locator;
  readonly displayName: Locator;
  readonly email: Locator;
  readonly password: Locator;
  readonly age: Locator;
  readonly terms: Locator;
  readonly submit: Locator;
  readonly errorSummary: Locator;
  readonly resend: Locator;
  /** "Continue with Google", rendered only when TuTuMuch API has a Google client (L2-088). */
  readonly google: Locator;

  constructor(private readonly page: Page) {
    this.heading = page.getByRole('heading', { level: 1 });
    this.displayName = page.getByLabel('Display name');
    this.email = page.getByLabel('Email');
    this.password = page.getByLabel('Password');
    this.age = page.getByLabel('I’m 18 or older');
    this.terms = page.getByRole('checkbox', { name: /I accept the terms/ });
    this.submit = page.getByRole('button', { name: 'Join TuTuMuch' });
    this.errorSummary = page.getByRole('alert').filter({ hasText: 'Fix these to continue' });
    this.resend = page.getByRole('button', { name: 'Resend email' });
    this.google = page.getByRole('link', { name: 'Continue with Google' });
  }

  async goto(): Promise<void> {
    await this.page.goto('/join');
  }

  async fill(values: {
    displayName: string;
    email: string;
    password: string;
    age?: boolean;
    terms?: boolean;
  }): Promise<void> {
    await this.displayName.fill(values.displayName);
    await this.email.fill(values.email);
    await this.password.fill(values.password);
    if (values.age ?? true) await this.age.check();
    if (values.terms ?? true) await this.terms.check();
  }

  fieldError(id: string): Locator {
    return this.page.locator(`#${id}-error`);
  }
}
