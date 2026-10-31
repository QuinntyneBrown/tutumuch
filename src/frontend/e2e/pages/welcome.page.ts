import { Locator, Page } from '@playwright/test';

/** The marketing page at /welcome and its pitch video dialog. */
export class WelcomePage {
  readonly main: Locator;
  readonly heading: Locator;
  readonly poster: Locator;
  readonly dialog: Locator;
  readonly video: Locator;
  readonly playPause: Locator;
  readonly captions: Locator;
  readonly videoError: Locator;

  constructor(private readonly page: Page) {
    this.main = page.locator('main#main');
    this.heading = this.main.getByRole('heading', { level: 1 });
    this.poster = this.main.getByRole('button', { name: 'Play' });
    this.dialog = page.getByRole('dialog', { name: 'TuTuMuch video' });
    this.video = this.dialog.locator('video');
    this.playPause = this.dialog.getByRole('button', { name: /^(Play|Pause)$/ });
    this.captions = this.dialog.getByRole('button', { name: 'Captions' });
    this.videoError = this.dialog.getByRole('alert');
  }

  async goto(): Promise<void> {
    await this.page.goto('/welcome');
  }

  /** Every link named "Start selling" on the page. */
  sellLinks(): Locator {
    return this.main.getByRole('link', { name: 'Start selling' });
  }

  /** A section of the page by its heading. */
  region(name: string): Locator {
    return this.main.getByRole('region', { name });
  }

  link(name: string): Locator {
    return this.main.getByRole('link', { name }).first();
  }

  async isPaused(): Promise<boolean> {
    return this.video.evaluate((video: HTMLVideoElement) => video.paused);
  }

  async headingLevels(): Promise<number[]> {
    return this.main
      .locator('h1, h2, h3, h4')
      .evaluateAll((headings) => headings.map((heading) => Number(heading.tagName.slice(1))));
  }
}
