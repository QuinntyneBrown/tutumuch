// Acceptance Test
// Traces to: L2-036, L2-010
// Description: The buyer of a Delivered order reports "Wrong size" with a description and a photo
// in the Report a problem dialog within the 3-day window; missing answers are shown without a call
// and a failure keeps everything. The order page then shows the open problem with the thread, and
// after a refund decision "Refunded in full". The seller sees the problem with the payout on hold
// and replies once. An administrator opens the problem in the admin shell and refunds the buyer or
// releases the payout with a reason; a missing reason is refused.

import { Page, Route } from '@playwright/test';

import {
  buyerOrder,
  expect,
  imageFile,
  marcus,
  orderProblem,
  problem,
  problemDetail,
  sale,
  signInAs,
  test,
  timeline,
  uspsTracking,
} from '../fixtures';
import { OrderDetailPage } from '../pages/order-detail.page';
import { ProblemDetailPage, ProblemSection, ReportProblemDialog } from '../pages/order-problems.page';
import { SaleDetailPage } from '../pages/sale-detail.page';

const PIXEL = Buffer.from(
  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR4nGNgYGD4DwABBAEAwS2OUAAAAABJRU5ErkJggg==',
  'base64',
);

const delivered = {
  status: 'Delivered',
  timeline: timeline('2026-10-20T15:00:00Z', '2026-10-24T15:00:00Z'),
  tracking: uspsTracking,
  autoCompletesAt: '2026-10-27T15:00:00Z',
  canReportProblem: true,
  problemDeadline: '2026-10-27T15:00:00Z',
};

