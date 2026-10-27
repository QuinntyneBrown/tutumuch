// Acceptance Test
// Traces to: L2-096, L2-064, L2-065
// Description: Every routed page follows the checkable rules of docs/page-guidelines.md: exactly
// one h1 and one logo link, icon-only controls with an aria-label, alt text on every image, at
// most one Tilly whose alt text starts with "Tilly", no inline hex colour or gradient, at most one
// primary button outside dialogs, Fraunces headings, DM Sans text and Titan One only in the logo
// and mark. Per-page checks cover the standard terms, the header skeleton and Tilly's poses.

import { Page } from '@playwright/test';

import {
  administrator,
  buyerOrder,
  catalogCard,
  expect,
  listingDetail,
  marcus,
  memberProfile,
  myListingRow,
  myListings,
  orderList,
  orderRow,
  payoutSummary,
  problemDetail,
  sale,
  serveCatalog,
  serveCatalogue,
  serveCheckout,
  serveListing,
  serveProfile,
  signInAs,
  test,
} from '../fixtures';
import { PageGuidelines } from '../pages/page-guidelines.page';

const PIXEL = Buffer.from(
  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=',
  'base64',
);

interface GuidelineCase {
  name: string;
  path: string;
  setup?: (page: Page) => Promise<void>;
  /** The visible h1 text once the page has its data. */
  heading: string | RegExp;
  /** The alt text of the one Tilly on the page, or none. */
  tilly?: RegExp;
}

async function photos(page: Page): Promise<void> {
  await page.route('**/photos/**', (route) => route.fulfill({ contentType: 'image/png', body: PIXEL }));
}

const cases: GuidelineCase[] = [
  { name: 'catalog', path: '/', setup: (p) => serveCatalog(p, 30).then(), heading: 'Find something she’ll love.' },
  {
    name: 'empty catalog',
    path: '/',
    setup: (p) => serveCatalog(p, 0).then(),
    heading: 'Find something she’ll love.',
    tilly: /^Tilly waving hello/,
  },
  {
    name: 'Dance',
    path: '/dance',
    setup: (p) => serveCatalog(p, 30).then(),
    heading: 'Dance',
    tilly: /^Tilly dancing/,
  },
  {
    name: 'Sports',
    path: '/sports',
    setup: (p) => serveCatalog(p, 30).then(),
    heading: 'Sports',
    tilly: /^Tilly kicking a soccer ball/,
  },
  { name: 'Clothing', path: '/clothing', setup: (p) => serveCatalog(p, 30).then(), heading: 'Clothing' },
  {
    name: 'listing detail',
    path: '/items/L-10001',
    setup: (p) => serveListing(p, listingDetail()),
    heading: 'Floral party dress, pink',
  },
  {
    name: 'sold listing',
    path: '/items/L-10001',
    setup: async (p) => {
      await serveCatalog(p, 4);
      await serveListing(p, listingDetail({ status: 'Sold' }));
    },
    heading: 'Floral party dress, pink',
  },
  { name: 'marketing page', path: '/welcome', heading: 'Twirl it forward.', tilly: /^Tilly twirling/ },
  { name: 'marketplace rules', path: '/rules', heading: 'Marketplace rules' },
  { name: 'privacy', path: '/privacy', heading: 'Privacy' },
  { name: 'terms', path: '/terms', heading: 'Terms of service' },
  { name: 'sign in', path: '/sign-in', heading: 'Sign in' },
  { name: 'join', path: '/join', heading: 'Join TuTuMuch' },
  {
    name: 'sell',
    path: '/sell',
    setup: async (p) => {
      await signInAs(p, marcus);
      await serveCatalogue(p);
    },
    heading: 'Sell an item',
  },
  {
    name: 'your listings',
    path: '/account/listings',
    setup: async (p) => {
      await signInAs(p, marcus);
      await photos(p);
      await p.route('**/api/me/listings*', (route) =>
        route.fulfill({ json: myListings([myListingRow()], { all: 1, active: 1 }) }),
      );
    },
    heading: 'Your listings',
  },
  {
    name: 'empty listings',
    path: '/account/listings',
    setup: async (p) => {
      await signInAs(p, marcus);
      await p.route('**/api/me/listings*', (route) => route.fulfill({ json: myListings([]) }));
    },
    heading: 'Your listings',
    tilly: /^Tilly waving hello/,
  },
  {
    name: 'purchases',
    path: '/account/purchases',
    setup: async (p) => {
      await signInAs(p, marcus);
      await photos(p);
      await p.route('**/api/purchases', (route) => route.fulfill({ json: orderList([orderRow()]) }));
    },
    heading: 'Purchases',
  },
  {
    name: 'sales',
    path: '/account/sales',
    setup: async (p) => {
      await signInAs(p, marcus);
      await p.route('**/api/sales', (route) => route.fulfill({ json: orderList([]) }));
    },
    heading: 'Sales',
    tilly: /^Tilly waving hello/,
  },
  {
    name: 'payouts',
    path: '/account/payouts',
    setup: async (p) => {
      await signInAs(p, marcus);
      await photos(p);
      await p.route('**/api/payouts', (route) => route.fulfill({ json: payoutSummary() }));
    },
    heading: 'Payouts',
  },
  {
    name: 'emails',
    path: '/account/emails',
    setup: async (p) => {
      await signInAs(p, marcus);
      await p.route('**/api/me/notification-preferences', (route) =>
        route.fulfill({
          json: {
            categories: [
              { key: 'accountSecurity', enabled: true, locked: true },
              { key: 'orders', enabled: true, locked: true },
              { key: 'reviews', enabled: true, locked: false },
              { key: 'marketing', enabled: false, locked: false },
            ],
          },
          headers: { ETag: '"0"' },
        }),
      );
    },
    heading: 'Emails',
  },
  {
    name: 'member profile',
    path: '/members/tutu_tessa',
    setup: (p) => serveProfile(p, memberProfile()).then(),
    heading: 'tutu_tessa',
  },
  {
    name: 'checkout',
    path: '/checkout/L-10001',
    setup: async (p) => {
      await signInAs(p, marcus);
      await serveCheckout(p);
    },
    heading: 'Checkout',
  },
  {
    name: 'order',
    path: '/orders/o-1',
    setup: async (p) => {
      await signInAs(p, marcus);
      await photos(p);
      await p.route('**/api/purchases/o-1', (route) => route.fulfill({ json: buyerOrder() }));
    },
    heading: 'Waiting to ship',
  },
  {
    name: 'sale',
    path: '/sales/o-1',
    setup: async (p) => {
      await signInAs(p, marcus);
      await photos(p);
      await p.route('**/api/sales/o-1', (route) => route.fulfill({ json: sale() }));
    },
    heading: 'You made a sale',
  },
  {
    name: 'order problem (admin)',
    path: '/admin/problems/P-1001',
    setup: async (p) => {
      await signInAs(p, administrator);
      await photos(p);
      await p.route('**/api/orders/*/problem/photos/*', (route) =>
        route.fulfill({ contentType: 'image/png', body: PIXEL }),
      );
      await p.route('**/api/admin/problems/P-1001', (route) => route.fulfill({ json: problemDetail() }));
    },
    heading: /P-1001|Not as described/,
  },
  {
    name: 'unsubscribe',
    path: '/emails/unsubscribe?token=t-1',
    setup: (p) =>
      p.route('**/api/notifications/unsubscribe', (route) =>
        route.fulfill({ json: { category: 'Marketing', unsubscribed: true } }),
      ),
    heading: /unsubscribed/i,
  },
];

