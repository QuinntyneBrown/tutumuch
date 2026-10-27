// Acceptance Test
// Traces to: L2-035
// Description: The seller of a Paid order cancels it with a reason and an optional note in the
// Cancel order dialog; the sale becomes Cancelled, the buyer refunded in full, and "Relist item"
// returns the listing to the catalog. The buyer asks the seller to cancel; the seller sees the
// request quoted and accepts or declines it. The buyer sees the request waiting or declined, and a
// cancelled order with the seller's note. Every dialog shows its busy and failed states, and a
// Shipped order offers no cancellation.

import { Page, Route } from '@playwright/test';

import {
  buyerOrder,
  cancellationRequest,
  expect,
  marcus,
  problem,
  sale,
  signInAs,
  test,
  timeline,
  uspsTracking,
} from '../fixtures';
import {
  CancelOrderDialog,
  RelistDialog,
  RequestCancellationDialog,
  RespondCancellationDialog,
} from '../pages/cancel-order.page';
import { OrderDetailPage } from '../pages/order-detail.page';
import { SaleDetailPage } from '../pages/sale-detail.page';

const cancelledSale = sale({
  status: 'Cancelled',
  cancellation: { reason: 'ItemDamaged', reasonLabel: 'Item damaged', note: 'Found a crack while packing.' },
  canRelist: true,
});

async function openSale(page: Page, body: Record<string, unknown> = sale()): Promise<SaleDetailPage> {
  await page.route('**/api/sales/o-1', (route) => route.fulfill({ json: body }));
  const salePage = new SaleDetailPage(page);
  await salePage.goto();
  return salePage;
}

/** Holds the response to <url> until release() is called, then fails it with a 500. */
async function holdThenFail(page: Page, url: string): Promise<() => void> {
  let release: () => void = () => undefined;
  const held = new Promise<void>((resolve) => (release = resolve));
  await page.route(url, async (route: Route) => {
    await held;
    await route.fulfill(problem(500, 'SERVER_ERROR', 'Something went wrong'));
  });
  return release;
}

