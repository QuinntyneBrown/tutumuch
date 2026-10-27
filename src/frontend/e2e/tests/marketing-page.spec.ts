// Acceptance Test
// Traces to: L2-078, L2-079, L2-080, L2-081, L2-082, L2-083
// Description: The public marketing page at /welcome: sections and CTA targets for visitors and
// members, the footer link, the pitch video (poster first, keyboard play and pause, captions off
// until turned on, error state), reduced motion, heading order, title, meta description and
// link-preview tags.
// The media files come from the static build; the error state fails their route.

import { expect, marcus, serveCatalog, signInAs, test } from '../fixtures';
import { ShellPage } from '../pages/shell.page';
import { WelcomePage } from '../pages/welcome.page';

test.describe('marketing page', () => {
  test('a visitor sees the sections and Start selling goes to /join', async ({ page, unfixturedApiCalls }) => {
    const welcome = new WelcomePage(page);
    await welcome.goto();

    await expect(welcome.heading).toHaveText('Twirl it forward.');
    for (const name of [
      'Everything here is for girls.',
      'Pass it on to the next girl.',
      'Three steps. That’s it.',
      'Her next favourite thing is waiting.',
    ]) {
      await expect(welcome.main.getByRole('heading', { level: 2, name })).toBeVisible();
    }
    await expect(welcome.main.getByRole('heading', { level: 3 })).toHaveText(['List it', 'Sell it', 'Get paid']);
    await expect(welcome.main).not.toContainText(/\bboys?['’]?\b|\bmen['’]?s\b|\badults?\b/i);
    const sell = welcome.sellLinks();
    await expect(sell).not.toHaveCount(0);
    for (const link of await sell.all()) await expect(link).toHaveAttribute('href', '/join');
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('a signed-in member gets Start selling to /sell', async ({ page }) => {
    await signInAs(page, marcus);
    const welcome = new WelcomePage(page);
    await welcome.goto();

    await expect(welcome.heading).toBeVisible();
    await expect(welcome.sellLinks().first()).toHaveAttribute('href', '/sell');
    for (const link of await welcome.sellLinks().all()) await expect(link).toHaveAttribute('href', '/sell');
  });

  test('Shop the catalog goes to the catalog', async ({ page }) => {
    await serveCatalog(page, 0);
    const welcome = new WelcomePage(page);
    await welcome.goto();

    await welcome.link('Shop the catalog').click();
    await expect(page).toHaveURL(/\/$/);
    await expect(page.getByRole('heading', { level: 1 })).toHaveText('Find something she’ll love.');
  });

  test('title, meta description, heading order and no horizontal scroll at 320px', async ({ page }) => {
    await page.setViewportSize({ width: 320, height: 640 });
    const welcome = new WelcomePage(page);
    await welcome.goto();

    await expect(page).toHaveTitle('Twirl it forward · TuTuMuch');
    await expect(page.locator('meta[name="description"]')).toHaveAttribute('content', /clothes and gear for girls/i);
    const levels = await welcome.headingLevels();
    expect(levels[0]).toBe(1);
    expect(levels.filter((level) => level === 1)).toHaveLength(1);
    levels.forEach((level, i) => i > 0 && expect(level - levels[i - 1]).toBeLessThanOrEqual(1));
    expect(await new ShellPage(page).hasHorizontalScroll()).toBe(false);
  });

  test('decorative shapes are hidden and stop moving under reduced motion', async ({ page }) => {
    await page.emulateMedia({ reducedMotion: 'reduce' });
    const welcome = new WelcomePage(page);
    await welcome.goto();

    const shapes = welcome.main.locator('.shapes').first();
    await expect(shapes).toHaveAttribute('aria-hidden', 'true');
    const animations = await welcome.main
      .locator('.shapes__shape')
      .evaluateAll((els) => els.map((el) => getComputedStyle(el).animationName));
    expect(animations.length).toBeGreaterThan(0);
    expect(animations.every((name) => name === 'none')).toBe(true);
  });

  test('the shapes drift when motion is allowed', async ({ page }) => {
    await page.emulateMedia({ reducedMotion: 'no-preference' });
    await new WelcomePage(page).goto();

    const name = await page
      .locator('.shapes__shape')
      .first()
      .evaluate((el) => getComputedStyle(el).animationName);
    expect(name).toBe('shape-drift');
  });

  test('the footer links to the marketing page', async ({ page }) => {
    await serveCatalog(page, 0);
    const shell = new ShellPage(page);
    await shell.goto();

    await shell.footerLink('About TuTuMuch').click();
    await expect(page).toHaveURL(/\/welcome$/);
    await expect(new WelcomePage(page).heading).toBeVisible();
  });

  test('the HTML a link preview fetches carries the share tags and the image exists', async ({ page }) => {
    const html = await (await page.request.get('/welcome')).text();
    const meta = (key: string) => html.match(new RegExp(`<meta (?:property|name)="${key}" content="([^"]*)"`))?.[1];
    expect(meta('og:title')).toBe('TuTuMuch · Twirl it forward');
    expect(meta('og:type')).toBe('website');
    expect(meta('og:image')).toBe('https://www.tutumuch.kids/media/tutumuch-share.jpg');
    expect(meta('og:image:width')).toBe('1200');
    expect(meta('og:image:height')).toBe('630');
    expect(meta('twitter:card')).toBe('summary_large_image');
    expect(meta('twitter:image')).toBe('https://www.tutumuch.kids/media/tutumuch-share.jpg');
    expect(meta('og:description')).toMatch(/for girls/);
    const image = await page.request.get('/media/tutumuch-share.jpg');
    expect(image.status()).toBe(200);
    expect(image.headers()['content-type']).toBe('image/jpeg');
  });
});

test.describe('pitch video', () => {
  test('shows only the poster until the visitor presses it', async ({ page }) => {
    const welcome = new WelcomePage(page);
    await welcome.goto();

    await expect(welcome.poster).toBeVisible();
    await expect(welcome.poster.locator('img')).toHaveAttribute('alt', '');
    await expect(page.locator('video')).toHaveCount(0);
  });

  test('plays from the keyboard, pauses and resumes, and Escape returns focus', async ({ page }) => {
    const welcome = new WelcomePage(page);
    await welcome.goto();

    await welcome.poster.focus();
    await page.keyboard.press('Enter');
    await expect(welcome.dialog).toBeVisible();
    await expect(welcome.playPause).toHaveText('Pause');
    await expect.poll(() => welcome.isPaused(), { timeout: 15_000 }).toBe(false);

    await welcome.playPause.focus();
    await page.keyboard.press('Enter');
    await expect.poll(() => welcome.isPaused(), { timeout: 15_000 }).toBe(true);
    await expect(welcome.playPause).toHaveText('Play');
    await page.keyboard.press('Space');
    await expect.poll(() => welcome.isPaused(), { timeout: 15_000 }).toBe(false);

    await page.keyboard.press('Escape');
    await expect(welcome.dialog).toHaveCount(0);
    await expect(welcome.poster).toBeFocused();
  });

  test('opens with captions off and no visible heading; Captions turns them on', async ({ page }) => {
    const welcome = new WelcomePage(page);
    await welcome.goto();
    await welcome.poster.click();

    await expect(welcome.dialog.getByRole('heading', { name: 'TuTuMuch video' })).toHaveClass(/visually-hidden/);
    await expect(welcome.captions).toHaveAttribute('aria-pressed', 'false');
    await expect.poll(() => welcome.video.evaluate((v: HTMLVideoElement) => v.textTracks[0]?.mode)).toBe('hidden');
    await welcome.captions.click();
    await expect(welcome.captions).toHaveAttribute('aria-pressed', 'true');
    expect(await welcome.video.evaluate((v: HTMLVideoElement) => v.textTracks[0]?.mode)).toBe('showing');
    await expect(welcome.dialog.getByRole('link')).toHaveCount(0);
  });

  test('shows an error with Try again, and the page still works', async ({ page }) => {
    await page.route('**/media/tutumuch-pitch.{mp4,webm}', (route) => route.fulfill({ status: 404, body: '' }));
    await serveCatalog(page, 0);
    const welcome = new WelcomePage(page);
    await welcome.goto();
    await welcome.poster.click();

    await expect(welcome.videoError).toContainText('The video didn’t load');
    await expect(welcome.dialog.getByRole('button', { name: 'Try again' })).toBeVisible();

    await welcome.dialog.getByRole('button', { name: 'Close' }).click();
    await welcome.link('Shop the catalog').click();
    await expect(page).toHaveURL(/\/$/);
  });
});