test.describe('page guidelines', () => {
  test.use({ viewport: { width: 1280, height: 900 } });

  for (const c of cases) {
    test(`${c.name} follows the page guidelines`, async ({ page }) => {
      await c.setup?.(page);
      await page.goto(c.path);
      await expect(page.getByRole('heading', { level: 1 })).toHaveText(c.heading);
      await expect(page.locator('[aria-busy="true"]')).toHaveCount(0);
      await page.evaluate(() => document.fonts.ready);

      const report = await new PageGuidelines(page).report();
      expect(report.h1Count, 'one h1').toBe(1);
      expect(report.visibleLogoLinks, 'one logo link').toBe(1);
      expect(report.unlabelledIconControls, 'icon-only controls need an aria-label').toEqual([]);
      expect(report.imagesWithoutAlt, 'every image has alt text').toEqual([]);
      expect(report.inlineHexOrGradient, 'colours come from tokens').toEqual([]);
      expect(report.visiblePrimaryButtons, 'at most one primary button').toBeLessThanOrEqual(1);
      expect(report.headingFont).toContain('Fraunces');
      expect(report.bodyFont).toContain('DM Sans');
      expect(report.titanOneOutsideLogo, 'Titan One only in the logo').toEqual([]);
      expect(report.tillyAlts.length, 'at most one Tilly').toBeLessThanOrEqual(1);
      for (const alt of report.tillyAlts) expect(alt).toMatch(/^Tilly /);
      if (c.tilly) expect(report.tillyAlts).toEqual([expect.stringMatching(c.tilly)]);
      else expect(report.tillyAlts).toEqual([]);
    });
  }

  test('the header follows the page skeleton: wordmark, search, Sell and Account, categories in guideline order', async ({
    page,
  }) => {
    await signInAs(page, marcus);
    await serveCatalog(page, 30);
    await page.goto('/');
    const header = page.getByRole('banner');

    await expect(header.getByRole('link', { name: 'TuTuMuch home' })).toBeVisible();
    await expect(header.getByRole('searchbox', { name: 'Search TuTuMuch' })).toHaveAttribute(
      'placeholder',
      'Search the closet',
    );
    await expect(header.getByRole('link', { name: 'Sell' })).toBeVisible();
    await expect(header.getByRole('link', { name: /Your account/ })).toBeVisible();
    await expect(header.getByRole('navigation', { name: 'Primary' }).getByRole('link')).toHaveText([
      'Dance',
      'Sports',
      'Clothing',
      'Shoes',
      'Cameras and tech',
      'Toys and crafts',
    ]);
    const footer = page.getByRole('contentinfo');
    await expect(footer).toContainText('© TuTuMuch');
    await expect(footer.getByRole('link', { name: 'TuTuMuch home' })).toHaveCount(0);
  });

  test('under 640px the header shows the mark inside the one home link', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 800 });
    await serveCatalog(page, 30);
    await page.goto('/');
    const home = page.getByRole('banner').getByRole('link', { name: 'TuTuMuch home' });

    await expect(home).toBeVisible();
    await expect(home.locator('.logo__mark')).toBeVisible();
    await expect(home.locator('.wordmark')).toBeHidden();
  });

  test('listing cards show title, brand, size and condition, and price, with aligned price rows', async ({ page }) => {
    await page.route('**/api/catalog/listings**', (route) =>
      route.fulfill({
        json: {
          items: [
            catalogCard(1, { title: 'Recital tutu, raspberry, with a very long title that does not fit on one line' }),
            catalogCard(2),
            catalogCard(3),
            catalogCard(4),
          ],
          page: 1,
          pageSize: 24,
          totalCount: 4,
        },
      }),
    );
    await page.route('**/api/catalog/brands', (route) => route.fulfill({ json: [] }));
    await photos(page);
    await page.goto('/');
    const cards = page.locator('tl-listing-card');
    await expect(cards).toHaveCount(4);

    const first = cards.first();
    await expect(first.locator('.listing-card__facts')).toHaveText(/^Girls’? · /);
    const rows = await cards
      .locator('.listing-card__price')
      .evaluateAll((els) => els.map((el) => Math.round(el.getBoundingClientRect().bottom)));
    expect(new Set(rows).size, 'price rows line up').toBe(1);
    const titleHeights = await cards
      .locator('.listing-card__title')
      .evaluateAll((els) => els.map((el) => Math.round(el.getBoundingClientRect().height)));
    expect(new Set(titleHeights).size, 'titles stay on one line').toBe(1);
  });

  test('a sold listing greys its photo, shows Sold and offers Find similar', async ({ page }) => {
    await serveCatalog(page, 4);
    await serveListing(page, listingDetail({ status: 'Sold' }));
    await page.goto('/items/L-10001');

    await expect(page.locator('.photo__stamp--sold')).toHaveText('Sold');
    await expect(page.locator('.gallery .photo--sold').first()).toBeVisible();
    expect(
      await page
        .locator('.gallery .photo--sold img')
        .first()
        .evaluate((img) => getComputedStyle(img).opacity),
    ).toBe('0.6');
    await expect(page.getByRole('link', { name: 'Find similar' })).toBeVisible();
  });

  test('the condition badge colours follow the condition scale', async ({ page }) => {
    await serveListing(page, listingDetail({ condition: 'New with tags' }));
    await page.goto('/items/L-10001');

    await expect(page.locator('tl-condition-badge .condition-badge--new')).toHaveText('New with tags');
  });

  test('loading shows blush skeletons, not a spinner', async ({ page }) => {
    await page.route('**/api/catalog/listings**', () => undefined);
    await page.route('**/api/catalog/brands', (route) => route.fulfill({ json: [] }));
    await page.goto('/');
    const skeleton = page.locator('main .skeleton').first();

    await expect(skeleton).toBeVisible();
    await expect(page.locator('main .spinner')).toHaveCount(0);
    const fill = await skeleton.evaluate((el) => getComputedStyle(el).backgroundColor);
    expect(fill).toBe('rgb(253, 226, 234)');
  });

  test('the catalog filters show Category, Size and Condition first and the rest under More filters', async ({
    page,
  }) => {
    await serveCatalog(page, 30);
    await page.goto('/');
    const filters = page.getByRole('complementary', { name: 'Filters' });

    await expect(filters.locator('legend, .field__label, summary')).toHaveText([
      'Category',
      'Size',
      'Condition',
      'More filters',
      'Brand',
      'Price',
    ]);
    await expect(filters.getByRole('group', { name: 'Brand' })).toBeHidden();
    await filters.getByText('More filters').click();
    await expect(filters.getByRole('group', { name: 'Brand' })).toBeVisible();
  });

  test('reduced motion stops transitions', async ({ page }) => {
    await page.emulateMedia({ reducedMotion: 'reduce' });
    await serveCatalog(page, 30);
    await page.goto('/');
    const duration = await page
      .getByRole('link', { name: 'Sell' })
      .first()
      .evaluate((el) => getComputedStyle(el).transitionDuration);

    expect(duration.split(',').every((d) => parseFloat(d) < 0.02)).toBe(true);
  });
});
