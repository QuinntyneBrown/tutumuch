// Acceptance Test
// Traces to: L2-032, L2-033
// Description: The seller of a Paid order sees the ship-by date and the buyer's address, adds a
// carrier and tracking number in the Add tracking dialog, and the sale becomes Shipped with the
// timeline and the tracking link. A malformed number is refused inline, the dialog shows busy and
// failed states, and the page shows loading and error states. The buyer's order page shows the
// order waiting to ship, on its way with tracking, delivered, or cancelled and refunded.

import { Route } from '@playwright/test';

import { buyerOrder, expect, marcus, problem, sale, signInAs, test, timeline, uspsTracking } from '../fixtures';
import { OrderDetailPage } from '../pages/order-detail.page';
import { SaleDetailPage } from '../pages/sale-detail.page';

const shippedSale = sale({ status: 'Shipped', timeline: timeline('2026-10-25T10:00:00Z'), tracking: uspsTracking });

async function serveSale(page: import('@playwright/test').Page, body = sale()): Promise<void> {
  await page.route('**/api/sales/o-1', (route) => route.fulfill({ json: body }));
  await page.route('**/photos/**', (route) => route.fulfill({ contentType: 'image/png', body: Buffer.alloc(0) }));
}

test.describe('ship order: seller', () => {
  test.beforeEach(async ({ page }) => {
    await signInAs(page, marcus);
  });

  test('the seller sees a paid sale with the ship-by date and where to ship it', async ({
    page,
    unfixturedApiCalls,
  }) => {
    await serveSale(page);
    const salePage = new SaleDetailPage(page);
    await salePage.goto();

    await expect(salePage.heading).toHaveText('You made a sale');
    await expect(page).toHaveTitle('Sale · TuTuMuch');
    await expect(salePage.eyebrow).toHaveText('Order TM-48302 · Sat 24 Oct');
    await expect(salePage.shipBy).toHaveText('Ship by Fri 30 Oct.');
    await expect(salePage.shipTo).toHaveText('Tom Reyes1450 Larimer St, Apt 6BDenver, CO 80202');
    await expect(page.getByText('Visible until 30 days after the order ends.')).toBeVisible();
    await expect(salePage.summary).toContainText('Unicorn hoodie, lilac');
    await expect(salePage.summary.locator('dl')).toContainText('Total$48.15');
    await expect(salePage.summary).toContainText('Buyertwirly_tara');
    await expect(salePage.timeline).toHaveCount(0);
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('adding tracking marks the order shipped and shows the tracking link', async ({ page, unfixturedApiCalls }) => {
    await serveSale(page);
    const sent: unknown[] = [];
    await page.route('**/api/orders/o-1/shipment', (route) => {
      sent.push(route.request().postDataJSON());
      return route.fulfill({ json: shippedSale });
    });
    const salePage = new SaleDetailPage(page);
    await salePage.goto();

    await salePage.addTracking.click();

    await expect(salePage.dialog).toBeVisible();
    await expect(salePage.dialog).toContainText('We’ll email twirly_tara the tracking link.');
    await expect(salePage.carrier).toBeFocused();
    await expect(salePage.carrier.locator('option')).toHaveText(['USPS', 'UPS', 'FedEx', 'DHL']);
    await salePage.trackingNumber.fill('9400 1118 9922 3456 1204 77');
    await expect(salePage.markShipped).toHaveText('Mark as shipped');
    await salePage.markShipped.click();

    await expect(salePage.dialog).toBeHidden();
    expect(sent).toEqual([{ carrier: 'USPS', trackingNumber: '9400 1118 9922 3456 1204 77' }]);
    await expect(salePage.heading).toHaveText('Shipped');
    await expect(salePage.currentStep).toHaveText('ShippedSun 25 Oct');
    await expect(salePage.timeline.locator('li').first()).toHaveAttribute('data-done', '');
    await expect(salePage.tracking).toContainText('USPS tracking');
    const link = salePage.tracking.getByRole('link');
    await expect(link).toHaveAttribute('href', uspsTracking.url);
    await expect(link).toContainText('9400 1118 9922 3456 1204 77');
    await expect(salePage.addTracking).toHaveCount(0);
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('a malformed tracking number is refused inline and the input is kept', async ({ page }) => {
    await serveSale(page);
    await page.route('**/api/orders/o-1/shipment', (route) =>
      route.fulfill(
        problem(422, 'TRACKING_INVALID', 'Check the highlighted fields', {
          trackingNumber: {
            code: 'TRACKING_MALFORMED',
            message: 'That doesn’t look like a USPS tracking number. It’s usually 22 digits.',
          },
        }),
      ),
    );
    const salePage = new SaleDetailPage(page);
    await salePage.goto();
    await salePage.addTracking.click();
    await salePage.trackingNumber.fill('9400 1118');

    await salePage.markShipped.click();

    await expect(salePage.trackingError).toHaveText(
      'That doesn’t look like a USPS tracking number. It’s usually 22 digits.',
    );
    await expect(salePage.trackingNumber).toHaveAttribute('aria-invalid', 'true');
    await expect(salePage.trackingNumber).toHaveValue('9400 1118');
    await expect(salePage.trackingNumber).toBeFocused();
    await expect(salePage.heading).toHaveText('You made a sale');
  });

  test('an empty tracking number is refused without calling the API', async ({ page, unfixturedApiCalls }) => {
    await serveSale(page);
    const salePage = new SaleDetailPage(page);
    await salePage.goto();
    await salePage.addTracking.click();

    await salePage.markShipped.click();

    await expect(salePage.trackingError).toHaveText('Enter the tracking number.');
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('while saving the dialog is busy, and a failure keeps the inputs for another try', async ({ page }) => {
    await serveSale(page);
    let release: () => void = () => undefined;
    const held = new Promise<void>((resolve) => (release = resolve));
    await page.route('**/api/orders/o-1/shipment', async (route: Route) => {
      await held;
      await route.fulfill(problem(500, 'SERVER_ERROR', 'Something went wrong'));
    });
    const salePage = new SaleDetailPage(page);
    await salePage.goto();
    await salePage.addTracking.click();
    await salePage.carrier.selectOption('UPS');
    await salePage.trackingNumber.fill('1Z999AA10123456784');

    await salePage.markShipped.click();

    await expect(salePage.markShipped).toHaveAttribute('aria-busy', 'true');
    await expect(salePage.cancel).toBeDisabled();
    release();
    await expect(salePage.dialogAlert).toContainText('That didn’t go through');
    await expect(salePage.dialogAlert).toContainText('Nothing changed. Check your connection and try again.');
    await expect(salePage.markShipped).toHaveText('Try again');
    await expect(salePage.carrier).toHaveValue('UPS');
    await expect(salePage.trackingNumber).toHaveValue('1Z999AA10123456784');
    await expect(salePage.cancel).toBeEnabled();
  });

  test('Escape closes the dialog and returns focus to Add tracking', async ({ page }) => {
    await serveSale(page);
    const salePage = new SaleDetailPage(page);
    await salePage.goto();
    await salePage.addTracking.click();
    await expect(salePage.carrier).toBeFocused();

    await page.keyboard.press('Escape');

    await expect(salePage.dialog).toBeHidden();
    await expect(salePage.addTracking).toBeFocused();
  });

  test('a shipped sale shows the timeline and the tracking link', async ({ page }) => {
    await serveSale(page, shippedSale);
    const salePage = new SaleDetailPage(page);
    await salePage.goto();

    await expect(salePage.heading).toHaveText('Shipped');
    await expect(salePage.timeline.locator('li')).toHaveText([
      'PaidSat 24 Oct',
      'ShippedSun 25 Oct',
      'Delivered',
      'Completed',
    ]);
    await expect(salePage.tracking.getByRole('link')).toHaveAttribute('target', '_blank');
    await expect(salePage.shipTo).toBeVisible();
  });

  test('a cancelled sale says the buyer was refunded', async ({ page }) => {
    await serveSale(page, sale({ status: 'Cancelled' }));
    const salePage = new SaleDetailPage(page);
    await salePage.goto();

    await expect(salePage.heading).toHaveText('Cancelled');
    await expect(salePage.alert).toContainText('Cancelled · Buyer refunded in full');
    await expect(salePage.addTracking).toHaveCount(0);
  });

  test('the sale page shows loading, then an error with a reference and a retry', async ({ page }) => {
    let calls = 0;
    let release: () => void = () => undefined;
    const held = new Promise<void>((resolve) => (release = resolve));
    await page.route('**/api/sales/o-1', async (route) => {
      calls++;
      if (calls === 1) {
        await held;
        return route.fulfill(problem(500, 'SERVER_ERROR', 'Something went wrong'));
      }
      return route.fulfill({ json: sale() });
    });
    const salePage = new SaleDetailPage(page);
    await salePage.goto();

    await expect(salePage.loading).toBeVisible();
    release();
    await expect(salePage.heading).toHaveText('Couldn’t load this sale');
    await expect(salePage.centerState).toContainText('Ref 5c1d-0e7a');
    await expect(salePage.centerState.getByRole('link', { name: 'Back to sales' })).toHaveAttribute(
      'href',
      '/account/sales',
    );
    await salePage.centerState.getByRole('button', { name: 'Try again' }).click();
    await expect(salePage.heading).toHaveText('You made a sale');
  });
});

test.describe('ship order: buyer', () => {
  test.beforeEach(async ({ page }) => {
    await signInAs(page, marcus);
    await page.route('**/photos/**', (route) => route.fulfill({ contentType: 'image/png', body: Buffer.alloc(0) }));
  });

  async function openOrder(
    page: import('@playwright/test').Page,
    body: Record<string, unknown>,
  ): Promise<OrderDetailPage> {
    await page.route('**/api/purchases/o-1', (route) => route.fulfill({ json: body }));
    const orderPage = new OrderDetailPage(page);
    await orderPage.goto();
    return orderPage;
  }

  test('a paid order is waiting to ship, with the automatic refund promised', async ({ page, unfixturedApiCalls }) => {
    const orderPage = await openOrder(page, buyerOrder());

    await expect(orderPage.heading).toHaveText('Waiting to ship');
    await expect(page).toHaveTitle('Order · TuTuMuch');
    await expect(orderPage.eyebrow).toHaveText('Order TM-48377 · Sat 24 Oct');
    await expect(orderPage.currentStep).toHaveText('PaidSat 24 Oct');
    await expect(orderPage.shipNote).toHaveText(
      'tutu_tessa ships by Fri 30 Oct. If not, you’re refunded automatically.',
    );
    await expect(orderPage.summary.locator('dl')).toContainText('Total$102.34');
    await expect(orderPage.summary).toContainText('Sellertutu_tessa');
    await expect(orderPage.tracking).toHaveCount(0);
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('a shipped order is on its way with the tracking link', async ({ page }) => {
    const orderPage = await openOrder(
      page,
      buyerOrder({ status: 'Shipped', timeline: timeline('2026-10-25T10:00:00Z'), tracking: uspsTracking }),
    );

    await expect(orderPage.heading).toHaveText('On its way');
    await expect(orderPage.currentStep).toHaveText('ShippedSun 25 Oct');
    await expect(orderPage.tracking).toContainText('USPS tracking');
    await expect(orderPage.tracking.getByRole('link')).toHaveAttribute('href', uspsTracking.url);
  });

  test('a delivered order shows the delivery date', async ({ page }) => {
    const orderPage = await openOrder(
      page,
      buyerOrder({
        status: 'Delivered',
        timeline: timeline('2026-10-25T10:00:00Z', '2026-10-28T18:00:00Z'),
        tracking: uspsTracking,
      }),
    );

    await expect(orderPage.heading).toHaveText('Delivered');
    await expect(orderPage.currentStep).toHaveText('DeliveredWed 28 Oct');
  });

  test('an order cancelled for a missed ship-by date says it was refunded in full', async ({ page }) => {
    const orderPage = await openOrder(page, buyerOrder({ status: 'Cancelled' }));

    await expect(orderPage.heading).toHaveText('Cancelled');
    await expect(orderPage.alert).toContainText('Refunded in full');
    await expect(orderPage.alert).toContainText('$102.34 back to Visa ending 4242. Allow 5–10 days.');
    await expect(orderPage.timeline).toHaveCount(0);
  });

  test('the order page shows an error with a way back to purchases', async ({ page }) => {
    await page.route('**/api/purchases/o-1', (route) =>
      route.fulfill(problem(500, 'SERVER_ERROR', 'Something went wrong')),
    );
    const orderPage = new OrderDetailPage(page);
    await orderPage.goto();

    await expect(orderPage.heading).toHaveText('Couldn’t load this order');
    await expect(orderPage.centerState.getByRole('link', { name: 'Back to purchases' })).toHaveAttribute(
      'href',
      '/account/purchases',
    );
  });
});
