// Acceptance Test
// Traces to: L2-031
// Description: The seller's payouts page shows paid out and pending totals, the bank, and each
// sale's payout after the fee; a seller who has not finished onboarding is asked to set up payouts
// and is sent to the provider-hosted onboarding. The page has loading, empty and error states. The
// payout banner above the top bar reflects onboarding and payout state, and a completed sale shows
// the payout after the TuTuMuch fee.

import { Page } from '@playwright/test';

import { expect, marcus, payoutRow, payoutSummary, problem, sale, signInAs, test } from '../fixtures';
import { PayoutsPage } from '../pages/payouts.page';
import { SaleDetailPage } from '../pages/sale-detail.page';

const notOnboarded = payoutSummary({
  paidOut: '0.00',
  onboarding: 'NotStarted',
  bankDisplay: null,
  banner: 'warning',
  history: [payoutRow()],
});

async function servePayouts(page: Page, body: Record<string, unknown>): Promise<void> {
  await page.route('**/api/payouts', (route) => route.fulfill({ json: body }));
}

test.describe('seller payouts', () => {
  test.beforeEach(async ({ page }) => {
    await signInAs(page, marcus);
    await page.route('**/photos/**', (route) => route.fulfill({ contentType: 'image/png', body: Buffer.alloc(0) }));
  });

  test('an onboarded seller sees totals, the bank and each payout after the fee', async ({
    page,
    unfixturedApiCalls,
  }) => {
    await servePayouts(page, payoutSummary());
    const payouts = new PayoutsPage(page);
    await payouts.goto();

    await expect(payouts.heading).toHaveText('Payouts');
    await expect(payouts.paidOut).toHaveText('$33.30');
    await expect(payouts.pending).toHaveText('$40.50');
    await expect(payouts.bank).toContainText('Chase •••• 6021');
    await expect(payouts.bank).toContainText('Managed by our payment provider');
    await expect(payouts.sectionHeading).toHaveText('History');
    await expect(payouts.rows).toHaveCount(2);
    const held = payouts.row('TM-48302');
    await expect(held).toContainText('Unicorn hoodie, lilac');
    await expect(held).toContainText('TM-48302 · Held until the order completes');
    await expect(held).toContainText('$40.50');
    await expect(held).toContainText('Pending');
    const paid = payouts.row('TM-47511');
    await expect(paid).toContainText('TM-47511 · Fee $3.70 · Mon 5 Oct');
    await expect(paid).toContainText('$33.30');
    await expect(paid).toContainText('Paid');
    await expect(paid.getByRole('link')).toHaveAttribute('href', '/sales/o-2');
    await expect(payouts.setupAlert).toHaveCount(0);
    await expect(payouts.banner).toHaveCount(0);
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('a seller who has not onboarded is sent to the provider-hosted onboarding', async ({
    page,
    unfixturedApiCalls,
  }) => {
    await servePayouts(page, notOnboarded);
    let linkRequests = 0;
    await page.route('**/api/payouts/onboarding-link', (route) => {
      linkRequests++;
      return route.fulfill({ json: { url: 'https://payments.example.com/onboarding/acct_1' } });
    });
    await page.route('https://payments.example.com/**', (route) =>
      route.fulfill({ contentType: 'text/html', body: '<h1>Provider onboarding</h1>' }),
    );
    const payouts = new PayoutsPage(page);
    await payouts.goto();

    await expect(payouts.setupAlert).toContainText('Set up payouts to get paid');
    await expect(payouts.setupAlert).toContainText('$40.50 is waiting for you.');
    await expect(payouts.sectionHeading).toHaveText('Held');
    await expect(payouts.row('TM-48302')).toContainText('Held until the order completes');
    await expect(payouts.paidOut).toHaveCount(0);
    await expect(payouts.banner).toHaveCount(0);

    await payouts.setUp.click();

    await expect(page).toHaveURL('https://payments.example.com/onboarding/acct_1');
    expect(linkRequests).toBe(1);
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('a provider outage keeps the seller on the page with a message', async ({ page }) => {
    await servePayouts(page, notOnboarded);
    await page.route('**/api/payouts/onboarding-link', (route) =>
      route.fulfill(
        problem(502, 'PAYOUT_PROVIDER_UNAVAILABLE', 'Payout setup isn’t available right now. Try again in a moment.'),
      ),
    );
    const payouts = new PayoutsPage(page);
    await payouts.goto();

    await payouts.setUp.click();

    await expect(payouts.alert).toContainText('Payout setup isn’t available right now. Try again in a moment.');
    await expect(page).toHaveURL(/\/account\/payouts$/);
  });

  test('returning from onboarding shows the success banner once it is complete', async ({ page }) => {
    await servePayouts(page, payoutSummary({ history: [payoutRow()], paidOut: '0.00' }));
    const payouts = new PayoutsPage(page);
    await payouts.goto('?onboarding=return');

    await expect(payouts.banner).toHaveClass(/alert--success/);
    await expect(payouts.banner).toContainText('Payouts are set up. You’ll be paid when orders complete.');

    await payouts.dismissBanner.click();
    await expect(payouts.banner).toHaveCount(0);
  });

  test('a seller with no sales sees the empty state', async ({ page }) => {
    await servePayouts(
      page,
      payoutSummary({ paidOut: '0.00', pending: '0.00', onboarding: 'NotStarted', history: [] }),
    );
    const payouts = new PayoutsPage(page);
    await payouts.goto();

    await expect(payouts.empty).toContainText('No payouts yet');
    await expect(payouts.empty).toContainText('Your earnings show up here after your first sale.');
    await expect(payouts.empty.getByRole('link', { name: 'Sell an item' })).toHaveAttribute('href', '/sell');
  });

  test('loading shows skeletons and a failure offers Try again', async ({ page }) => {
    let calls = 0;
    let release: () => void = () => undefined;
    const held = new Promise<void>((resolve) => (release = resolve));
    await page.route('**/api/payouts', async (route) => {
      calls++;
      if (calls === 1) {
        await held;
        return route.fulfill(problem(500, 'INTERNAL', 'Something went wrong'));
      }
      return route.fulfill({ json: payoutSummary() });
    });
    const payouts = new PayoutsPage(page);
    await payouts.goto();

    await expect(payouts.loading).toBeVisible();
    release();
    await expect(payouts.empty).toContainText('Couldn’t load payouts');
    await expect(payouts.empty).toContainText('Ref 5c1d-0e7a');

    await payouts.empty.getByRole('button', { name: 'Try again' }).click();
    await expect(payouts.rows).toHaveCount(2);
  });

  test('the warning banner asks to finish payout setup on other pages', async ({ page }) => {
    await servePayouts(page, notOnboarded);
    await page.route('**/api/sales/o-1', (route) => route.fulfill({ json: sale() }));
    const payouts = new PayoutsPage(page);
    await new SaleDetailPage(page).goto();

    await expect(payouts.banner).toHaveClass(/alert--warning/);
    await expect(payouts.banner).toContainText('Finish payout setup to receive $40.50.');
    await expect(payouts.dismissBanner).toHaveCount(0);

    await payouts.banner.getByRole('link', { name: 'Set up payouts' }).click();
    await expect(page).toHaveURL(/\/account\/payouts$/);
    await expect(payouts.setupAlert).toBeVisible();
    await expect(payouts.banner).toHaveCount(0);
  });

  test('the info banner says a payout is on its way and can be dismissed', async ({ page }) => {
    await servePayouts(
      page,
      payoutSummary({ banner: 'info', history: [payoutRow({ status: 'Sent', holdReason: null, amount: '33.30' })] }),
    );
    await page.route('**/api/sales/o-1', (route) => route.fulfill({ json: sale() }));
    const payouts = new PayoutsPage(page);
    await new SaleDetailPage(page).goto();

    await expect(payouts.banner).toHaveClass(/alert--info/);
    await expect(payouts.banner).toContainText('$33.30 is on its way to Chase •••• 6021.');
    await expect(payouts.banner.getByRole('link', { name: 'Payouts' })).toHaveAttribute('href', '/account/payouts');

    await payouts.dismissBanner.click();
    await expect(payouts.banner).toHaveCount(0);
  });

  test('the danger banner says the last payout was returned', async ({ page }) => {
    await servePayouts(
      page,
      payoutSummary({ banner: 'danger', history: [payoutRow({ status: 'Failed', holdReason: null })] }),
    );
    await page.route('**/api/sales/o-1', (route) => route.fulfill({ json: sale() }));
    const payouts = new PayoutsPage(page);
    await new SaleDetailPage(page).goto();

    await expect(payouts.banner).toHaveClass(/alert--danger/);
    await expect(payouts.banner).toContainText('Your last payout was returned by the bank.');
    await expect(payouts.banner.getByRole('link', { name: 'Update bank details' })).toBeVisible();
    await expect(payouts.dismissBanner).toHaveCount(0);
  });

  test('a completed sale shows the payout after the TuTuMuch fee', async ({ page }) => {
    await page.route('**/api/sales/o-1', (route) =>
      route.fulfill({
        json: sale({
          status: 'Completed',
          itemPrice: '30.00',
          shippingCost: '7.00',
          timeline: [
            { status: 'Paid', at: '2026-10-01T15:00:00Z' },
            { status: 'Shipped', at: '2026-10-02T10:00:00Z' },
            { status: 'Delivered', at: '2026-10-04T18:00:00Z' },
            { status: 'Completed', at: '2026-10-07T09:00:00Z' },
          ],
          payout: {
            gross: '37.00',
            feePercent: 10,
            fee: '3.70',
            net: '33.30',
            status: 'Sent',
            sentAt: '2026-10-07T12:00:00Z',
            bankDisplay: 'Chase •••• 6021',
          },
        }),
      }),
    );
    await new SaleDetailPage(page).goto();

    const panel = page.locator('main').locator('.panel--sunken');
    await expect(panel.getByRole('heading', { name: 'Payout' })).toBeVisible();
    await expect(panel).toContainText('Item and shipping$37.00');
    await expect(panel).toContainText('TuTuMuch fee (10%)−$3.70');
    await expect(panel).toContainText('Your payout$33.30');
    await expect(panel).toContainText('Sent Wed 7 Oct to Chase •••• 6021.');
  });
});
