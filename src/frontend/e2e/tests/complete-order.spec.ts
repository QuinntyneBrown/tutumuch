// Acceptance Test
// Traces to: L2-034
// Description: The buyer of a Delivered order sees when it completes automatically and confirms
// receipt with "I received it" in the Confirm receipt dialog; the order becomes Completed with the
// full timeline. A Shipped order can be confirmed too. The dialog shows busy and failed states and
// "Not yet" closes it. A completed order shows the full timeline and no actions.

import { Page, Route } from '@playwright/test';

import { buyerOrder, expect, marcus, problem, signInAs, test, timeline, uspsTracking } from '../fixtures';
import { OrderDetailPage } from '../pages/order-detail.page';

const delivered = buyerOrder({
  status: 'Delivered',
  timeline: timeline('2026-10-25T10:00:00Z', '2026-10-28T18:00:00Z'),
  tracking: uspsTracking,
  autoCompletesAt: '2026-10-31T18:00:00Z',
});

const completed = buyerOrder({
  status: 'Completed',
  timeline: [
    { status: 'Paid', at: '2026-10-24T15:00:00Z' },
    { status: 'Shipped', at: '2026-10-25T10:00:00Z' },
    { status: 'Delivered', at: '2026-10-28T18:00:00Z' },
    { status: 'Completed', at: '2026-10-29T09:00:00Z' },
  ],
  tracking: uspsTracking,
  autoCompletesAt: null,
});

test.describe('complete order', () => {
  test.beforeEach(async ({ page }) => {
    await signInAs(page, marcus);
    await page.route('**/photos/**', (route) => route.fulfill({ contentType: 'image/png', body: Buffer.alloc(0) }));
  });

  async function openOrder(page: Page, body: Record<string, unknown>): Promise<OrderDetailPage> {
    await page.route('**/api/purchases/o-1', (route) => route.fulfill({ json: body }));
    const orderPage = new OrderDetailPage(page);
    await orderPage.goto();
    return orderPage;
  }

  test('a delivered order says when it completes automatically and offers I received it', async ({
    page,
    unfixturedApiCalls,
  }) => {
    const orderPage = await openOrder(page, delivered);

    await expect(orderPage.heading).toHaveText('Delivered');
    await expect(orderPage.completesNote).toHaveText(
      'Completes automatically on Sat 31 Oct unless you report a problem.',
    );
    await expect(orderPage.confirmReceipt).toBeVisible();
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('confirming receipt completes the order', async ({ page, unfixturedApiCalls }) => {
    const methods: string[] = [];
    await page.route('**/api/orders/o-1/receipt', (route) => {
      methods.push(route.request().method());
      return route.fulfill({ json: completed });
    });
    const orderPage = await openOrder(page, delivered);

    await orderPage.confirmReceipt.click();

    await expect(orderPage.dialog).toBeVisible();
    await expect(orderPage.dialog).toContainText(
      'This completes the order and pays tutu_tessa. You can’t report a problem afterwards.',
    );
    await expect(orderPage.dialog.locator('.item-row')).toContainText('Floral party dress, pink');
    await expect(orderPage.completeOrder).toHaveText('Yes, complete order');
    await orderPage.completeOrder.click();

    await expect(orderPage.dialog).toBeHidden();
    expect(methods).toEqual(['POST']);
    await expect(orderPage.heading).toHaveText('Completed');
    await expect(orderPage.currentStep).toHaveText('CompletedThu 29 Oct');
    await expect(orderPage.timeline.locator('li[data-done]')).toHaveCount(3);
    await expect(orderPage.confirmReceipt).toHaveCount(0);
    await expect(orderPage.completesNote).toHaveCount(0);
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('a shipped order can be confirmed as received', async ({ page }) => {
    await page.route('**/api/orders/o-1/receipt', (route) => route.fulfill({ json: completed }));
    const orderPage = await openOrder(
      page,
      buyerOrder({ status: 'Shipped', timeline: timeline('2026-10-25T10:00:00Z'), tracking: uspsTracking }),
    );
    await expect(orderPage.completesNote).toHaveCount(0);

    await orderPage.confirmReceipt.click();
    await orderPage.completeOrder.click();

    await expect(orderPage.heading).toHaveText('Completed');
  });

  test('while completing the dialog is busy, and a failure offers another try', async ({ page }) => {
    let release: () => void = () => undefined;
    const held = new Promise<void>((resolve) => (release = resolve));
    await page.route('**/api/orders/o-1/receipt', async (route: Route) => {
      await held;
      await route.fulfill(problem(500, 'SERVER_ERROR', 'Something went wrong'));
    });
    const orderPage = await openOrder(page, delivered);
    await orderPage.confirmReceipt.click();

    await orderPage.completeOrder.click();

    await expect(orderPage.completeOrder).toHaveAttribute('aria-busy', 'true');
    await expect(orderPage.notYet).toBeDisabled();
    release();
    await expect(orderPage.dialogAlert).toContainText('That didn’t go through');
    await expect(orderPage.completeOrder).toHaveText('Try again');
    await expect(orderPage.notYet).toBeEnabled();
    await expect(orderPage.heading).toHaveText('Delivered');
  });

  test('Not yet closes the dialog and returns focus to I received it', async ({ page, unfixturedApiCalls }) => {
    const orderPage = await openOrder(page, delivered);
    await orderPage.confirmReceipt.click();
    await expect(orderPage.dialog).toBeVisible();

    await orderPage.notYet.click();

    await expect(orderPage.dialog).toBeHidden();
    await expect(orderPage.confirmReceipt).toBeFocused();
    await expect(orderPage.heading).toHaveText('Delivered');
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('a completed order shows the full timeline and nothing left to do', async ({ page }) => {
    const orderPage = await openOrder(page, completed);

    await expect(orderPage.heading).toHaveText('Completed');
    await expect(orderPage.currentStep).toHaveText('CompletedThu 29 Oct');
    await expect(orderPage.confirmReceipt).toHaveCount(0);
    await expect(orderPage.completesNote).toHaveCount(0);
  });
});