async function servePhotos(page: Page): Promise<void> {
  await page.route('**/photos/**', (route) => route.fulfill({ contentType: 'image/png', body: PIXEL }));
  await page.route('**/api/orders/*/problem/photos/*', (route) =>
    route.fulfill({ contentType: 'image/png', body: PIXEL }),
  );
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

test.describe('resolve order problems: buyer', () => {
  test.beforeEach(async ({ page }) => {
    await signInAs(page, marcus);
    await servePhotos(page);
  });

  test('the buyer reports wrong size with a photo and sees the open problem', async ({ page, unfixturedApiCalls }) => {
    let reported = false;
    let body = '';
    await page.route('**/api/purchases/o-1', (route) =>
      route.fulfill({
        json: buyerOrder(reported ? { ...delivered, canReportProblem: false, problem: orderProblem() } : delivered),
      }),
    );
    await page.route('**/api/orders/o-1/problem', (route) => {
      body = route.request().postDataBuffer()?.toString('latin1') ?? '';
      reported = true;
      return route.fulfill({ status: 201, json: orderProblem() });
    });
    const order = new OrderDetailPage(page);
    const section = new ProblemSection(page);
    const dialog = new ReportProblemDialog(page);
    await order.goto();

    await section.reportProblem.click();

    await expect(dialog.root).toContainText(
      'Open until Tue 27 Oct. We hold the seller’s payout while our team looks into it.',
    );
    await expect(dialog.reasons).toHaveCount(4);
    await dialog.send.click();
    await expect(dialog.summary).toContainText('Choose what’s wrong');
    await expect(dialog.summary).toContainText('Tell us what happened');
    expect(reported).toBe(false);

    await dialog.reason('Wrong size').check({ force: true });
    await dialog.description.fill('Label inside says 11.5, not 12.');
    await dialog.photoInput.setInputFiles(imageFile('label.jpg'));
    await expect(dialog.photos).toHaveCount(1);
    await expect(dialog.root.locator('.uploader__cover')).toHaveCount(0);
    await dialog.send.click();

    await expect(dialog.root).toBeHidden();
    expect(body).toContain('name="reason"\r\n\r\nWrongSize');
    expect(body).toContain('Label inside says 11.5, not 12.');
    expect(body).toContain('filename="label.jpg"');
    await expect(order.heading).toHaveText('We’re on it');
    await expect(section.alert).toContainText('Problem open · Wrong size');
    await expect(section.alert).toContainText('We’re holding the seller’s payout until our team decides.');
    await expect(section.entries.first()).toContainText('You · Sat 24 Oct');
    await expect(section.entries.first().locator('img')).toHaveCount(1);
    await expect(order.confirmReceipt).toHaveCount(0);
    await expect(section.reportProblem).toHaveCount(0);
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('while sending Cancel is disabled, and a failure keeps the report for another try', async ({ page }) => {
    await page.route('**/api/purchases/o-1', (route) => route.fulfill({ json: buyerOrder(delivered) }));
    const release = await holdThenFail(page, '**/api/orders/o-1/problem');
    const order = new OrderDetailPage(page);
    const section = new ProblemSection(page);
    const dialog = new ReportProblemDialog(page);
    await order.goto();
    await section.reportProblem.click();
    await dialog.reason('Not received').check({ force: true });
    await dialog.description.fill('Tracking has not moved.');

    await dialog.send.click();

    await expect(dialog.send).toHaveAttribute('aria-busy', 'true');
    await expect(dialog.cancel).toBeDisabled();
    release();
    await expect(dialog.alert).toContainText('Your photos and description are saved here. Try again.');
    await expect(dialog.send).toHaveText('Try again');
    await expect(dialog.description).toHaveValue('Tracking has not moved.');
    await expect(dialog.reason('Not received')).toBeChecked();
  });

  test('outside the window the option is not offered', async ({ page }) => {
    await page.route('**/api/purchases/o-1', (route) =>
      route.fulfill({ json: buyerOrder({ ...delivered, canReportProblem: false, problemDeadline: null }) }),
    );
    const order = new OrderDetailPage(page);
    await order.goto();

    await expect(order.confirmReceipt).toBeVisible();
    await expect(new ProblemSection(page).reportProblem).toHaveCount(0);
  });

  test('a problem decided for the buyer shows the refund', async ({ page }) => {
    await page.route('**/api/purchases/o-1', (route) =>
      route.fulfill({
        json: buyerOrder({
          status: 'Refunded',
          problem: orderProblem({
            status: 'Resolved',
            outcome: 'FullRefund',
            resolutionReason: 'Wrong size sent.',
            resolvedAt: '2026-10-24T20:00:00Z',
          }),
        }),
      }),
    );
    const order = new OrderDetailPage(page);
    await order.goto();

    await expect(order.heading).toHaveText('Refunded');
    await expect(order.alert).toContainText('Refunded in full');
    await expect(order.alert).toContainText(
      'Our team reviewed the problem. $102.34 is on its way back to Visa ending 4242.',
    );
    await expect(page.locator('main')).toContainText('Resolved Sat 24 Oct by Tutu Much support.');
    await expect(page.getByRole('link', { name: 'Back to purchases' })).toBeVisible();
  });
});

test.describe('resolve order problems: seller', () => {
  test.beforeEach(async ({ page }) => {
    await signInAs(page, marcus);
    await servePhotos(page);
  });

  test('the seller sees the problem with the payout on hold and replies once', async ({ page, unfixturedApiCalls }) => {
    const open = sale({
      status: 'Delivered',
      timeline: timeline('2026-10-20T15:00:00Z', '2026-10-24T15:00:00Z'),
      problem: orderProblem(),
    });
    const sent: unknown[] = [];
    await page.route('**/api/sales/o-1', (route) => route.fulfill({ json: open }));
    await page.route('**/api/orders/o-1/problem/response', (route) => {
      sent.push(route.request().postDataJSON());
      return route.fulfill({
        json: {
          ...open,
          problem: orderProblem({
            sellerResponse: 'My mistake. Happy to take them back.',
            sellerRespondedAt: '2026-10-25T10:00:00Z',
          }),
        },
      });
    });
    const salePage = new SaleDetailPage(page);
    const section = new ProblemSection(page);
    await salePage.goto();

    await expect(salePage.heading).toHaveText('Problem reported');
    await expect(section.alert).toContainText('Problem reported · Wrong size');
    await expect(section.alert).toContainText('Your payout is on hold while our team reviews this.');
    await expect(section.entries).toHaveCount(1);
    await expect(section.entries.first()).toContainText('twirly_tara · Sat 24 Oct');
    await expect(page.locator('main')).toContainText('Visible to the buyer and to Tutu Much.');

    await section.sendReply.click();
    await expect(page.locator('main .field__error')).toHaveText('Write your reply');
    expect(sent).toEqual([]);
    await section.reply.fill('My mistake. Happy to take them back.');
    await section.sendReply.click();

    await expect(section.entries).toHaveCount(2);
    await expect(section.entries.nth(1)).toContainText('You · Sun 25 Oct');
    await expect(section.reply).toHaveCount(0);
    expect(sent).toEqual([{ message: 'My mistake. Happy to take them back.' }]);
    expect(unfixturedApiCalls).toEqual([]);
  });
});

test.describe('resolve order problems: administrator', () => {
  test.beforeEach(async ({ page }) => {
    await signInAs(page, { ...marcus, displayName: 'ana_ruiz' });
    await servePhotos(page);
  });

  test('an administrator refunds the buyer in full with a reason', async ({ page, unfixturedApiCalls }) => {
    const sent: unknown[] = [];
    await page.route('**/api/admin/problems/P-1001', (route) => route.fulfill({ json: problemDetail() }));
    await page.route('**/api/admin/problems/P-1001/resolution', (route) => {
      sent.push(route.request().postDataJSON());
      const detail = problemDetail();
      return route.fulfill({
        json: {
          ...detail,
          problem: {
            ...(detail['problem'] as object),
            status: 'Resolved',
            outcome: 'FullRefund',
            resolvedAt: '2026-10-26T15:05:00Z',
            resolutionReason: 'Hole not disclosed.',
          },
          order: { ...(detail['order'] as object), status: 'Refunded' },
          payout: 'Cancelled',
        },
      });
    });
    const admin = new ProblemDetailPage(page);
    await admin.goto();

    await expect(admin.topBar).toContainText('Admin');
    await expect(admin.footer).toHaveCount(0);
    await expect(admin.heading).toHaveText('Not as described');
    await expect(admin.subtitle).toHaveText('Order problem P-1001 · opened Sat 24 Oct');
    await expect(admin.thread.first()).toContainText('marcus_12 · buyer · Sat 24 Oct');
    await expect(admin.thread.nth(1)).toContainText('vintage_lou · seller · Sun 25 Oct');
    await expect(admin.photos).toHaveCount(1);
    await expect(admin.panel).toContainText('Order TM-48140');
    await expect(admin.facts).toContainText('Delivered');
    await expect(admin.facts).toContainText('$59.88');
    await expect(admin.facts).toContainText('Held');

    await admin.resolve.click();
    await expect(admin.dialog).toContainText('Resolve problem P-1001');
    await expect(admin.dialog).toContainText('$59.88 to marcus_12 · no payout');
    await expect(admin.dialog).toContainText('Order completes · vintage_lou is paid');
    await expect(admin.refund).toBeChecked();
    await admin.confirm.click();
    await expect(admin.reasonError).toHaveText('Add a reason. Both members see it.');
    expect(sent).toEqual([]);
    await admin.reason.fill('Hole not disclosed.');
    await expect(admin.confirm).toHaveText('Refund buyer');
    await admin.confirm.click();

    await expect(admin.dialog).toBeHidden();
    expect(sent).toEqual([{ outcome: 'FullRefund', reason: 'Hole not disclosed.' }]);
    await expect(admin.alert).toContainText('Refunded in full');
    await expect(admin.alert).toContainText('$59.88 back to the buyer. No payout.');
    await expect(admin.facts).toContainText('Refunded');
    await expect(admin.facts).toContainText('Not paid');
    await expect(admin.resolve).toHaveCount(0);
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('releasing the payout sends ReleaseToSeller and a busy confirm', async ({ page }) => {
    const sent: unknown[] = [];
    await page.route('**/api/admin/problems/P-1001', (route) => route.fulfill({ json: problemDetail() }));
    const release = await holdThenFail(page, '**/api/admin/problems/P-1001/resolution');
    await page.route('**/api/admin/problems/P-1001/resolution', async (route) => {
      sent.push(route.request().postDataJSON());
      await route.fallback();
    });
    const admin = new ProblemDetailPage(page);
    await admin.goto();
    await admin.resolve.click();

    await admin.release.check();
    await admin.reason.fill('Photos show the right size.');
    await expect(admin.confirm).toHaveText('Release payout');
    await admin.confirm.click();

    await expect(admin.confirm).toHaveAttribute('aria-busy', 'true');
    await expect(admin.cancel).toBeDisabled();
    release();
    await expect(admin.dialog.getByRole('alert')).toContainText('That didn’t go through');
    expect(sent).toEqual([{ outcome: 'ReleaseToSeller', reason: 'Photos show the right size.' }]);
  });

  test('a member without the role sees the load error', async ({ page }) => {
    await page.route('**/api/admin/problems/P-1001', (route) =>
      route.fulfill(problem(403, 'ADMINISTRATOR_REQUIRED', 'Only Tutu Much administrators can do that')),
    );
    const admin = new ProblemDetailPage(page);
    await admin.goto();

    await expect(admin.error).toContainText('Couldn’t load this problem');
    await expect(admin.error).toContainText('Ref 5c1d-0e7a');
    await expect(admin.resolve).toHaveCount(0);
  });
});
