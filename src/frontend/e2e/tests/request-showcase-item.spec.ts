// Acceptance Test
// Traces to: L2-107, L2-108, L2-109, L2-110, L2-111
// Description: A showcase listing looks like any Active listing, but Buy now shows the Sold notice
// with "Request this item" and never calls checkout. A member sends or updates a request with an
// optional note in the dialog; a visitor signs in or joins and comes back to the listing with the
// dialog open, and only a same-site return URL is followed. A checkout opened for a showcase
// listing asks for no address or card. The owner turns the Showcase switch on their own listing
// and reads every request at /admin/requests in the admin shell.

import { Page, Route } from '@playwright/test';

import {
  administrator,
  adminShowcaseRequest,
  catalogPage,
  expect,
  listingDetail,
  marcus,
  problem,
  serveListing,
  showcaseRequest,
  signInAs,
  test,
} from '../fixtures';
import { JoinPage } from '../pages/join.page';
import { ListingDetailPage } from '../pages/listing-detail.page';
import { RequestShowcaseItemDialog } from '../pages/request-showcase-item.page';
import { ShowcaseRequestsPage } from '../pages/showcase-requests.page';
import { SignInPage } from '../pages/sign-in.page';

const showcase = listingDetail({ title: 'Gold sequin recital tutu', brand: 'Capezio', showcase: true });

/** Answers the member's request for L-10001: the request, or 404 when there is none. */
async function serveMyRequest(page: Page, request: Record<string, unknown> | null): Promise<void> {
  await page.route('**/api/me/showcase-requests/L-10001', (route) =>
    route.request().method() === 'GET'
      ? route.fulfill(request ? { json: request } : problem(404, 'SHOWCASE_REQUEST_NOT_FOUND', 'Not found'))
      : route.fallback(),
  );
}

/** Answers PUT /api/me/showcase-requests/L-10001 with each response in turn and records the bodies. */
async function serveSend(page: Page, ...responses: ((route: Route) => Promise<void>)[]): Promise<unknown[]> {
  const bodies: unknown[] = [];
  await page.route('**/api/me/showcase-requests/L-10001', async (route) => {
    if (route.request().method() !== 'PUT') return route.fallback();
    bodies.push(route.request().postDataJSON());
    const respond = responses[Math.min(bodies.length, responses.length) - 1];
    await respond(route);
  });
  return bodies;
}

const created = (note: string | null) => (route: Route) =>
  route.fulfill({ status: 201, json: showcaseRequest({ note, requestedAt: '2026-10-25T12:00:00Z' }) });
const updated = (note: string | null) => (route: Route) => route.fulfill({ json: showcaseRequest({ note }) });

