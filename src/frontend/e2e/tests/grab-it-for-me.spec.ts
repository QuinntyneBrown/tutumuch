// Acceptance Test
// Traces to: L2-117, L2-118, L2-119, L2-121
// Description: A verified member prices a grab for a US address, sees the shelf price, grab fee,
// tax, total and the non-refundable deposit, must tick the acknowledgement before paying, pays once
// with a test card and lands on the grab when the webhook has decided; a declined card keeps the
// address and the tick. The shopper cancels a grab with the refund the dialog states, and says
// "I've got it" once TuTuMuch bought it. The administrator accepts, buys, declines or reports a grab
// unavailable at /admin/grabs in the admin shell.

import { Page, Route } from '@playwright/test';

import {
  adminGrab,
  administrator,
  expect,
  findDetail,
  grabFixture,
  grabQuote,
  marcus,
  problem,
  signInAs,
  test,
  unverifiedMarcus,
} from '../fixtures';
import { AdminGrabsPage, GrabPage, MyGrabsPage } from '../pages/shelf-finds.page';

async function serveFind(page: Page, body: Record<string, unknown>): Promise<void> {
  await page.route('**/api/finds/F-1001', (route) => route.fulfill({ json: body }));
}

/** Answers GET /api/me/grabs/grab-1 with each body in turn, the last one from then on. */
async function serveGrab(page: Page, ...bodies: Record<string, unknown>[]): Promise<void> {
  let calls = 0;
  await page.route('**/api/me/grabs/grab-1', (route) => {
    if (route.request().method() !== 'GET') return route.fallback();
    const body = bodies[Math.min(calls++, bodies.length - 1)];
    return route.fulfill({ json: body });
  });
}

test.describe('grab it for me', () => {
  test.beforeEach(async ({ page }) => {
    await serveFind(page, findDetail());
    await page.route('**/api/grabs/quote', (route) => route.fulfill({ json: grabQuote }));
  });

  test('the member sees every amount, ticks the deposit box and pays once', async ({ page }) => {
    await signInAs(page, marcus);
    const starts: { body: Record<string, unknown>; key: string | null }[] = [];
    await page.route('**/api/grabs', (route: Route) => {
      starts.push({ body: route.request().postDataJSON(), key: route.request().headers()['idempotency-key'] ?? null });
      return route.fulfill({ status: 201, json: grabFixture({ status: 'PaymentPending' }) });
    });
    await serveGrab(page, grabFixture({ status: 'PaymentPending' }), grabFixture());
    const grab = new GrabPage(page);
    await grab.goto();

    await expect(grab.heading).toHaveText('Grab it for me');
    await expect(page.locator('main')).toContainText('TuTuMuch goes to Target, buys it and sends it to you.');
    await grab.fillBrooklyn();
    await grab.submit.click();

    await expect(grab.summary).toContainText('Shelf price');
    await expect(grab.summary).toContainText('$40.00');
    await expect(grab.summary).toContainText('Grab fee');
    await expect(grab.summary).toContainText('$8.00');
    await expect(grab.summary).toContainText('$4.26');
    await expect(grab.summary).toContainText('$52.26');
    await expect(grab.summary).toContainText('Non-refundable deposit: $8.00');
    await expect(grab.depositNote).toContainText(
      'If you cancel after TuTuMuch accepts, you get everything back except the $8.00 deposit.',
    );
    await expect(grab.submit).toHaveText('Pay $52.26');

    await grab.submit.click();
    await expect(grab.errorSummary).toContainText('Tick the box to confirm you understand the deposit');
    expect(starts).toHaveLength(0);

    await grab.acknowledge.check();
    await grab.submit.click();
    await expect(page).toHaveURL(/\/account\/grabs\/grab-1$/);
    expect(starts).toHaveLength(1);
    expect(starts[0].key).toMatch(/^[0-9a-f-]{36}$/);
    expect(starts[0].body).toMatchObject({
      findReference: 'F-1001',
      depositAcknowledged: true,
      paymentMethodToken: 'tok_visa',
      expectedTotal: 52.26,
    });
  });

  test('a declined card keeps the address and the tick', async ({ page }) => {
    await signInAs(page, marcus);
    await page.route('**/api/grabs', (route) =>
      route.fulfill({ status: 201, json: grabFixture({ status: 'PaymentPending' }) }),
    );
    await serveGrab(page, grabFixture({ status: 'PaymentFailed', paymentDeclineReason: 'Insufficient funds.' }));
    const grab = new GrabPage(page);
    await grab.goto();
    await grab.fillBrooklyn();
    await grab.submit.click();
    await grab.acknowledge.check();
    await grab.submit.click();

    await expect(grab.formAlert).toContainText('Your bank declined the card');
    await expect(grab.formAlert).toContainText('Insufficient funds.');
    await expect(grab.street).toHaveValue('214 Kent Ave');
    await expect(grab.acknowledge).toBeChecked();
  });

  test('a grab already in progress says so', async ({ page }) => {
    await signInAs(page, marcus);
    await page.unroute('**/api/finds/F-1001');
    await serveFind(page, findDetail({ grabOpen: true }));
    const grab = new GrabPage(page);
    await grab.goto();

    await expect(grab.centerState).toContainText('Someone already asked TuTuMuch to grab this one');
  });

  test('a non-US address is refused', async ({ page }) => {
    await signInAs(page, marcus);
    await page.unroute('**/api/grabs/quote');
    await page.route('**/api/grabs/quote', (route) =>
      route.fulfill(problem(422, 'NON_US_ADDRESS', 'TuTuMuch ships within the United States only')),
    );
    const grab = new GrabPage(page);
    await grab.goto();
    await grab.fillBrooklyn();
    await grab.submit.click();

    await expect(grab.formAlert).toContainText('TuTuMuch ships within the United States only');
  });

  test('an unverified member is asked to verify; a visitor signs in first', async ({ page }) => {
    await signInAs(page, unverifiedMarcus);
    const grab = new GrabPage(page);
    await grab.goto();
    await expect(grab.centerState).toContainText('Verify your email to ask for a grab');
  });

  test('a visitor is sent to sign in with the grab page as returnUrl', async ({ page }) => {
    const grab = new GrabPage(page);
    await grab.goto();

    await expect(page).toHaveURL(/\/sign-in\?returnUrl=%2Ffinds%2FF-1001%2Fgrab$/);
  });
});

