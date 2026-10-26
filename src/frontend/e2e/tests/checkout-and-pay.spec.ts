// Acceptance Test
// Traces to: L2-026, L2-027, L2-028, L2-029, L2-030, L2-063, L2-065
// Description: A verified member opens checkout from Buy now: the item is reserved for 10 minutes
// with a countdown, the saved address is offered, the summary shows item, shipping, tax and
// total, and paying sends only the provider's token with one idempotency key, then shows the
// confirmation once the webhook settled the payment. A new address is US-only with every missing
// field identified; a decline shows the reason and allows a retry; another buyer's reservation,
// an expiry and a failure each show their state; Cancel releases the item.

import {
  brooklyn,
  checkout,
  checkoutStart,
  expect,
  marcus,
  pricedCheckout,
  problem,
  purchase,
  serveCheckout,
  serveListing,
  listingDetail,
  signInAs,
  test,
} from '../fixtures';
import { CheckoutPage } from '../pages/checkout.page';
import { OrderConfirmationPage } from '../pages/order-confirmation.page';

const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/;

test.describe('checkout and pay', () => {
  test.beforeEach(async ({ page }) => {
    await page.clock.install({ time: checkoutStart });
    await signInAs(page, marcus);
    await page.route('**/api/purchases/o-1', (route) => route.fulfill({ json: purchase }));
  });

  test('a buyer pays with the saved address and sees the confirmation', async ({ page, unfixturedApiCalls }) => {
    // The installed clock follows real time, so under parallel load whole seconds passed before the
    // timer was read ("9:35 left"). Pause half a second in, so the countdown reads 9:41 for as long
    // as the test needs, tick 1 ms at a time to let the page render, then let time run again.
    await page.clock.pauseAt(new Date(checkoutStart.getTime() + 500));
    const sent = await serveCheckout(page);
    const checkoutPage = new CheckoutPage(page);
    await checkoutPage.goto();
    await expect(async () => {
      await page.clock.runFor(1);
      await expect(checkoutPage.timer).toHaveText('Reserved for you · 9:41 left', { timeout: 200 });
    }).toPass();
    await page.clock.resume();

    await expect(checkoutPage.heading).toHaveText('Checkout');
    await expect(page).toHaveTitle('Checkout · Tutu Much');
    await expect(checkoutPage.savedAddress).toBeChecked();
    await expect(checkoutPage.summary).toContainText('Floral party dress, pink');
    await expect(checkoutPage.summary).toContainText("Gymboree · Girls' · Used - Excellent");
    await expect(checkoutPage.summary.locator('dl')).toContainText('Item$85.00');
    await expect(checkoutPage.summary.locator('dl')).toContainText('Shipping$9.00');
    await expect(checkoutPage.summary.locator('dl')).toContainText('Sales tax$8.34');
    await expect(checkoutPage.summary.locator('dl')).toContainText('Total$102.34');
    await expect(checkoutPage.secureNote).toHaveText(
      'Card details go straight to our payment provider. Tutu Much never sees them.',
    );
    await expect(checkoutPage.pay).toHaveText('Pay $102.34');

    await checkoutPage.pay.click();

    const confirmation = new OrderConfirmationPage(page);
    await expect(page).toHaveURL('/orders/o-1/confirmed');
    await expect(confirmation.heading).toHaveText('It’s yours.');
    await expect(confirmation.reference).toHaveText('Order TM-48377');
    await expect(confirmation.item).toContainText('Paid $102.34 · Visa ending 4242');
    await expect(confirmation.nextSteps.first()).toContainText('tutu_tessa ships it');
    await expect(confirmation.viewOrder).toHaveAttribute('href', '/orders/o-1');
    await expect(confirmation.keepShopping).toHaveAttribute('href', '/');

    const start = sent.find((r) => r.method === 'POST' && r.url.endsWith('/api/checkouts'));
    expect(start?.body).toEqual({ listingReference: 'L-10001' });
    const address = sent.find((r) => r.method === 'PUT');
    expect(address?.body).toEqual(brooklyn);
    const payments = sent.filter((r) => r.url.endsWith('/payments'));
    expect(payments).toHaveLength(1);
    expect(payments[0].headers['idempotency-key']).toMatch(UUID);
    // Only the provider's token and the shown total reach Tutu Much: no card number or security code (L2-030 AC1).
    expect(payments[0].body).toEqual({ paymentMethodToken: 'tok_visa', expectedTotal: 102.34 });
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('a double click on Pay sends one payment and shows it is confirming', async ({ page }) => {
    const sent = await serveCheckout(page, { polls: [pricedCheckout({ status: 'Processing' })] });
    const checkoutPage = new CheckoutPage(page);
    await checkoutPage.goto();
    await expect(checkoutPage.pay).toHaveText('Pay $102.34');

    await checkoutPage.pay.dblclick();

    await expect(checkoutPage.paying).toBeVisible();
    await expect(checkoutPage.pay).toHaveAttribute('aria-busy', 'true');
    await expect.poll(() => sent.filter((r) => r.url.endsWith('/payments')).length).toBe(1);
  });

  test('a new address with missing fields lists each one and marks the fields', async ({ page }) => {
    await serveCheckout(page, {
      address: problem(422, 'ADDRESS_INVALID', 'Check the highlighted fields', {
        line1: { code: 'STREET_REQUIRED', message: 'Enter a street address' },
        city: { code: 'CITY_REQUIRED', message: 'Enter a city' },
        zip: { code: 'ZIP_INVALID', message: 'Enter a 5-digit ZIP code' },
      }),
    });
    const checkoutPage = new CheckoutPage(page);
    await checkoutPage.goto();

    await checkoutPage.newAddress.check();
    await expect(checkoutPage.fullName).toHaveValue('Marcus Bell');
    await expect(checkoutPage.zip).toHaveAttribute('inputmode', 'numeric');
    await expect(checkoutPage.country).toHaveValue('US');
    await checkoutPage.state.selectOption('NY');
    await checkoutPage.pay.click();

    await expect(checkoutPage.errorSummary).toBeFocused();
    await expect(checkoutPage.errorSummary).toContainText('Fix these to continue');
    await expect(checkoutPage.errorSummary.getByRole('link')).toHaveText([
      'Enter a street address',
      'Enter a city',
      'Enter a 5-digit ZIP code',
    ]);
    await expect(checkoutPage.street).toHaveAttribute('aria-invalid', 'true');
    await expect(checkoutPage.city).toHaveAttribute('aria-invalid', 'true');
    await expect(checkoutPage.zip).toHaveAccessibleDescription(/Enter a 5-digit ZIP code$/);
    await checkoutPage.errorSummary.getByRole('link', { name: 'Enter a city' }).click();
    await expect(checkoutPage.city).toBeFocused();
  });

  test('an address outside the United States is refused', async ({ page }) => {
    const sent = await serveCheckout(page, {
      address: problem(422, 'NON_US_ADDRESS', 'Tutu Much ships within the United States only', {
        country: { code: 'NON_US_ADDRESS', message: 'Choose a US address to continue.' },
      }),
    });
    const checkoutPage = new CheckoutPage(page);
    await checkoutPage.goto();

    await checkoutPage.newAddress.check();
    await checkoutPage.street.fill('88 Queen St W');
    await checkoutPage.city.fill('Toronto');
    await checkoutPage.zip.fill('M5H 2M9');
    await checkoutPage.country.selectOption('CA');
    await checkoutPage.pay.click();

    await expect(checkoutPage.formAlert).toContainText('Tutu Much ships within the United States only');
    await expect(checkoutPage.formAlert).toContainText('Choose a US address to continue.');
    await expect(checkoutPage.country).toHaveAttribute('aria-invalid', 'true');
    expect(sent.filter((r) => r.url.endsWith('/payments'))).toHaveLength(0);
    expect(sent.filter((r) => r.method === 'PUT').at(-1)?.body).toMatchObject({ city: 'Toronto', country: 'CA' });
  });

  test('a new address that changes the tax shows the new total before any payment', async ({ page }) => {
    const sent = await serveCheckout(page);
    let puts = 0;
    await page.route('**/api/checkouts/c-1/address', (route) =>
      ++puts === 1 ? route.fallback() : route.fulfill({ json: pricedCheckout({ salesTax: '6.81', total: '100.81' }) }),
    );
    const checkoutPage = new CheckoutPage(page);
    await checkoutPage.goto();
    await expect(checkoutPage.pay).toHaveText('Pay $102.34');

    await checkoutPage.newAddress.check();
    await checkoutPage.street.fill('500 Congress Ave');
    await checkoutPage.city.fill('Austin');
    await checkoutPage.state.selectOption('TX');
    await checkoutPage.zip.fill('78701');
    await checkoutPage.pay.click();

    await expect(checkoutPage.formAlert).toContainText('The total changed');
    await expect(checkoutPage.summary.locator('dl')).toContainText('Total$100.81');
    await expect(checkoutPage.pay).toHaveText('Pay $100.81');
    expect(sent.filter((r) => r.url.endsWith('/payments'))).toHaveLength(0);
  });

  test('a declined card shows the reason and the buyer can try again with a new key', async ({ page }) => {
    const sent = await serveCheckout(page, {
      polls: [
        pricedCheckout({ status: 'Open', lastDeclineReason: 'Insufficient funds.' }),
        pricedCheckout({ status: 'Paid', orderId: 'o-1' }),
      ],
    });
    const checkoutPage = new CheckoutPage(page);
    await checkoutPage.goto();
    await checkoutPage.pay.click();

    await expect(checkoutPage.formAlert).toContainText('Your bank declined the card');
    await expect(checkoutPage.formAlert).toContainText(
      'Insufficient funds. Try another card — the item is still reserved for you.',
    );
    await expect(checkoutPage.pay).toBeEnabled();

    await checkoutPage.testCard.selectOption('tok_visa');
    await checkoutPage.pay.click();

    await expect(page).toHaveURL('/orders/o-1/confirmed');
    const keys = sent.filter((r) => r.url.endsWith('/payments')).map((r) => r.headers['idempotency-key']);
    expect(keys).toHaveLength(2);
    expect(keys[0]).not.toEqual(keys[1]);
  });

  test('a reservation by another buyer shows Someone else is checking out', async ({ page }) => {
    await serveCheckout(page, { start: problem(409, 'LISTING_RESERVED', 'Someone else is checking out') });
    const checkoutPage = new CheckoutPage(page);
    await checkoutPage.goto();

    await expect(checkoutPage.heading).toHaveText('Someone else is checking out');
    await expect(checkoutPage.centerState).toContainText('If they don’t finish within 10 minutes, it’s back on sale.');
    await expect(checkoutPage.centerState.getByRole('link', { name: 'Back to the item' })).toHaveAttribute(
      'href',
      '/items/L-10001',
    );
    await expect(checkoutPage.centerState.getByRole('link', { name: 'Keep shopping' })).toHaveAttribute('href', '/');
  });

  test('the seller is sent back to their own listing', async ({ page }) => {
    await serveCheckout(page, { start: problem(422, 'CANNOT_BUY_OWN_LISTING', 'You can’t buy your own listing') });
    await serveListing(page, listingDetail({ viewer: { relation: 'Seller' } }));
    const checkoutPage = new CheckoutPage(page);
    await checkoutPage.goto();

    await expect(page).toHaveURL('/items/L-10001');
  });

  test('a visitor is sent to sign in and back to checkout', async ({ page }) => {
    await serveCheckout(page, { start: problem(401, 'UNAUTHENTICATED', 'Sign in to continue') });
    const checkoutPage = new CheckoutPage(page);
    await checkoutPage.goto();

    await expect(page).toHaveURL('/sign-in?returnUrl=%2Fcheckout%2FL-10001');
  });

  test('a failure to start shows the reference and Try again starts again', async ({ page }) => {
    let calls = 0;
    await serveCheckout(page);
    await page.route('**/api/checkouts', (route) =>
      ++calls === 1 ? route.fulfill(problem(500, 'SERVER_ERROR', 'Server error')) : route.fallback(),
    );
    const checkoutPage = new CheckoutPage(page);
    await checkoutPage.goto();

    await expect(checkoutPage.heading).toHaveText('Couldn’t start checkout');
    await expect(checkoutPage.centerState).toContainText('Ref 5c1d-0e7a');
    await checkoutPage.centerState.getByRole('button', { name: 'Try again' }).click();

    await expect(checkoutPage.heading).toHaveText('Checkout');
  });

  test('skeletons show while the item is reserved', async ({ page }) => {
    let release: () => void = () => undefined;
    const held = new Promise<void>((resolve) => (release = resolve));
    await serveCheckout(page);
    await page.route('**/api/checkouts', async (route) => {
      await held;
      await route.fallback();
    });
    const checkoutPage = new CheckoutPage(page);
    await checkoutPage.goto();

    await expect(checkoutPage.loading).toBeVisible();
    release();
    await expect(checkoutPage.heading).toHaveText('Checkout');
  });

  test('the timer warns in the last minute and at zero disables Pay', async ({ page }) => {
    // The installed clock follows real time, so a slow run used to see seconds gone before the
    // first check. Pause one minute in, before the page loads, so the countdown starts at a known
    // instant and moves only when the test runs the clock.
    await page.clock.pauseAt(new Date(checkoutStart.getTime() + 60 * 1000));
    await serveCheckout(page);
    const checkoutPage = new CheckoutPage(page);
    await checkoutPage.goto();
    await expect(async () => {
      await page.clock.runFor(10);
      await expect(checkoutPage.timer).toHaveText('Reserved for you · 8:41 left', { timeout: 200 });
    }).toPass();

    await page.clock.runFor((7 * 60 + 42) * 1000);

    await expect(checkoutPage.timer).toHaveText('Reserved for you · 0:59 left');
    await expect(checkoutPage.timer).toHaveClass(/timer--warning/);
    await expect(checkoutPage.timerBanner).toContainText(
      'Less than a minute left to pay. After that, the item goes back on sale.',
    );

    await page.clock.runFor(60 * 1000);

    await expect(checkoutPage.timer).toHaveText('Reserved for you · 0:00 left');
    await expect(checkoutPage.timer).toHaveClass(/timer--danger/);
    await expect(checkoutPage.timerBanner).toContainText('Your reservation ended. You weren’t charged.');
    await expect(checkoutPage.timerBanner.getByRole('link', { name: 'Start again' })).toHaveAttribute(
      'href',
      '/items/L-10001',
    );
    await expect(checkoutPage.pay).toBeDisabled();
  });

  test('paying after the reservation ended shows Your reservation ended', async ({ page }) => {
    await serveCheckout(page, { pay: problem(409, 'RESERVATION_EXPIRED', 'Your reservation ended') });
    const checkoutPage = new CheckoutPage(page);
    await checkoutPage.goto();
    await checkoutPage.pay.click();

    await expect(checkoutPage.heading).toHaveText('Your reservation ended');
    await expect(checkoutPage.centerState).toContainText('You weren’t charged.');
    await expect(checkoutPage.centerState.getByRole('link', { name: 'Back to the item' })).toHaveAttribute(
      'href',
      '/items/L-10001',
    );
  });

  test('Cancel and release item frees the item and goes back to it', async ({ page }) => {
    const sent = await serveCheckout(page);
    await serveListing(page, listingDetail());
    const checkoutPage = new CheckoutPage(page);
    await checkoutPage.goto();
    await expect(checkoutPage.pay).toHaveText('Pay $102.34');

    await checkoutPage.cancel.click();

    await expect(page).toHaveURL('/items/L-10001');
    expect(sent.some((r) => r.method === 'DELETE' && r.url.endsWith('/api/checkouts/c-1'))).toBe(true);
  });

  test('without a saved address the new address form is shown first', async ({ page }) => {
    await serveCheckout(page, { start: checkout({ savedAddress: null }) });
    const checkoutPage = new CheckoutPage(page);
    await checkoutPage.goto();

    await expect(checkoutPage.street).toBeVisible();
    await expect(checkoutPage.savedAddress).toHaveCount(0);
    await expect(checkoutPage.summary.locator('dl')).toContainText('Sales taxWith your address');
    await expect(checkoutPage.pay).toHaveText('Continue');
  });

  test('on a phone the page has one column and no horizontal scroll', async ({ page }) => {
    await page.setViewportSize({ width: 320, height: 700 });
    await serveCheckout(page);
    const checkoutPage = new CheckoutPage(page);
    await checkoutPage.goto();
    await expect(checkoutPage.pay).toHaveText('Pay $102.34');

    const overflow = await page.evaluate(
      () => document.documentElement.scrollWidth - document.documentElement.clientWidth,
    );
    expect(overflow).toBeLessThanOrEqual(0);
  });
});

test.describe('order confirmation', () => {
  test.beforeEach(async ({ page }) => {
    await signInAs(page, marcus);
  });

  test('the confirmation waits for the order, then shows it', async ({ page }) => {
    let release: () => void = () => undefined;
    const held = new Promise<void>((resolve) => (release = resolve));
    await page.route('**/api/purchases/o-1', async (route) => {
      await held;
      await route.fulfill({ json: purchase });
    });
    const confirmation = new OrderConfirmationPage(page);
    await confirmation.goto();

    await expect(confirmation.heading).toHaveText('Confirming your payment');
    await expect(confirmation.centerState).toContainText('This usually takes a few seconds. Don’t close this page.');
    release();
    await expect(confirmation.heading).toHaveText('It’s yours.');
    await expect(page).toHaveTitle('Order confirmed · Tutu Much');
  });

  test('a failure says the order is in Purchases', async ({ page }) => {
    await page.route('**/api/purchases/o-1', (route) => route.fulfill(problem(500, 'SERVER_ERROR', 'Server error')));
    const confirmation = new OrderConfirmationPage(page);
    await confirmation.goto();

    await expect(confirmation.heading).toHaveText('We couldn’t show your order');
    await expect(confirmation.centerState).toContainText('If you were charged, the order is in Purchases.');
    await expect(confirmation.centerState).toContainText('Ref 5c1d-0e7a');
    await expect(confirmation.centerState.getByRole('link', { name: 'Go to purchases' })).toHaveAttribute(
      'href',
      '/account/purchases',
    );
  });
});
