import { Locator, Page } from '@playwright/test';

/** The application shell: skip link, top bar, main region and footer. */
export class ShellPage {
  readonly skipLink: Locator;
  readonly main: Locator;
  readonly topBar: Locator;
  readonly signIn: Locator;
  readonly menuButton: Locator;
  readonly footer: Locator;

  constructor(private readonly page: Page) {
    this.skipLink = page.getByRole('link', { name: 'Skip to content' });
    this.main = page.locator('main#main');
    this.topBar = page.getByRole('banner');
    this.signIn = this.topBar.getByRole('link', { name: 'Sign in' });
    this.menuButton = this.topBar.getByRole('button', { name: 'Open menu' });
    this.footer = page.getByRole('contentinfo');
  }

  async goto(path = '/'): Promise<void> {
    await this.page.goto(path);
  }

  footerLink(name: string): Locator {
    return this.footer.getByRole('navigation', { name: 'Footer' }).getByRole('link', { name });
  }

  async hasHorizontalScroll(): Promise<boolean> {
    return this.page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth);
  }
}