test.describe('cancel order: seller', () => {
  test.beforeEach(async ({ page }) => {
    await signInAs(page, marcus);
    await page.route('**/photos/**', (route) => route.fulfill({ contentType: 'image/png', body: Buffer.alloc(0) }));
  });

  test('the seller cancels a paid sale with a reason and a note', async ({ page, unfixturedApiCalls }) => {
    const sent: unknown[] = [];
    await page.route('**/api/orders/o-1/cancellation', (route) => {
      sent.push(route.request().postDataJSON());
      return route.fulfill({ json: cancelledSale });
    });
    const salePage = await openSale(page);
    const dialog = new CancelOrderDialog(page);

    await salePage.cancelOrder.click();

    await expect(dialog.root).toBeVisible();
    await expect(dialog.root).toContainText(
      'twirly_tara is refunded $48.15 in full. The listing stays out of the closet until you relist.',
    );
    await expect(dialog.reasons).toHaveCount(4);
    await expect(dialog.root.locator('fieldset label')).toHaveText([
      'Item damaged',
      'No longer have it',
      'Listed by mistake',
      'Buyer asked me to cancel',
    ]);
    await expect(dialog.reasons.first()).toBeFocused();
    await dialog.reason('Item damaged').check();
    await dialog.note.fill('Found a crack while packing.');
    await expect(dialog.submit).toHaveText('Cancel sale');
    await dialog.submit.click();

    await expect(dialog.root).toBeHidden();
    expect(sent).toEqual([{ reason: 'ItemDamaged', note: 'Found a crack while packing.' }]);
    await expect(salePage.heading).toHaveText('Cancelled');
    await expect(salePage.alert).toContainText('Cancelled · Buyer refunded in full');
    await expect(salePage.alert).toContainText('The listing stays out of the closet until you relist it.');
    await expect(salePage.relistItem).toBeVisible();
    await expect(salePage.addTracking).toHaveCount(0);
    await expect(salePage.cancelOrder).toHaveCount(0);
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('a cancellation without a reason is refused inline without calling the API', async ({
    page,
    unfixturedApiCalls,
  }) => {
    const salePage = await openSale(page);
    const dialog = new CancelOrderDialog(page);
    await salePage.cancelOrder.click();

    await dialog.submit.click();

    await expect(dialog.reasonError).toHaveText('Choose a reason');
    await expect(dialog.reasons.first()).toHaveAttribute('aria-invalid', 'true');
    await expect(dialog.reasons.first()).toBeFocused();
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('while cancelling the dialog is busy, and a failure keeps the choices for another try', async ({ page }) => {
    const release = await holdThenFail(page, '**/api/orders/o-1/cancellation');
    const salePage = await openSale(page);
    const dialog = new CancelOrderDialog(page);
    await salePage.cancelOrder.click();
    await dialog.reason('No longer have it').check();
    await dialog.note.fill('Sold it at a market, sorry.');

    await dialog.submit.click();

    await expect(dialog.submit).toHaveAttribute('aria-busy', 'true');
    await expect(dialog.keep).toBeDisabled();
    release();
    await expect(dialog.alert).toContainText('That didn’t go through');
    await expect(dialog.submit).toHaveText('Try again');
    await expect(dialog.reason('No longer have it')).toBeChecked();
    await expect(dialog.note).toHaveValue('Sold it at a market, sorry.');
    await dialog.keep.click();
    await expect(dialog.root).toBeHidden();
    await expect(salePage.heading).toHaveText('You made a sale');
  });

  test('the seller sees a buyer’s request quoted and accepts it', async ({ page }) => {
    const sent: unknown[] = [];
    await page.route('**/api/orders/o-1/cancellation-request/response', (route) => {
      sent.push(route.request().postDataJSON());
      return route.fulfill({
        json: sale({
          status: 'Cancelled',
          cancellation: { reason: 'BuyerAskedToCancel', reasonLabel: 'Buyer asked me to cancel', note: null },
          cancellationRequest: cancellationRequest('Accepted'),
          canRelist: true,
        }),
      });
    });
    const salePage = await openSale(page, sale({ cancellationRequest: cancellationRequest() }));

    await expect(salePage.heading).toHaveText('Buyer asked to cancel');
    await expect(salePage.quote).toContainText('“Ordered the wrong colour, sorry. Can we cancel?”');
    await expect(salePage.quote).toContainText('twirly_tara');
    await expect(salePage.shipTo).toBeVisible();
    await expect(salePage.addTracking).toHaveCount(0);
    await salePage.respond.click();
    const dialog = new RespondCancellationDialog(page, 'twirly_tara');
    await expect(dialog.root).toContainText(
      'Accepting refunds $48.15 in full. Declining keeps the order — you still ship by Fri 30 Oct.',
    );
    await expect(dialog.quote).toContainText('Ordered the wrong colour, sorry. Can we cancel?');
    await expect(dialog.accept).toBeFocused();
    await dialog.accept.click();

    await expect(dialog.root).toBeHidden();
    expect(sent).toEqual([{ decision: 'Accept' }]);
    await expect(salePage.heading).toHaveText('Cancelled');
    await expect(salePage.relistItem).toBeVisible();
  });

  test('declining the request keeps the sale to ship', async ({ page }) => {
    const sent: unknown[] = [];
    await page.route('**/api/orders/o-1/cancellation-request/response', (route) => {
      sent.push(route.request().postDataJSON());
      return route.fulfill({ json: sale({ cancellationRequest: cancellationRequest('Declined') }) });
    });
    const salePage = await openSale(page, sale({ cancellationRequest: cancellationRequest() }));
    await salePage.respond.click();
    const dialog = new RespondCancellationDialog(page, 'twirly_tara');

    await dialog.decline.click();

    await expect(dialog.root).toBeHidden();
    expect(sent).toEqual([{ decision: 'Decline' }]);
    await expect(salePage.heading).toHaveText('You made a sale');
    await expect(salePage.addTracking).toBeVisible();
    await expect(salePage.cancelOrder).toBeVisible();
  });

  test('a failed response keeps the dialog open for another try', async ({ page }) => {
    const release = await holdThenFail(page, '**/api/orders/o-1/cancellation-request/response');
    const salePage = await openSale(page, sale({ cancellationRequest: cancellationRequest() }));
    await salePage.respond.click();
    const dialog = new RespondCancellationDialog(page, 'twirly_tara');

    await dialog.accept.click();

    await expect(dialog.accept).toHaveAttribute('aria-busy', 'true');
    await expect(dialog.decline).toBeDisabled();
    release();
    await expect(dialog.alert).toContainText('Nothing changed. Check your connection and try again.');
    await expect(dialog.decline).toBeEnabled();
    await expect(salePage.heading).toHaveText('Buyer asked to cancel');
  });

  test('the seller relists the item of a cancelled sale', async ({ page, unfixturedApiCalls }) => {
    let relisted = 0;
    await page.route('**/api/listings/L-10001/relist', (route) => {
      relisted++;
      return route.fulfill({ status: 204 });
    });
    const salePage = await openSale(page, cancelledSale);

    await salePage.relistItem.click();
    const dialog = new RelistDialog(page);
    await expect(dialog.root).toContainText(
      'It goes back in the closet with its original photos, price and description.',
    );
    await expect(dialog.item).toContainText('Unicorn hoodie, lilac');
    await expect(dialog.item).toContainText('$38.00 · $7.00 shipping');
    await expect(dialog.relist).toBeFocused();
    await dialog.relist.click();

    await expect(dialog.root).toBeHidden();
    expect(relisted).toBe(1);
    await expect(salePage.relistItem).toHaveCount(0);
    await expect(salePage.alert).toContainText('The listing is back in the closet.');
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('a failed relist keeps the dialog open, and Not now closes it', async ({ page }) => {
    const release = await holdThenFail(page, '**/api/listings/L-10001/relist');
    const salePage = await openSale(page, cancelledSale);
    await salePage.relistItem.click();
    const dialog = new RelistDialog(page);

    await dialog.relist.click();

    await expect(dialog.relist).toHaveAttribute('aria-busy', 'true');
    release();
    await expect(dialog.alert).toContainText('That didn’t go through');
    await dialog.notNow.click();
    await expect(dialog.root).toBeHidden();
    await expect(salePage.relistItem).toBeVisible();
  });

  test('a shipped sale offers no cancellation', async ({ page }) => {
    const salePage = await openSale(
      page,
      sale({ status: 'Shipped', timeline: timeline('2026-10-25T10:00:00Z'), tracking: uspsTracking }),
    );

    await expect(salePage.heading).toHaveText('Shipped');
    await expect(salePage.cancelOrder).toHaveCount(0);
  });
});

test.describe('cancel order: buyer', () => {
  test.beforeEach(async ({ page }) => {
    await signInAs(page, marcus);
    await page.route('**/photos/**', (route) => route.fulfill({ contentType: 'image/png', body: Buffer.alloc(0) }));
  });

  async function openOrder(page: Page, body: Record<string, unknown> = buyerOrder()): Promise<OrderDetailPage> {
    await page.route('**/api/purchases/o-1', (route) => route.fulfill({ json: body }));
    const orderPage = new OrderDetailPage(page);
    await orderPage.goto();
    return orderPage;
  }

  test('the buyer asks the seller to cancel a paid order', async ({ page, unfixturedApiCalls }) => {
    const sent: unknown[] = [];
    await page.route('**/api/orders/o-1/cancellation-request', (route) => {
      sent.push(route.request().postDataJSON());
      return route.fulfill({ json: buyerOrder({ cancellationRequest: cancellationRequest() }) });
    });
    const orderPage = await openOrder(page);
    const dialog = new RequestCancellationDialog(page, 'tutu_tessa');

    await orderPage.askToCancel.click();

    await expect(dialog.root).toContainText(
      'If tutu_tessa accepts, you’re refunded in full. Until then the order stays as is.',
    );
    await expect(dialog.message).toBeFocused();
    await dialog.message.fill('Ordered the wrong colour, sorry. Can we cancel?');
    await dialog.send.click();

    await expect(dialog.root).toBeHidden();
    expect(sent).toEqual([{ message: 'Ordered the wrong colour, sorry. Can we cancel?' }]);
    await expect(orderPage.heading).toHaveText('Waiting to ship');
    await expect(orderPage.requestNote).toHaveText('You asked tutu_tessa to cancel. They haven’t answered yet.');
    await expect(orderPage.askToCancel).toHaveCount(0);
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('a failed request keeps the message for another try', async ({ page }) => {
    const release = await holdThenFail(page, '**/api/orders/o-1/cancellation-request');
    const orderPage = await openOrder(page);
    await orderPage.askToCancel.click();
    const dialog = new RequestCancellationDialog(page, 'tutu_tessa');
    await dialog.message.fill('Wrong colour');

    await dialog.send.click();

    await expect(dialog.send).toHaveAttribute('aria-busy', 'true');
    await expect(dialog.keep).toBeDisabled();
    release();
    await expect(dialog.alert).toContainText('That didn’t go through');
    await expect(dialog.send).toHaveText('Try again');
    await expect(dialog.message).toHaveValue('Wrong colour');
  });

  test('a declined request says the seller still ships, and cannot be sent again', async ({ page }) => {
    const orderPage = await openOrder(page, buyerOrder({ cancellationRequest: cancellationRequest('Declined') }));

    await expect(orderPage.requestNote).toHaveText('tutu_tessa declined to cancel. They still ship by Fri 30 Oct.');
    await expect(orderPage.askToCancel).toHaveCount(0);
  });

  test('an order the seller cancelled shows the refund and the seller’s note', async ({ page }) => {
    const orderPage = await openOrder(
      page,
      buyerOrder({
        status: 'Cancelled',
        cancellation: { reason: 'ItemDamaged', reasonLabel: 'Item damaged', note: 'Found a crack while packing.' },
      }),
    );

    await expect(orderPage.heading).toHaveText('Cancelled');
    await expect(orderPage.alert).toContainText('Refunded in full');
    await expect(orderPage.alert).toContainText('$102.34 back to Visa ending 4242. Allow 5–10 days.');
    await expect(orderPage.quote).toContainText('“Found a crack while packing.”');
    await expect(orderPage.quote).toContainText('tutu_tessa');
    await expect(orderPage.findSimilar).toHaveAttribute('href', '/');
  });

  test('a shipped order offers no cancellation', async ({ page }) => {
    const orderPage = await openOrder(
      page,
      buyerOrder({ status: 'Shipped', timeline: timeline('2026-10-25T10:00:00Z'), tracking: uspsTracking }),
    );

    await expect(orderPage.heading).toHaveText('On its way');
    await expect(orderPage.askToCancel).toHaveCount(0);
  });
});