test.describe('the shopper’s grabs', () => {
  test.beforeEach(async ({ page }) => {
    await signInAs(page, marcus);
  });

  test('the list shows each grab with its status', async ({ page }) => {
    await page.route('**/api/me/grabs', (route) =>
      route.fulfill({
        json: [
          {
            id: 'grab-1',
            reference: 'GR-1001',
            status: 'Requested',
            findReference: 'F-1001',
            findTitle: 'Glitter ballet flats, pink',
            storeName: 'Target',
            total: '52.26',
            deadline: '2026-10-27T12:00:00Z',
            createdAt: '2026-10-25T12:00:00Z',
          },
          {
            id: 'grab-2',
            reference: 'GR-1002',
            status: 'Unavailable',
            findReference: 'F-1002',
            findTitle: 'Soccer cleats',
            storeName: 'Play It Again Sports',
            total: '23.40',
            deadline: null,
            createdAt: '2026-10-24T12:00:00Z',
          },
        ],
      }),
    );
    const grabs = new MyGrabsPage(page);
    await grabs.goto();

    await expect(grabs.heading).toHaveText('Your grabs');
    await expect(grabs.rows).toHaveCount(2);
    await expect(grabs.rows.first()).toContainText('GR-1001 · Target · $52.26');
    await expect(grabs.rows.first()).toContainText('Requested');
    await expect(grabs.rows.nth(1)).toContainText('Couldn’t get it');
    await expect(grabs.rows.first().getByRole('link')).toHaveAttribute('href', '/account/grabs/grab-1');
  });

  test('no grabs yet points to shelf finds', async ({ page }) => {
    await page.route('**/api/me/grabs', (route) => route.fulfill({ json: [] }));
    const grabs = new MyGrabsPage(page);
    await grabs.goto();

    await expect(grabs.empty).toContainText('No grabs yet');
    await expect(grabs.empty.getByRole('link', { name: 'See shelf finds' })).toHaveAttribute('href', '/finds');
  });

  test('cancelling before acceptance refunds everything', async ({ page }) => {
    await serveGrab(page, grabFixture());
    await page.route('**/api/me/grabs/grab-1/cancel', (route) =>
      route.fulfill({ json: grabFixture({ status: 'Cancelled', refundIfCancelled: null, refundedAmount: '52.26' }) }),
    );
    const grabs = new MyGrabsPage(page);
    await grabs.goto('grab-1');

    await expect(grabs.status).toContainText('Waiting for TuTuMuch to accept (by Tue 27 Oct)');
    await expect(grabs.summary).toContainText('Non-refundable deposit: $8.00');
    await grabs.cancel.click();
    await expect(grabs.dialog).toContainText('TuTuMuch hasn’t accepted yet, so you get all $52.26 back.');
    await grabs.dialog.getByRole('button', { name: 'Cancel grab' }).click();
    await expect(grabs.status).toContainText("You cancelled. You've been refunded $52.26.");
    await expect(page.locator('.toast-region')).toContainText('$52.26 is on its way back to your card.');
    await expect(grabs.cancel).toHaveCount(0);
  });

  test('cancelling after acceptance states the deposit kept', async ({ page }) => {
    await serveGrab(
      page,
      grabFixture({ status: 'Accepted', refundIfCancelled: '44.26', buyBy: '2026-10-29T12:00:00Z' }),
    );
    const grabs = new MyGrabsPage(page);
    await grabs.goto('grab-1');

    await expect(grabs.status).toContainText('TuTuMuch is going to the store (by Thu 29 Oct)');
    await grabs.cancel.click();
    await expect(grabs.dialog).toContainText('You get $44.26 back. TuTuMuch keeps the $8.00 deposit.');
  });

  test('a bought grab is received, not cancelled', async ({ page }) => {
    await serveGrab(page, grabFixture({ status: 'Bought', refundIfCancelled: null }));
    await page.route('**/api/me/grabs/grab-1/received', (route) =>
      route.fulfill({ json: grabFixture({ status: 'Received', refundIfCancelled: null }) }),
    );
    const grabs = new MyGrabsPage(page);
    await grabs.goto('grab-1');

    await expect(grabs.status).toContainText('TuTuMuch has it and is sending it to you');
    await expect(grabs.cancel).toHaveCount(0);
    await grabs.received.click();
    await grabs.dialog.getByRole('button', { name: 'Yes, I’ve got it' }).click();
    await expect(grabs.status).toContainText('You’ve got it'.replace('’', "'"));
  });

  test('another member’s grab is not found', async ({ page }) => {
    await page.route('**/api/me/grabs/grab-9', (route) => route.fulfill(problem(404, 'GRAB_NOT_FOUND', 'Not found')));
    const grabs = new MyGrabsPage(page);
    await grabs.goto('grab-9');

    await expect(grabs.centerState).toContainText('We couldn’t find that grab');
  });
});

