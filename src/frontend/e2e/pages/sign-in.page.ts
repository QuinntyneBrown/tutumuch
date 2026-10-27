import { Locator, Page } from '@playwright/test';

/** Sign in at /sign-in (mocks pages/sign-in). */
export class SignInPage {
  readonly heading: Locator;
  readonly email: Locator;
  readonly password: Locator;
  readonly submit: Locator;
  readonly alert: Locator;
  /** "Continue with Google", rendered only when TuTuMuch API has a Google client (L2-088). */
  readonly google: Locator;
  readonly divider: Locator;
  readonly status: Locator;

  constructor(private readonly page: Page) {
    this.heading = page.getByRole('heading', { level: 1 });
    this.email = page.getByLabel('Email');
    this.password = page.getByLabel('Password');
    this.submit = page.getByRole('button', { name: 'Sign in' });
    this.alert = page.getByRole('alert');
    this.google = page.getByRole('link', { name: 'Continue with Google' });
    this.divider = page.getByRole('separator', { name: 'or' });
    this.status = page.locator('main').getByRole('status');
  }

  async goto(returnUrl?: string, query: Record<string, string> = {}): Promise<void> {
    const params = new URLSearchParams(query);
    if (returnUrl) params.set('returnUrl', returnUrl);
    const search = params.toString();
    await this.page.goto(search ? `/sign-in?${search}` : '/sign-in');
  }

  async signIn(email: string, password: string): Promise<void> {
    await this.email.fill(email);
    await this.password.fill(password);
    await this.submit.click();
  }
}
