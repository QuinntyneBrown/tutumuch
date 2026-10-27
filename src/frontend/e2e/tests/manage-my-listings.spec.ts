// Acceptance Test
// Traces to: L2-019, L2-021
// Description: "Your listings" shows every listing of the seller newest first with cover, title,
// price, status and date, filters by status with a count on each chip, opens each listing where
// its status belongs, and has loading, empty and error states. A seller with drafts is told once
// per visit that drafts older than 90 days are deleted.

import { Page } from '@playwright/test';

import { expect, marcus, myListingRow, myListings, problem, signInAs, test } from '../fixtures';
import { MyListingsPage } from '../pages/my-listings.page';

const all = myListings(
  [
    myListingRow({
      reference: 'L-10030',
      status: 'Draft',
      title: 'Rainbow tulle tutu, pink',
      price: 60,
      createdAt: '2026-10-26T09:00:00Z',
    }),
    myListingRow({
      reference: 'L-10029',
      status: 'Draft',
      title: null,
      category: 'Shoes',
      price: null,
      photo: null,
      createdAt: '2026-10-25T09:00:00Z',
    }),
    myListingRow({
      reference: 'L-10025',
      status: 'Sold',
      title: 'Unicorn hoodie, lilac',
      price: 38,
      createdAt: '2026-10-24T09:00:00Z',
      saleOrderId: 'o-9',
    }),
    myListingRow(),
    myListingRow({
      reference: 'L-10002',
      status: 'Sold',
      title: 'Tech Fleece shorts',
      price: 30,
      createdAt: '2026-10-02T09:00:00Z',
      saleOrderId: 'o-0',
    }),
  ],
  { all: 5, draft: 2, active: 1, sold: 2 },
);

const sold = myListings(
  (all['items'] as Record<string, unknown>[]).filter((row) => row['status'] === 'Sold'),
  all['counts'] as Record<string, number>,
);

/** Answers GET /api/me/listings with every listing, or the sold ones for ?status=Sold; records each status asked for. */
async function serve(page: Page): Promise<string[]> {
  const queries: string[] = [];
  await page.route('**/api/me/listings*', (route) => {
    const status = new URL(route.request().url()).searchParams.get('status');
    queries.push(status ?? 'all');
    return route.fulfill({ json: status === 'Sold' ? sold : all });
  });
  return queries;
}

