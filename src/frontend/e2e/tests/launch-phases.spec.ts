// Acceptance Test
// Traces to: L2-090, L2-093, L2-094, L2-092
// Description: The app reads the launch phase before its first render. Phase 1 has no category
// links, menu button, Category filter or category pages; in Phases 1 and 2 only an administrator
// is offered selling (top bar, sell routes, welcome page, payout banner); Phase 1 at the product
// limit shows "The catalog is full for now". A failed launch call behaves as Phase 1.

import {
  administrator,
  expect,
  marcus,
  payoutSummary,
  problem,
  serveCatalog,
  serveCatalogue,
  serveLaunchPhase,
  servePhotoUploads,
  signInAs,
  test,
} from '../fixtures';
import { ShellPage } from '../pages/shell.page';

const CATEGORIES = ['Clothing', 'Shoes', 'Dance', 'Sports', 'Cameras and tech', 'Toys and crafts'];

test.describe('launch phase 1', () => {
  test.beforeEach(async ({ page }) => {
    await serveLaunchPhase(page, 1);
    await serveCatalog(page, 30);
  });

  test.describe('on a desktop', () => {
    test.use({ viewport: { width: 1400, height: 900 } });

    test('a visitor sees one flat catalog without category links, Category filter or Sell', async ({ page }) => {
      const shell = new ShellPage(page);
      await shell.goto();

      await expect(page.locator('tl-listing-card').first()).toBeVisible();
      await expect(page.getByRole('navigation', { name: 'Primary' })).toHaveCount(0);
      for (const name of CATEGORIES) {
        await expect(shell.topBar.getByRole('link', { name, exact: true })).toHaveCount(0);
      }
      await expect(shell.topBar.getByRole('link', { name: 'Sell' })).toHaveCount(0);
      await expect(shell.signIn).toBeVisible();
      const filters = page.getByRole('complementary', { name: 'Filters' });
      await expect(filters.getByRole('group', { name: 'Condition' })).toBeVisible();
      await expect(filters.getByRole('group', { name: 'Category' })).toHaveCount(0);
    });

    test('the administrator is offered Sell', async ({ page }) => {
      await signInAs(page, administrator);
      const shell = new ShellPage(page);
      await shell.goto();

      await expect(shell.topBar.getByRole('link', { name: 'Sell' })).toBeVisible();
      await expect(page.getByRole('navigation', { name: 'Primary' })).toHaveCount(0);
    });
  });

  test('at 320px there is no menu button', async ({ page }) => {
    await page.setViewportSize({ width: 320, height: 640 });
    const shell = new ShellPage(page);
    await shell.goto();

    await expect(shell.signIn).toBeVisible();
    await expect(shell.menuButton).toHaveCount(0);
  });

  test('a category URL redirects to the catalog', async ({ page }) => {
    await page.goto('/dance');

    await expect(page).toHaveURL('/');
    await expect(page.locator('tl-listing-card').first()).toBeVisible();
    await expect(page).toHaveTitle('TuTuMuch');
  });

  test('a member who is not an administrator sees "Selling opens soon" on every seller route', async ({ page }) => {
    await signInAs(page, marcus);
    for (const path of ['/sell', '/sell/L-10001', '/items/L-10001/edit', '/account/listings']) {
      await page.goto(path);

      await expect(page.getByRole('heading', { level: 1 })).toHaveText('Selling opens soon');
      await expect(page.getByRole('main').getByRole('link', { name: 'Shop the catalog' })).toHaveAttribute('href', '/');
      await expect(page.getByLabel('Title')).toHaveCount(0);
    }
    await expect(page).toHaveTitle('Selling opens soon · TuTuMuch');
  });

  test('a visitor at /sell is not sent to sign in', async ({ page }) => {
    await page.goto('/sell');

    await expect(page).toHaveURL('/sell');
    await expect(page.getByRole('heading', { level: 1 })).toHaveText('Selling opens soon');
  });

  test('the administrator sells with the usual form until the catalog is full', async ({ page }) => {
    await signInAs(page, administrator);
    await serveCatalogue(page);
    await servePhotoUploads(page);
    await page.route('**/api/listings', (route) =>
      route.fulfill(problem(409, 'PRODUCT_LIMIT_REACHED', 'The catalog is full for now')),
    );
    await page.goto('/sell');
    await expect(page.getByRole('heading', { level: 1 })).toHaveText('Sell an item');

    await page.getByLabel('Category').selectOption('Dance');
    await page.getByRole('button', { name: 'Save draft' }).click();

    await expect(page.getByRole('heading', { level: 1 })).toHaveText('The catalog is full for now');
    await expect(page.getByRole('main').getByRole('link', { name: 'Your listings' })).toHaveAttribute(
      'href',
      '/account/listings',
    );
  });

  test('the welcome page says selling opens soon and offers no Start selling', async ({ page }) => {
    await page.goto('/welcome');

    await expect(page.getByRole('heading', { level: 1 })).toContainText('Twirl it');
    await expect(page.getByRole('link', { name: 'Start selling' })).toHaveCount(0);
    await expect(page.getByRole('heading', { level: 2, name: 'Selling opens soon.' })).toBeVisible();
    await expect(page.getByText('Selling opens soon.', { exact: true }).first()).toBeVisible();
    await expect(page.getByText('For now, every item is hand-picked by TuTuMuch.')).toBeVisible();
    await expect(page.getByRole('link', { name: 'Shop the catalog' }).first()).toBeVisible();
  });

  test('the administrator still sees Start selling on the welcome page', async ({ page }) => {
    await signInAs(page, administrator);
    await page.goto('/welcome');

    await expect(page.getByRole('link', { name: 'Start selling' }).first()).toHaveAttribute('href', '/sell');
  });
});

