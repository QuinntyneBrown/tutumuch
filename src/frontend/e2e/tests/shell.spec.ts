// Acceptance Test
// Traces to: L2-071, L2-065
// Description: The built app serves the empty guest shell at / with a skip link, main#main,
// guest top bar and footer, without horizontal scroll at 320px and calling TuTuMuch API only for the catalog.

import { expect, serveCatalog, test } from '../fixtures';
import { ShellPage } from '../pages/shell.page';

test.describe('empty guest shell', () => {
  test.use({ viewport: { width: 320, height: 640 } });
  test.beforeEach(async ({ page }) => {
    await serveCatalog(page, 0);
  });

  test('skip link is the first focusable element and targets main', async ({ page, unfixturedApiCalls }) => {
    const shell = new ShellPage(page);
    await shell.goto();

    await page.keyboard.press('Tab');
    await expect(shell.skipLink).toBeFocused();
    await expect(shell.skipLink).toHaveAttribute('href', '#main');
    await expect(shell.main).toBeAttached();
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('guest top bar, footer links and no horizontal scroll at 320px', async ({ page, unfixturedApiCalls }) => {
    const shell = new ShellPage(page);
    await shell.goto();

    await expect(shell.menuButton).toBeVisible();
    await expect(shell.menuButton).toHaveAttribute('aria-expanded', 'false');
    await expect(shell.signIn).toBeVisible();
    for (const name of ['Marketplace rules', 'Privacy', 'Terms']) {
      await expect(shell.footerLink(name)).toBeVisible();
    }
    await expect(shell.footer.getByRole('button', { name: 'Cookie settings' })).toBeVisible();
    await expect(shell.footer).toContainText(`© ${new Date().getFullYear()} TuTuMuch`);
    expect(await shell.hasHorizontalScroll()).toBe(false);
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('the catalog renders inside main', async ({ page }) => {
    const shell = new ShellPage(page);
    await shell.goto();

    await expect(shell.main.getByRole('heading', { level: 1 })).toHaveText('Girls’ clothing. Nothing else.');
  });
});
