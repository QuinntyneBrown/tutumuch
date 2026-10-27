// Acceptance Test
// Traces to: L2-022, L2-059, L2-061
// Description: Anyone opens / and pages through the newest Active and Reserved listings, 24 at a
// time, in 2, 3 or 4 columns, opens a category from the top bar, and sees the loading, empty,
// error and offline states.

import { catalogCard, catalogPage, expect, problem, serveCatalog, test } from '../fixtures';
import { CatalogPage } from '../pages/catalog.page';

test.describe('browse catalog', () => {
  test('a visitor sees the newest listings as cards and loads the next page', async ({ page, unfixturedApiCalls }) => {
    const requests = await serveCatalog(page, 30);
    const catalog = new CatalogPage(page);
    await catalog.goto();

    await expect(catalog.heading).toHaveText('Find something she’ll love.');
    await expect(catalog.count).toHaveText('30 items');
    await expect(catalog.cards).toHaveCount(24);
    const card = catalog.card('Floral party dress no. 1');
    await expect(card.getByRole('link')).toHaveAttribute('href', '/items/L-10001');
    await expect(card).toContainText('Gymboree');
    await expect(card).toContainText('Used - Excellent');
    await expect(card).toContainText('$85');
    await expect(card.getByLabel('Size Girls')).toBeVisible();
    await expect(catalog.shown).toHaveText('Showing 24 of 30');

    await catalog.loadMore.click();

    await expect(catalog.cards).toHaveCount(30);
    await expect(catalog.loadMore).toBeHidden();
    expect(requests.map((q) => q.get('page') ?? '1')).toEqual(['1', '2']);
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('a reserved card is stamped and prices show cents only when non-zero', async ({ page }) => {
    await page.route('**/api/catalog/listings**', (route) =>
      route.fulfill({
        json: {
          items: [
            catalogCard(1, { status: 'Reserved' }),
            catalogCard(2, { price: '18.50', sizeLabel: 'Girls' }),
            catalogCard(3, { price: '1200.00' }),
          ],
          page: 1,
          pageSize: 24,
          totalCount: 3,
        },
      }),
    );
    const catalog = new CatalogPage(page);
    await catalog.goto();

    await expect(catalog.card('Floral party dress no. 1')).toContainText('Reserved');
    await expect(catalog.card('Glitter sneakers no. 2')).toContainText('$18.50');
    await expect(catalog.card('Glitter sneakers no. 2').getByLabel('Size Girls')).toBeVisible();
    await expect(catalog.card('Pink tulle tutu no. 3')).toContainText('$1,200');
    await expect(catalog.loadMore).toBeHidden();
  });

  test('photos are WebP with a JPEG fallback, sized by srcset, lazy below the first row', async ({ page }) => {
    await serveCatalog(page, 30);
    const catalog = new CatalogPage(page);
    await catalog.goto();

    const first = catalog.cards.first();
    await expect(first.locator('picture source[type="image/webp"]')).toHaveAttribute('srcset', /400\.webp 400w/);
    const img = first.locator('picture img');
    await expect(img).toHaveAttribute('srcset', /800\.jpg 800w/);
    await expect(img).toHaveAttribute('width', '1200');
    await expect(img).toHaveAttribute('height', '1600');
    await expect(img).toHaveAttribute('loading', 'eager');
    await expect(img).toHaveAttribute('alt', 'Photo 1 of Gymboree Floral party dress no. 1');
    await expect(catalog.cards.nth(4).locator('img')).toHaveAttribute('loading', 'lazy');
  });

  for (const { width, columns } of [
    { width: 375, columns: 2 },
    { width: 800, columns: 3 },
    { width: 1280, columns: 4 },
  ]) {
    test(`the grid has ${columns} columns at ${width}px`, async ({ page }) => {
      await page.setViewportSize({ width, height: 900 });
      await serveCatalog(page, 30);
      const catalog = new CatalogPage(page);
      await catalog.goto();

      await expect(catalog.cards).toHaveCount(24);
      expect(await catalog.columns()).toBe(columns);
    });
  }

  test('no horizontal scroll at 320px', async ({ page }) => {
    await page.setViewportSize({ width: 320, height: 640 });
    await serveCatalog(page, 30);
    const catalog = new CatalogPage(page);
    await catalog.goto();

    await expect(catalog.cards).toHaveCount(24);
    expect(await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth)).toBe(false);
  });

  test('a category from the top bar narrows the catalog', async ({ page }) => {
    const requests = await serveCatalog(page, 30);
    const catalog = new CatalogPage(page);
    await catalog.goto();
    await expect(catalog.cards).toHaveCount(24);

    await page.getByRole('navigation', { name: 'Primary' }).getByRole('link', { name: 'Cameras and tech' }).click();

    await expect(page).toHaveURL(/\/cameras-and-tech$/);
    await expect(catalog.heading).toHaveText('Cameras and tech');
    await expect(page.locator('main .page-header').getByLabel('Size Girls')).toBeVisible();
    await expect.poll(() => requests.at(-1)?.get('category')).toBe('cameras-and-tech');
  });

  test('a non-clothing category page lists cards for girls', async ({ page }) => {
    const requests = await serveCatalog(page, 6);
    const catalog = new CatalogPage(page);
    await page.goto('/cameras-and-tech');

    await expect(page).toHaveTitle('Cameras and tech · TuTuMuch');
    await expect(catalog.heading).toHaveText('Cameras and tech');
    await expect(catalog.cards).toHaveCount(6);
    for (const card of await catalog.cards.all()) {
      await expect(card.getByLabel('Size Girls')).toBeVisible();
    }
    expect(requests.at(-1)?.get('category')).toBe('cameras-and-tech');
  });

  test('skeleton cards show while the first page loads', async ({ page }) => {
    let release: () => void = () => undefined;
    const held = new Promise<void>((resolve) => (release = resolve));
    await page.route('**/api/catalog/listings**', async (route) => {
      await held;
      await route.fulfill({ json: catalogPage(3) });
    });
    const catalog = new CatalogPage(page);
    await catalog.goto();

    await expect(catalog.skeletonGrid).toHaveAttribute('aria-busy', 'true');
    await expect(catalog.skeletonGrid.getByRole('listitem')).toHaveCount(8);
    release();
    await expect(catalog.cards).toHaveCount(3);
    await expect(catalog.skeletonGrid).toHaveCount(0);
  });

  test('an empty catalog invites the visitor to sell', async ({ page }) => {
    await serveCatalog(page, 0);
    const catalog = new CatalogPage(page);
    await catalog.goto();

    await expect(catalog.count).toHaveText('0 items');
    await expect(catalog.emptyState.getByRole('heading', { level: 2 })).toHaveText('Nothing listed yet');
    await expect(catalog.emptyState).toContainText('Be the first to list a pair of 12s or something in L.');
    await expect(catalog.emptyState.getByRole('link', { name: 'Sell an item' })).toHaveAttribute('href', '/sell');
  });

  test('a server error shows the reference and Try again repeats the request', async ({ page }) => {
    let fail = true;
    await page.route('**/api/catalog/listings**', (route) =>
      fail
        ? route.fulfill(problem(500, 'INTERNAL_ERROR', 'Something went wrong'))
        : route.fulfill({ json: catalogPage(3) }),
    );
    const catalog = new CatalogPage(page);
    await catalog.goto();

    await expect(catalog.emptyState.getByRole('heading', { level: 2 })).toHaveText('Couldn’t load items');
    await expect(catalog.emptyState).toContainText('Something went wrong on our side. Ref 5c1d-0e7a');
    fail = false;
    await catalog.emptyState.getByRole('button', { name: 'Try again' }).click();
    await expect(catalog.cards).toHaveCount(3);
  });

  test('a failed next page keeps the loaded cards and offers Try again', async ({ page }) => {
    let failNext = true;
    await page.route('**/api/catalog/listings**', (route) => {
      const pageNumber = Number(new URL(route.request().url()).searchParams.get('page') ?? 1);
      if (pageNumber === 2 && failNext) return route.fulfill(problem(500, 'INTERNAL_ERROR', 'Something went wrong'));
      return route.fulfill({ json: catalogPage(30, pageNumber) });
    });
    const catalog = new CatalogPage(page);
    await catalog.goto();
    await expect(catalog.cards).toHaveCount(24);

    await catalog.loadMore.click();

    await expect(catalog.loadMoreError).toContainText('Couldn’t load more items');
    await expect(catalog.loadMoreError).toContainText('Ref 5c1d-0e7a');
    await expect(catalog.cards).toHaveCount(24);
    failNext = false;
    await catalog.loadMoreError.getByRole('button', { name: 'Try again' }).click();
    await expect(catalog.cards).toHaveCount(30);
    await expect(catalog.loadMoreError).toHaveCount(0);
  });

  test('offline, the catalog shows the items saved on the last visit', async ({ page }) => {
    await serveCatalog(page, 8);
    const catalog = new CatalogPage(page);
    await catalog.goto();
    await expect(catalog.cards).toHaveCount(8);

    await page.unroute('**/api/catalog/listings**');
    await page.route('**/api/catalog/listings**', (route) => route.abort('internetdisconnected'));
    await page.addInitScript(() => Object.defineProperty(navigator, 'onLine', { get: () => false }));
    await page.reload();

    await expect(catalog.offlineBanner).toBeVisible();
    await expect(catalog.count).toHaveText('8 saved items');
    await expect(catalog.cards).toHaveCount(8);
    await expect(catalog.loadMore).toBeHidden();
  });
});