test.describe('request a showcase item', () => {
  test('Buy now shows the Sold notice without checkout and a member sends a request', async ({
    page,
    unfixturedApiCalls,
  }) => {
    await signInAs(page, marcus);
    await serveListing(page, showcase);
    await serveMyRequest(page, null);
    const bodies = await serveSend(page, created('Size 7, for her spring recital.'));
    const checkouts: string[] = [];
    await page.route('**/api/checkouts', (route) => {
      checkouts.push(route.request().url());
      return route.fulfill(problem(409, 'LISTING_SHOWCASE', 'This one has already found a home'));
    });
    const detail = new ListingDetailPage(page);
    await detail.goto();

    await expect(detail.heading).toHaveText('Gold sequin recital tutu');
    await expect(detail.price).toContainText('$85.00');
    await expect(detail.stamp).toHaveCount(0);
    await expect(detail.soldNotice).toHaveCount(0);
    await detail.buyNowButton.click();

    await expect(page).toHaveURL('/items/L-10001');
    await expect(detail.soldNoticeHeading).toBeFocused();
    await expect(detail.soldNotice).toHaveAttribute('role', 'status');
    await expect(detail.soldNotice.locator('.photo__stamp')).toHaveText('Sold');
    await expect(detail.soldNotice).toContainText('We can look for another one like it. Tell us what you’re after.');
    await expect(detail.buyBar).toHaveCount(0);
    await expect(page.getByLabel('Street address')).toHaveCount(0);

    await detail.requestItem.click();
    const dialog = new RequestShowcaseItemDialog(page);
    await expect(dialog.dialog).toBeVisible();
    await expect(dialog.dialog).toContainText('Gold sequin recital tutu');
    await expect(dialog.note).toBeFocused();
    await dialog.note.fill('  Size 7, for her spring recital.  ');
    await expect(dialog.dialog).toContainText('35 / 500');
    await dialog.send.click();

    await expect(dialog.dialog).toHaveCount(0);
    await expect(detail.toast).toContainText('Request sent');
    await expect(detail.toast).toContainText('Thank you. TuTuMuch sees every request.');
    await expect(detail.soldNotice).toContainText('You asked for this on Oct 25.');
    await expect(detail.updateRequest).toBeVisible();
    expect(bodies).toEqual([{ note: 'Size 7, for her spring recital.' }]);
    expect(checkouts).toEqual([]);
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('an existing request is shown and sending again updates it', async ({ page, unfixturedApiCalls }) => {
    await signInAs(page, marcus);
    await serveListing(page, showcase);
    await serveMyRequest(page, showcaseRequest());
    const bodies = await serveSend(page, updated('Size 8 now.'));
    const detail = new ListingDetailPage(page);
    await detail.goto();

    await detail.buyNowButton.click();
    await expect(detail.soldNotice).toContainText('You asked for this on Oct 24. Add a note or change it any time.');
    await detail.updateRequest.click();
    const dialog = new RequestShowcaseItemDialog(page);
    await expect(dialog.requested).toHaveText('You asked for this on Oct 24. Sending again updates your request.');
    await expect(dialog.note).toHaveValue('Size 7 if you find one, for her spring recital.');
    await dialog.note.fill('Size 8 now.');
    await dialog.update.click();

    await expect(dialog.dialog).toHaveCount(0);
    await expect(detail.toast).toContainText('Request updated');
    expect(bodies).toEqual([{ note: 'Size 8 now.' }]);
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('a note over 500 characters is refused without a call, and an empty note is sent as none', async ({ page }) => {
    await signInAs(page, marcus);
    await serveListing(page, showcase);
    await serveMyRequest(page, null);
    const bodies = await serveSend(page, created(null));
    const detail = new ListingDetailPage(page);
    await detail.goto('L-10001', '?request=1');
    const dialog = new RequestShowcaseItemDialog(page);

    await dialog.note.fill('a'.repeat(501));
    await dialog.send.click();

    await expect(dialog.summary).toBeFocused();
    await expect(dialog.summary.getByRole('link', { name: 'Keep the note to 500 characters' })).toBeVisible();
    await expect(dialog.error).toHaveText('Keep the note to 500 characters');
    await expect(dialog.note).toHaveAttribute('aria-invalid', 'true');
    expect(bodies).toEqual([]);

    await dialog.note.fill('   ');
    await dialog.send.click();

    await expect(dialog.dialog).toHaveCount(0);
    expect(bodies).toEqual([{ note: null }]);
  });

  test('a failure keeps the note with a reference, and Try again sends it', async ({ page }) => {
    await signInAs(page, marcus);
    await serveListing(page, showcase);
    await serveMyRequest(page, null);
    const bodies = await serveSend(
      page,
      (route) => route.fulfill(problem(500, 'SERVER_ERROR', 'Something went wrong')),
      created('Size 7'),
    );
    const detail = new ListingDetailPage(page);
    await detail.goto('L-10001', '?request=1');
    const dialog = new RequestShowcaseItemDialog(page);

    await dialog.note.fill('Size 7');
    await dialog.send.click();

    await expect(dialog.alert).toContainText('Couldn’t send your request');
    await expect(dialog.alert).toContainText('Ref 5c1d-0e7a');
    await expect(dialog.note).toHaveValue('Size 7');
    await dialog.tryAgain.click();

    await expect(dialog.dialog).toHaveCount(0);
    await expect(detail.toast).toContainText('Request sent');
    expect(bodies).toHaveLength(2);
  });

  test('the 21st new request of the day is refused with a warning', async ({ page }) => {
    await signInAs(page, marcus);
    await serveListing(page, showcase);
    await serveMyRequest(page, null);
    await serveSend(page, (route) =>
      route.fulfill({
        ...problem(429, 'RATE_LIMITED', 'That’s 20 requests today'),
        headers: { 'Retry-After': '3600' },
      }),
    );
    await new ListingDetailPage(page).goto('L-10001', '?request=1');
    const dialog = new RequestShowcaseItemDialog(page);

    await dialog.send.click();

    await expect(dialog.alert).toContainText('That’s 20 requests today');
    await expect(dialog.alert).toContainText('Try again tomorrow. You can still change the requests you’ve sent.');
    await expect(dialog.dialog).toBeVisible();
  });

  test('a visitor signs in and comes back to the listing with the request open', async ({
    page,
    unfixturedApiCalls,
  }) => {
    await serveListing(page, listingDetail({ ...showcase, viewer: { relation: 'Anonymous' } }));
    const detail = new ListingDetailPage(page);
    await detail.goto();

    await detail.buyNowButton.click();
    await expect(page).toHaveURL('/items/L-10001');
    await expect(detail.soldNotice).toBeVisible();
    await detail.requestItem.click();

    await expect(page).toHaveURL('/sign-in?returnUrl=%2Fitems%2FL-10001%3Frequest%3D1');
    const signIn = new SignInPage(page);
    await expect(signIn.heading).toHaveText('Sign in to request this item');
    await expect(page.locator('main')).toContainText('You’ll go straight back to the item and your request.');
    await expect(page.getByRole('link', { name: 'Join' })).toHaveAttribute(
      'href',
      '/join?returnUrl=%2Fitems%2FL-10001%3Frequest%3D1',
    );
    await serveListing(page, showcase);
    await serveMyRequest(page, null);
    await page.route('**/api/auth/sign-in', async (route) => {
      await signInAs(page, marcus);
      await route.fulfill({ json: marcus });
    });
    await signIn.signIn('marcus.bell@example.com', 'correct horse battery');

    await expect(page).toHaveURL('/items/L-10001?request=1');
    await expect(detail.soldNotice).toBeVisible();
    await expect(new RequestShowcaseItemDialog(page).dialog).toBeVisible();
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('joining keeps the return URL and Google carries it', async ({ page }) => {
    await page.route('**/api/auth/providers', (route) => route.fulfill({ json: { google: true } }));
    await page.route('**/api/auth/register', (route) => route.fulfill({ status: 201 }));
    await page.goto('/join?returnUrl=%2Fitems%2FL-10001%3Frequest%3D1');
    const join = new JoinPage(page);

    await expect(page.locator('main')).toContainText(
      'Join, verify your email, then sign in and we’ll take you back to your request.',
    );
    await expect(join.google).toHaveAttribute('href', /returnUrl=%2Fitems%2FL-10001%3Frequest%3D1$/);
    await expect(page.locator('main').getByRole('link', { name: 'Sign in' })).toHaveAttribute(
      'href',
      '/sign-in?returnUrl=%2Fitems%2FL-10001%3Frequest%3D1',
    );
    await join.fill({ displayName: 'marcus_12', email: 'marcus.bell@example.com', password: 'correct horse battery' });
    await join.submit.click();

    await expect(join.heading).toHaveText('Check your email');
    await expect(page.getByRole('link', { name: 'Sign in to send your request' })).toHaveAttribute(
      'href',
      '/sign-in?returnUrl=%2Fitems%2FL-10001%3Frequest%3D1',
    );
  });

  test('only a same-site return URL is followed after signing in', async ({ page }) => {
    await page.route('**/api/catalog/listings?*', (route) => route.fulfill({ json: catalogPage(0) }));
    await page.route('**/api/auth/sign-in', async (route) => {
      await signInAs(page, marcus);
      await route.fulfill({ json: marcus });
    });
    const signIn = new SignInPage(page);

    for (const unsafe of ['//evil.example/items', '/\\evil.example', 'https://evil.example']) {
      await signIn.goto(unsafe);
      await expect(signIn.heading).toHaveText('Sign in');
      await signIn.signIn('marcus.bell@example.com', 'correct horse battery');
      await expect(page).toHaveURL('/');
    }
  });

  test('a checkout opened for a showcase listing asks for no address or card', async ({ page }) => {
    await signInAs(page, marcus);
    await page.route('**/api/checkouts', (route) =>
      route.fulfill(problem(409, 'LISTING_SHOWCASE', 'This one has already found a home')),
    );
    await page.goto('/checkout/L-10001');

    await expect(page.getByRole('heading', { level: 1 })).toHaveText('This one has already found a home');
    await expect(page.locator('main')).toContainText('We can look for another one like it.');
    await expect(page.getByRole('link', { name: 'Request this item' })).toHaveAttribute(
      'href',
      '/items/L-10001?request=1',
    );
    await expect(page.getByLabel('Street address')).toHaveCount(0);
    await expect(page.getByLabel('Card')).toHaveCount(0);
  });

  test('an ordinary listing keeps Buy now as the link to checkout', async ({ page }) => {
    await signInAs(page, marcus);
    await serveListing(page, listingDetail());
    const detail = new ListingDetailPage(page);
    await detail.goto();

    await expect(detail.buyNow).toHaveAttribute('href', '/checkout/L-10001');
    await expect(detail.buyNowButton).toHaveCount(0);
  });
});

test.describe('request a showcase item: owner', () => {
  test('the owner turns the Showcase switch on their own listing and sees the requests link', async ({
    page,
    unfixturedApiCalls,
  }) => {
    await signInAs(page, administrator);
    await serveListing(page, listingDetail({ viewer: { relation: 'Seller' } }));
    const bodies: unknown[] = [];
    await page.route('**/api/admin/listings/L-10001/showcase', async (route) => {
      bodies.push(route.request().postDataJSON());
      await route.fulfill({ json: { reference: 'L-10001', showcase: true } });
    });
    const detail = new ListingDetailPage(page);
    await detail.goto();

    await expect(detail.editListing).toBeVisible();
    await expect(detail.showcaseSwitch).not.toBeChecked();
    await expect(detail.seeRequests).toHaveCount(0);
    await detail.showcaseSwitch.check();

    await expect(detail.showcaseSwitch).toBeChecked();
    await expect(detail.seeRequests).toHaveAttribute('href', '/admin/requests');
    expect(bodies).toEqual([{ showcase: true }]);
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('a failed switch goes back with a danger toast', async ({ page }) => {
    await signInAs(page, administrator);
    await serveListing(page, listingDetail({ viewer: { relation: 'Seller' }, showcase: true }));
    let release: () => void = () => undefined;
    const held = new Promise<void>((resolve) => (release = resolve));
    await page.route('**/api/admin/listings/L-10001/showcase', async (route) => {
      await held;
      await route.fulfill(problem(500, 'SERVER_ERROR', 'Something went wrong'));
    });
    const detail = new ListingDetailPage(page);
    await detail.goto();

    await expect(detail.buyNowButton).toHaveCount(0);
    await expect(detail.showcaseSwitch).toBeChecked();
    await detail.showcaseSwitch.uncheck();
    await expect(detail.showcaseSwitch).not.toBeChecked();
    release();

    await expect(detail.toast).toContainText('Couldn’t change the showcase');
    await expect(detail.showcaseSwitch).toBeChecked();
  });

  test('a seller without the Administrator role gets no switch', async ({ page }) => {
    await signInAs(page, marcus);
    await serveListing(page, listingDetail({ viewer: { relation: 'Seller' } }));
    const detail = new ListingDetailPage(page);
    await detail.goto();

    await expect(detail.editListing).toBeVisible();
    await expect(detail.showcaseSwitch).toHaveCount(0);
  });

  test('the owner reads every request in the admin shell', async ({ page, unfixturedApiCalls }) => {
    await signInAs(page, administrator);
    let release: () => void = () => undefined;
    const held = new Promise<void>((resolve) => (release = resolve));
    await page.route('**/api/admin/showcase-requests', async (route) => {
      await held;
      await route.fulfill({
        json: [
          adminShowcaseRequest(),
          adminShowcaseRequest({
            memberDisplayName: 'pia_reyes',
            memberEmail: 'pia.reyes@example.com',
            note: null,
            requestedAt: '2026-10-24T18:40:00Z',
          }),
          adminShowcaseRequest({
            memberDisplayName: 'rainbow_rosa',
            memberEmail: 'rosa.kim@example.com',
            note: 'Any colour but pink, please. <b>Thanks</b>',
            requestedAt: '2026-10-23T10:00:00Z',
          }),
        ],
      });
    });
    const list = new ShowcaseRequestsPage(page);
    await list.goto();

    await expect(list.loading).toBeVisible();
    await expect(page.locator('.topbar__admin')).toHaveText('Admin');
    release();
    await expect(list.heading).toHaveText('Showcase requests');
    await expect(list.subtitle).toHaveText('3 requests');
    await expect(list.requests).toHaveCount(3);
    const first = list.requests.first();
    await expect(first.getByRole('link', { name: 'Gold sequin recital tutu' })).toHaveAttribute(
      'href',
      '/items/L-10001',
    );
    await expect(first).toContainText('L-10001');
    await expect(first).toContainText('tutu_tessa');
    await expect(first.getByRole('link', { name: 'tessa.bell@example.com' })).toHaveAttribute(
      'href',
      'mailto:tessa.bell@example.com',
    );
    await expect(first).toContainText('Size 7 if you find one, for her spring recital.');
    await expect(list.requests.nth(1)).toContainText('No note');
    await expect(list.requests.nth(2)).toContainText('Any colour but pink, please. <b>Thanks</b>');
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('no requests yet', async ({ page }) => {
    await signInAs(page, administrator);
    await page.route('**/api/admin/showcase-requests', (route) => route.fulfill({ json: [] }));
    const list = new ShowcaseRequestsPage(page);
    await list.goto();

    await expect(list.empty).toContainText('No requests yet');
    await expect(list.empty).toContainText('When a shopper asks for a showcase item, it shows up here.');
  });

  test('a failed load shows the reference and Try again loads the list', async ({ page }) => {
    await signInAs(page, administrator);
    let calls = 0;
    await page.route('**/api/admin/showcase-requests', (route) =>
      ++calls === 1
        ? route.fulfill(problem(500, 'SERVER_ERROR', 'Something went wrong'))
        : route.fulfill({ json: [adminShowcaseRequest()] }),
    );
    const list = new ShowcaseRequestsPage(page);
    await list.goto();

    await expect(list.empty).toContainText('Couldn’t load requests');
    await expect(list.empty).toContainText('Ref 5c1d-0e7a');
    await list.tryAgain.click();

    await expect(list.requests).toHaveCount(1);
  });

  test('a member without the Administrator role cannot open the list', async ({ page }) => {
    await signInAs(page, marcus);
    await page.route('**/api/admin/showcase-requests', (route) =>
      route.fulfill(problem(403, 'ADMINISTRATOR_REQUIRED', 'Administrators only')),
    );
    const list = new ShowcaseRequestsPage(page);
    await list.goto();

    await expect(list.heading).toHaveText('You can’t open this');
    await expect(list.state).toContainText('Only TuTuMuch’s administrators can see requests.');
  });

  test('a visitor is sent to sign in and back', async ({ page }) => {
    await page.route('**/api/admin/showcase-requests', (route) =>
      route.fulfill(problem(401, 'UNAUTHENTICATED', 'Sign in to continue')),
    );
    await new ShowcaseRequestsPage(page).goto();

    await expect(page).toHaveURL('/sign-in?returnUrl=%2Fadmin%2Frequests');
  });

  test('on a phone each request takes the full width', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 800 });
    await signInAs(page, administrator);
    await page.route('**/api/admin/showcase-requests', (route) => route.fulfill({ json: [adminShowcaseRequest()] }));
    const list = new ShowcaseRequestsPage(page);
    await list.goto();

    const block = (await list.requests.first().boundingBox())!;
    expect(block.width).toBeGreaterThan(375 - 2 * 24 - 2);
  });
});
