// Acceptance Test
// Traces to: L2-112, L2-083, L2-096
// Description: The Phase 1 marketing page at /welcome: the hand-picked closet for visitors and
// members without the Administrator role. Its sections and Join early, the closet marquee from
// the catalog (with the loop copies hidden from assistive technology) and without it when the
// call fails, a top bar with no search and no Sign in, the administrator's regular page, reduced
// motion and one primary button.

import { administrator, expect, marcus, problem, serveCatalog, serveLaunchPhase, signInAs, test } from '../fixtures';
import { ShellPage } from '../pages/shell.page';
import { WelcomePage } from '../pages/welcome.page';

test.describe('phase 1 marketing page', () => {
  test.beforeEach(async ({ page }) => {
    await serveLaunchPhase(page, 1);
  });

  test('a visitor sees the hand-picked closet and no Start selling', async ({ page }) => {
    await serveCatalog(page, 12);
    const welcome = new WelcomePage(page);
    await welcome.goto();

    await expect(welcome.heading).toHaveText('Twirl it forward.');
    for (const name of [
      'Hand-picked this week',
      'Hand-picked, every one for girls.',
      'Three steps to her next favourite.',
      'Her next favourite thing is waiting.',
    ]) {
      await expect(welcome.main.getByRole('heading', { level: 2, name })).toBeVisible();
    }
    await expect(welcome.main.getByRole('heading', { level: 3 })).toHaveText([
      'Find it',
      'Pay at checkout',
      'It ships to you',
    ]);
    await expect(welcome.sellLinks()).toHaveCount(0);
    await expect(welcome.main).not.toContainText(/\bboys?['’]?\b|\bmen['’]?s\b|\badults?\b/i);
  });

  test('Join early goes to /join', async ({ page }) => {
    await serveCatalog(page, 12);
    const welcome = new WelcomePage(page);
    await welcome.goto();

    const joins = welcome.main.getByRole('link', { name: 'Join early' });
    await expect(joins).toHaveCount(2);
    for (const link of await joins.all()) await expect(link).toHaveAttribute('href', '/join');
  });

  test('the marquee shows the newest listings and hides its loop copies', async ({ page }) => {
    const requests = await serveCatalog(page, 12);
    const welcome = new WelcomePage(page);
    await welcome.goto();

    const marquee = welcome.region('Hand-picked this week');
    await expect(marquee.getByRole('link', { name: /Floral party dress no\. 1\b/ })).toHaveAttribute(
      'href',
      '/items/L-10001',
    );
    // Twelve tiles reachable by keyboard and assistive technology; twelve aria-hidden copies.
    await expect(marquee.getByRole('link', { name: /no\. \d+/ })).toHaveCount(12);
    const copies = marquee.locator('a[aria-hidden="true"]');
    await expect(copies).toHaveCount(12);
    for (const copy of await copies.all()) await expect(copy).toHaveAttribute('tabindex', '-1');
    await expect(marquee.getByRole('link', { name: 'See everything' })).toHaveAttribute('href', '/');
    expect(requests).toHaveLength(1);
  });

  test('a failing catalog call leaves the marquee out and the rest of the page in', async ({ page }) => {
    await page.route('**/api/catalog/listings**', (route) =>
      route.fulfill(problem(500, 'INTERNAL', 'Something went wrong')),
    );
    const welcome = new WelcomePage(page);
    await welcome.goto();

    await expect(
      welcome.main.getByRole('heading', { level: 2, name: 'Three steps to her next favourite.' }),
    ).toBeVisible();
    await expect(welcome.main.getByRole('heading', { level: 2, name: 'Hand-picked this week' })).toHaveCount(0);
  });

  test('the top bar has no search and no Sign in', async ({ page }) => {
    await serveCatalog(page, 12);
    const welcome = new WelcomePage(page);
    const shell = new ShellPage(page);
    await welcome.goto();

    await expect(welcome.heading).toHaveText('Twirl it forward.');
    await expect(shell.search).toHaveCount(0);
    await expect(shell.searchButton).toHaveCount(0);
    await expect(shell.signIn).toHaveCount(0);
  });

  test('the closet-worth calculator is gone', async ({ page }) => {
    await serveCatalog(page, 12);
    const welcome = new WelcomePage(page);
    await welcome.goto();

    await expect(welcome.heading).toHaveText('Twirl it forward.');
    await expect(welcome.main.getByRole('heading', { name: /closet worth/ })).toHaveCount(0);
    await expect(welcome.main.getByRole('link', { name: 'Join and sell first' })).toHaveCount(0);
  });

  test('a signed-in member is not asked to join', async ({ page }) => {
    await serveCatalog(page, 12);
    await signInAs(page, marcus);
    const welcome = new WelcomePage(page);
    await welcome.goto();

    await expect(welcome.main.getByRole('heading', { level: 2, name: 'Hand-picked this week' })).toBeVisible();
    await expect(welcome.main.getByRole('link', { name: /^Join/ })).toHaveCount(0);
  });

  test('the administrator gets the regular page with Start selling', async ({ page }) => {
    await signInAs(page, administrator);
    const welcome = new WelcomePage(page);
    await welcome.goto();

    await expect(welcome.main.getByRole('heading', { level: 2, name: 'Pass it on to the next girl.' })).toBeVisible();
    await expect(welcome.sellLinks().first()).toHaveAttribute('href', '/sell');
    await expect(new ShellPage(page).search).toHaveCount(1);
    await expect(welcome.main.getByRole('heading', { level: 2, name: 'Hand-picked this week' })).toHaveCount(0);
  });

  test('under reduced motion the marquee stops and every journey step is lit', async ({ page }) => {
    await page.emulateMedia({ reducedMotion: 'reduce' });
    await serveCatalog(page, 12);
    const welcome = new WelcomePage(page);
    await welcome.goto();

    await expect(welcome.region('Hand-picked this week')).toBeVisible();
    const animations = await welcome.main
      .locator('.closet-marquee__track')
      .evaluateAll((tracks) => tracks.map((track) => getComputedStyle(track).animationName));
    expect(animations.length).toBe(2);
    expect(animations.every((name) => name === 'none')).toBe(true);
    await expect(welcome.main.locator('.journey')).not.toHaveAttribute('data-journey-live');
  });

  test('the tulle field is the one decorative layer and it is hidden from assistive technology', async ({ page }) => {
    await serveCatalog(page, 12);
    const welcome = new WelcomePage(page);
    await welcome.goto();

    const canvas = welcome.main.locator('canvas.tulle-field');
    await expect(canvas).toHaveCount(1);
    await expect(canvas).toHaveAttribute('aria-hidden', 'true');
    await expect(welcome.main.locator('.shapes, .shapes__shape')).toHaveCount(0);
  });

  test('the page has one primary button and one h1, with headings in order', async ({ page }) => {
    await serveCatalog(page, 12);
    const welcome = new WelcomePage(page);
    await welcome.goto();

    await expect(welcome.main.locator('.btn--primary')).toHaveCount(1);
    const levels = await welcome.headingLevels();
    expect(levels.filter((level) => level === 1)).toHaveLength(1);
    levels.forEach((level, i) => i > 0 && expect(level - levels[i - 1]).toBeLessThanOrEqual(1));
  });
});