test.describe('manage my listings', () => {
  test.beforeEach(async ({ page }) => {
    await page.clock.setFixedTime(new Date('2026-10-26T18:00:00Z'));
    await signInAs(page, marcus);
    await page.route('**/photos/**', (route) => route.fulfill({ contentType: 'image/png', body: Buffer.alloc(0) }));
  });

  test('every listing is shown newest first with its status, price and date', async ({ page, unfixturedApiCalls }) => {
    await serve(page);
    const listings = new MyListingsPage(page);
    await listings.goto();

    await expect(listings.heading).toHaveText('Your listings');
    await expect(listings.subtitle).toHaveText('5 listings · 1 active');
    await expect(listings.sellOutgrownGear).toHaveAttribute('href', '/sell');
    await expect(listings.rows).toHaveCount(5);
    await expect(listings.rows.nth(0)).toContainText('Rainbow tulle tutu, pink');
    await expect(listings.rows.nth(0)).toContainText('Draft');
    await expect(listings.rows.nth(0)).toContainText('$60');
    await expect(listings.rows.nth(0)).toContainText('Today');
    await expect(listings.rows.nth(1)).toContainText('Untitled draft · Shoes');
    await expect(listings.rows.nth(1)).toContainText('—');
    await expect(listings.rows.nth(1)).toContainText('Yesterday');
    await expect(listings.rows.nth(2)).toContainText('Sat 24 Oct');
    await expect(listings.rows.nth(3)).toContainText('Active');
    await expect(listings.rows.nth(3)).toContainText('$45');
    await expect(listings.rows.nth(3)).toContainText('Wed 21 Oct');
    await expect(listings.chip('All')).toHaveAttribute('aria-pressed', 'true');
    await expect(listings.chip('Draft')).toHaveText('Draft 2');
    await expect(listings.chip('Reserved')).toHaveText('Reserved 0');
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('each row opens the place that fits its status', async ({ page }) => {
    await serve(page);
    const listings = new MyListingsPage(page);
    await listings.goto();

    await expect(listings.row('Untitled draft').getByRole('link')).toHaveAttribute('href', '/sell/L-10029');
    await expect(listings.row('Unicorn hoodie').getByRole('link')).toHaveAttribute('href', '/sales/o-9');
    await expect(listings.row('Arizona sandals').getByRole('link')).toHaveAttribute('href', '/items/L-10021');
  });

  test('filtering by sold shows only the sold listings and survives a reload', async ({ page }) => {
    const queries = await serve(page);
    const listings = new MyListingsPage(page);
    await listings.goto();
    await expect(listings.rows).toHaveCount(5);

    await listings.chip('Sold').click();

    await expect(page).toHaveURL(/\/account\/listings\?status=sold$/);
    await expect(listings.chip('Sold')).toHaveAttribute('aria-pressed', 'true');
    await expect(listings.chip('All')).toHaveAttribute('aria-pressed', 'false');
    await expect(listings.rows).toHaveCount(2);
    await page.reload();
    await expect(listings.rows).toHaveCount(2);
    await expect(listings.chip('Sold')).toHaveAttribute('aria-pressed', 'true');
    expect(queries).toEqual(['all', 'Sold', 'Sold']);
  });

  test('a seller with drafts is told once that drafts older than 90 days are deleted', async ({ page }) => {
    await serve(page);
    const listings = new MyListingsPage(page);
    await listings.goto();

    await expect(listings.draftToast).toHaveCount(1);
    await expect(listings.draftToast).toHaveClass(/toast--info/);
    await expect(listings.draftToast).toContainText('We’ll email you a week before.');
    await listings.chip('Sold').click();
    await expect(listings.rows).toHaveCount(2);
    await expect(listings.draftToast).toHaveCount(1);
  });

  test('a seller without drafts gets no draft toast', async ({ page }) => {
    await page.route('**/api/me/listings*', (route) =>
      route.fulfill({ json: myListings([myListingRow()], { all: 1, active: 1 }) }),
    );
    const listings = new MyListingsPage(page);
    await listings.goto();

    await expect(listings.rows).toHaveCount(1);
    await expect(listings.draftToast).toHaveCount(0);
  });

  test('a seller with no listings is asked to sell an item', async ({ page }) => {
    await page.route('**/api/me/listings*', (route) => route.fulfill({ json: myListings([]) }));
    const listings = new MyListingsPage(page);
    await listings.goto();

    await expect(listings.empty).toContainText('Nothing listed yet');
    await expect(listings.empty).toContainText('List a dress, a leotard or anything she’s outgrown.');
    await expect(listings.empty.getByRole('link', { name: 'Sell outgrown gear' })).toHaveAttribute('href', '/sell');
    await expect(listings.filter).toHaveCount(0);
  });

  test('a failed load shows the reference and tries again', async ({ page }) => {
    let calls = 0;
    await page.route('**/api/me/listings*', (route) =>
      ++calls === 1 ? route.fulfill(problem(500, 'INTERNAL', 'Something went wrong')) : route.fulfill({ json: all }),
    );
    const listings = new MyListingsPage(page);
    await listings.goto();

    await expect(listings.empty).toContainText('Couldn’t load your listings');
    await expect(listings.empty).toContainText('Try again in a moment.');
    await expect(listings.empty).toContainText('Ref 5c1d-0e7a');
    await listings.tryAgain.click();
    await expect(listings.rows).toHaveCount(5);
  });

  test('loading shows four skeleton rows', async ({ page }) => {
    let release!: () => void;
    const held = new Promise<void>((resolve) => (release = resolve));
    await page.route('**/api/me/listings*', async (route) => {
      await held;
      await route.fulfill({ json: all });
    });
    const listings = new MyListingsPage(page);
    await listings.goto();

    await expect(listings.loading).toHaveAttribute('aria-label', 'Loading listings');
    await expect(listings.loading.locator('li')).toHaveCount(4);
    release();
    await expect(listings.rows).toHaveCount(5);
  });
});
