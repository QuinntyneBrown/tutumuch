// Acceptance Test
// Traces to: L2-065, L2-054, L2-056, L2-011
// Description: Every footer link opens its page in the full application: Marketplace rules,
// Privacy, Terms and About TuTuMuch, and "Cookie settings" opens the dialog. The rules, privacy and
// terms pages are static and call no TuTuMuch API.

import { expect, serveCatalog, test } from '../fixtures';
import { ShellPage } from '../pages/shell.page';

test.describe('footer pages', () => {
  test.beforeEach(async ({ page }) => {
    await serveCatalog(page, 0);
  });

  for (const { link, path, heading } of [
    { link: 'Marketplace rules', path: '/rules', heading: 'Marketplace rules' },
    { link: 'Privacy', path: '/privacy', heading: 'Privacy' },
    { link: 'Terms', path: '/terms', heading: 'Terms of service' },
    { link: 'About TuTuMuch', path: '/welcome', heading: 'Twirl it forward.' },
  ]) {
    test(`${link} opens ${path}`, async ({ page }) => {
      const shell = new ShellPage(page);
      await shell.goto();

      await shell.footerLink(link).click();
      await expect(page).toHaveURL(path);
      await expect(page.getByRole('heading', { level: 1 })).toHaveText(heading);
    });
  }

  test('the rules page lists every category as girls’ and the rules', async ({ page, unfixturedApiCalls }) => {
    await page.goto('/rules');

    const table = page.getByRole('table', { name: 'Accepted categories and sizes' });
    await expect(table.getByRole('rowheader')).toHaveText([
      'Clothing',
      'Shoes',
      'Dance',
      'Sports',
      'Cameras and tech',
      'Toys and crafts',
    ]);
    await expect(page.getByRole('main')).toContainText('No counterfeits');
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('the terms page links to the rules and privacy', async ({ page, unfixturedApiCalls }) => {
    await page.goto('/terms');

    await expect(page.getByRole('main').locator('ol > li')).toHaveCount(7);
    await page.getByRole('main').getByRole('link', { name: 'marketplace rules' }).click();
    await expect(page).toHaveURL('/rules');
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('Cookie settings opens the dialog from the footer and the privacy page', async ({
    page,
    unfixturedApiCalls,
  }) => {
    const shell = new ShellPage(page);
    await shell.goto('/privacy');

    await page.getByRole('main').getByRole('button', { name: 'Cookie settings' }).click();
    const dialog = page.getByRole('dialog', { name: 'Cookie settings' });
    await expect(dialog).toBeVisible();
    await dialog.getByRole('button', { name: 'Cancel' }).click();
    await expect(dialog).toBeHidden();

    await shell.footer.getByRole('button', { name: 'Cookie settings' }).click();
    await expect(dialog).toBeVisible();
    expect(unfixturedApiCalls).toEqual([]);
  });
});
