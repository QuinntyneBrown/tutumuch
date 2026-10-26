// Acceptance Test
// Traces to: L2-037
// Description: "Purchases" and "Sales" list the member's orders newest first with the item, the
// other party, the total, the status and the date, and link to each order. Each page has loading,
// empty and error states, and the seller's sale hides the buyer's address 30 days after the order
// ended.

import { Page } from '@playwright/test';

import { expect, marcus, orderList, orderRow, problem, sale, signInAs, test } from '../fixtures';
import { OrderHistoryPage } from '../pages/order-history.page';
import { SaleDetailPage } from '../pages/sale-detail.page';

function item(title: string): Record<string, unknown> {
  return { ...(sale()['item'] as object), title };
}

const purchases = orderList(
  [
    orderRow({
      id: 'o-3',
      status: 'Shipped',
      otherParty: 'vintage_lou',
      total: '244.97',
      paidAt: '2026-10-22T15:00:00Z',
      item: item('Iron Ranger 8111 boots'),
    }),
    orderRow({
      id: 'o-2',
      status: 'Delivered',
      otherParty: 'vintage_lou',
      total: '59.88',
      paidAt: '2026-10-19T15:00:00Z',
      item: item('Better Sweater fleece'),
    }),
    orderRow({
      id: 'o-1',
      status: 'Completed',
      otherParty: 'jaybird',
      total: '84.92',
      paidAt: '2026-10-13T15:00:00Z',
      item: item('Samba OG'),
    }),
  ],
  { onTheWay: 1, toConfirm: 1 },
);

const sales = orderList(
  [
    orderRow({ shipBy: '2026-10-30T23:59:59Z' }),
    orderRow({
      id: 'o-0',
      status: 'Completed',
      total: '39.59',
      paidAt: '2026-10-02T15:00:00Z',
      item: item('Tech Fleece shorts'),
    }),
  ],
  { toShip: 1 },
);

async function serve(page: Page, list: 'purchases' | 'sales', body: Record<string, unknown>): Promise<void> {
  await page.route(`**/api/${list}`, (route) => route.fulfill({ json: body }));
}

test.describe('order history', () => {
  test.beforeEach(async ({ page }) => {
    await signInAs(page, marcus);
    await page.route('**/photos/**', (route) => route.fulfill({ contentType: 'image/png', body: Buffer.alloc(0) }));
  });

  test('purchases are listed newest first and open the order', async ({ page, unfixturedApiCalls }) => {
    await serve(page, 'purchases', purchases);
    const history = new OrderHistoryPage(page, 'purchases');
    await history.goto();

    await expect(history.heading).toHaveText('Purchases');
    await expect(history.subtitle).toHaveText('1 on the way · 1 to confirm');
    await expect(history.rows).toHaveCount(3);
    await expect(history.rows.nth(0)).toContainText('Iron Ranger 8111 boots');
    await expect(history.rows.nth(0)).toContainText('from vintage_lou · Thu 22 Oct');
    await expect(history.rows.nth(0)).toContainText('$244.97');
    await expect(history.rows.nth(0)).toContainText('Shipped');
    await expect(history.rows.nth(2)).toContainText('from jaybird · Tue 13 Oct');
    await expect(history.rows.nth(2)).toContainText('Completed');
    await expect(history.row('Samba OG').getByRole('link')).toHaveAttribute('href', '/orders/o-1');
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('sales show the buyer and the ship-by date and open the sale', async ({ page, unfixturedApiCalls }) => {
    await serve(page, 'sales', sales);
    const history = new OrderHistoryPage(page, 'sales');
    await history.goto();

    await expect(history.heading).toHaveText('Sales');
    await expect(history.subtitle).toHaveText('1 to ship');
    await expect(history.rows.nth(0)).toContainText('to tall_tom · ship by Fri 30 Oct · Sat 24 Oct');
    await expect(history.rows.nth(0)).toContainText('$48.15');
    await expect(history.rows.nth(0)).toContainText('Paid');
    await expect(history.rows.nth(1)).toContainText('to tall_tom · Fri 2 Oct');
    await expect(history.row('Tech Fleece shorts').getByRole('link')).toHaveAttribute('href', '/sales/o-0');
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('a member with no purchases is sent to the catalog', async ({ page }) => {
    await serve(page, 'purchases', orderList([]));
    const history = new OrderHistoryPage(page, 'purchases');
    await history.goto();

    await expect(history.empty).toContainText('Nothing bought yet');
    await expect(history.empty).toContainText('Everything in the catalog already fits.');
    await expect(history.empty.getByRole('link', { name: 'Browse the catalog' })).toHaveAttribute('href', '/');
    await expect(history.subtitle).toHaveCount(0);
  });

  test('a member with no sales is asked to sell an item', async ({ page }) => {
    await serve(page, 'sales', orderList([]));
    const history = new OrderHistoryPage(page, 'sales');
    await history.goto();

    await expect(history.empty).toContainText('No sales yet');
    await expect(history.empty).toContainText('Sold items show up here with what to do next.');
    await expect(history.empty.getByRole('link', { name: 'Sell an item' })).toHaveAttribute('href', '/sell');
  });

  test('a failed load shows the reference and tries again', async ({ page }) => {
    let calls = 0;
    await page.route('**/api/purchases', (route) =>
      ++calls === 1
        ? route.fulfill(problem(500, 'INTERNAL', 'Something went wrong'))
        : route.fulfill({ json: purchases }),
    );
    const history = new OrderHistoryPage(page, 'purchases');
    await history.goto();

    await expect(history.empty).toContainText('Couldn’t load purchases');
    await expect(history.empty).toContainText('Ref 5c1d-0e7a');
    await history.tryAgain.click();
    await expect(history.rows).toHaveCount(3);
  });

  test('loading shows three skeleton rows', async ({ page }) => {
    let release!: () => void;
    const held = new Promise<void>((resolve) => (release = resolve));
    await page.route('**/api/sales', async (route) => {
      await held;
      await route.fulfill({ json: sales });
    });
    const history = new OrderHistoryPage(page, 'sales');
    await history.goto();

    await expect(history.loading.locator('li')).toHaveCount(3);
    release();
    await expect(history.rows).toHaveCount(2);
  });

  test('the sale hides the buyer’s address 30 days after the order ended', async ({ page }) => {
    await page.route('**/api/sales/o-1', (route) =>
      route.fulfill({ json: sale({ status: 'Completed', shipTo: null }) }),
    );
    const detail = new SaleDetailPage(page);
    await detail.goto('o-1');

    await expect(page.locator('main')).toContainText('Removed 30 days after the order ended.');
    await expect(page.locator('main address')).toHaveCount(0);
  });
});