test.describe('TuTuMuch handles grabs', () => {
  test('the administrator accepts, then buys a grab', async ({ page }) => {
    await signInAs(page, administrator);
    await page.route('**/api/admin/grabs', (route) => route.fulfill({ json: [adminGrab()] }));
    await page.route('**/api/admin/grabs/grab-1/accept', (route) =>
      route.fulfill({
        json: adminGrab({
          status: 'Accepted',
          deadline: '2026-10-28T12:00:00Z',
          address: {
            fullName: 'Marcus Bell',
            line1: '214 Kent Ave',
            line2: null,
            city: 'Brooklyn',
            state: 'NY',
            zip: '11249',
          },
        }),
      }),
    );
    await page.route('**/api/admin/grabs/grab-1/bought', (route) =>
      route.fulfill({ json: adminGrab({ status: 'Bought', deadline: null }) }),
    );
    const admin = new AdminGrabsPage(page);
    await admin.goto();

    await expect(admin.heading).toHaveText('Grabs');
    await expect(admin.subtitle).toHaveText('1 open · 0 ended');
    await expect(admin.grabs.first()).toContainText('GR-1001 · Target · F-1001');
    await expect(admin.grabs.first()).toContainText('Answer by Tue 27 Oct');
    await expect(admin.grabs.first()).toContainText('marcus.bell@example.com');
    await expect(admin.grabs.first()).toContainText('Total $52.26 · deposit $8.00');
    await expect(admin.grabs.first()).not.toContainText('Ship to');
    await admin.grabs.first().getByRole('button', { name: 'Accept' }).click();

    await expect(admin.grabs.first()).toContainText('Buy by Wed 28 Oct');
    await expect(admin.grabs.first()).toContainText('Ship to');
    await expect(page.locator('.toast-region')).toContainText('Grab accepted');
    await admin.grabs.first().getByRole('button', { name: 'Bought it' }).click();
    await expect(admin.grabs.first()).toContainText('Bought');
    await expect(admin.grabs.first().getByRole('button')).toHaveCount(0);
  });

  test('declining asks first and refunds in full', async ({ page }) => {
    await signInAs(page, administrator);
    await page.route('**/api/admin/grabs', (route) => route.fulfill({ json: [adminGrab()] }));
    await page.route('**/api/admin/grabs/grab-1/decline', (route) =>
      route.fulfill({ json: adminGrab({ status: 'Declined', deadline: null, refundedAmount: '52.26' }) }),
    );
    const admin = new AdminGrabsPage(page);
    await admin.goto();

    await admin.grabs.first().getByRole('button', { name: 'Decline' }).click();
    await expect(admin.dialog).toContainText('marcus_12 is refunded $52.26 in full and emailed.');
    await admin.dialog.getByRole('button', { name: 'Decline and refund' }).click();
    await expect(admin.grabs.first()).toContainText('Declined');
    await expect(admin.grabs.first()).toContainText('Refunded $52.26');
  });

  test('couldn’t get it needs a reason', async ({ page }) => {
    await signInAs(page, administrator);
    await page.route('**/api/admin/grabs', (route) =>
      route.fulfill({ json: [adminGrab({ status: 'Accepted', deadline: '2026-10-28T12:00:00Z' })] }),
    );
    const bodies: unknown[] = [];
    await page.route('**/api/admin/grabs/grab-1/unavailable', (route) => {
      bodies.push(route.request().postDataJSON());
      return route.fulfill({
        json: adminGrab({
          status: 'Unavailable',
          deadline: null,
          refundedAmount: '52.26',
          unavailableReason: 'Price went up',
        }),
      });
    });
    const admin = new AdminGrabsPage(page);
    await admin.goto();

    await admin.grabs.first().getByRole('button', { name: 'Couldn’t get it' }).click();
    await admin.dialog.getByRole('button', { name: 'Refund in full' }).click();
    await expect(admin.dialog).toContainText('Choose a reason');
    await admin.dialog.getByLabel('Price went up').check();
    await admin.dialog.getByRole('button', { name: 'Refund in full' }).click();
    await expect(admin.grabs.first()).toContainText('Couldn’t get it');
    expect(bodies).toEqual([{ reason: 'PriceWentUp' }]);
  });

  test('a member who is not an administrator sees the forbidden state', async ({ page }) => {
    await signInAs(page, marcus);
    await page.route('**/api/admin/grabs', (route) =>
      route.fulfill(problem(403, 'ADMINISTRATOR_REQUIRED', 'Only TuTuMuch administrators can do that')),
    );
    const admin = new AdminGrabsPage(page);
    await admin.goto();

    await expect(admin.state).toContainText('You can’t open this');
  });

  test('no grabs yet', async ({ page }) => {
    await signInAs(page, administrator);
    await page.route('**/api/admin/grabs', (route) => route.fulfill({ json: [] }));
    const admin = new AdminGrabsPage(page);
    await admin.goto();

    await expect(admin.empty).toContainText('When a shopper asks TuTuMuch to grab a find, it shows up here.');
  });
});