test.describe('launch phase 2', () => {
  test.use({ viewport: { width: 1400, height: 900 } });
  test.beforeEach(async ({ page }) => {
    await serveLaunchPhase(page, 2);
    await serveCatalog(page, 30);
  });

  test('every category link shows, but no Sell for a member', async ({ page }) => {
    await signInAs(page, marcus);
    const shell = new ShellPage(page);
    await shell.goto();

    await expect(page.getByRole('navigation', { name: 'Primary' }).getByRole('link')).toHaveText(CATEGORIES);
    await expect(shell.topBar.getByRole('link', { name: 'Sell' })).toHaveCount(0);
    await expect(
      page.getByRole('complementary', { name: 'Filters' }).getByRole('group', { name: 'Category' }),
    ).toBeVisible();
  });

  test('category pages are offered', async ({ page }) => {
    await page.goto('/dance');

    await expect(page).toHaveURL('/dance');
    await expect(page.getByRole('heading', { level: 1 })).toHaveText('Dance');
  });

  test('a member is not shown the payout banner or offered selling', async ({ page }) => {
    await signInAs(page, marcus);
    await page.route('**/api/payouts', (route) =>
      route.fulfill({ json: payoutSummary({ banner: 'warning', onboarding: 'NotStarted', pending: '76.50' }) }),
    );
    await page.goto('/sell');

    await expect(page.getByRole('heading', { level: 1 })).toHaveText('Selling opens soon');
    await expect(page.getByText('Set up payouts to get paid')).toHaveCount(0);
  });

  test('the administrator opens the sell form', async ({ page }) => {
    await signInAs(page, administrator);
    await serveCatalogue(page);
    await page.goto('/sell');

    await expect(page.getByRole('heading', { level: 1 })).toHaveText('Sell an item');
  });
});

test.describe('launch phase 3', () => {
  test.use({ viewport: { width: 1400, height: 900 } });

  test('every member is offered Sell', async ({ page }) => {
    await serveCatalog(page, 30);
    await signInAs(page, marcus);
    const shell = new ShellPage(page);
    await shell.goto();

    await expect(shell.topBar.getByRole('link', { name: 'Sell' })).toBeVisible();
    await expect(page.getByRole('navigation', { name: 'Primary' }).getByRole('link')).toHaveCount(6);
  });
});

test.describe('when the launch call fails', () => {
  test.use({ viewport: { width: 1400, height: 900 } });

  test('the app behaves as phase 1', async ({ page }) => {
    await page.route('**/api/launch', (route) => route.fulfill(problem(500, 'INTERNAL_ERROR', 'Something went wrong')));
    await serveCatalog(page, 30);
    await signInAs(page, marcus);
    const shell = new ShellPage(page);
    await shell.goto();

    await expect(page.locator('tl-listing-card').first()).toBeVisible();
    await expect(page.getByRole('navigation', { name: 'Primary' })).toHaveCount(0);
    await expect(shell.topBar.getByRole('link', { name: 'Sell' })).toHaveCount(0);
  });
});
