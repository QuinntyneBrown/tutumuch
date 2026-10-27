// Acceptance Test
// Traces to: L2-097, L2-098, L2-099, L2-102, L2-103, L2-104, L2-105
// Description: An empty search or category page offers "Can't find it? Request it", which opens
// the request form filled in from the page, through sign-in for a visitor. The form has no size
// input, saves with the API's field errors and limit, and My requests lists each request with its
// Active matches, no match yet or no longer available, Expired with Renew, Edit and Delete, the
// limit line and its empty, loading and error states. The match email's unsubscribe link names
// item request emails. Every TuTuMuch API call is a page.route fixture.

import { Page, Request } from '@playwright/test';

import {
  catalogCard,
  expect,
  itemRequest,
  marcus,
  myItemRequests,
  problem,
  serveSearch,
  signInAs,
  test,
} from '../fixtures';
import { MyRequestsPage } from '../pages/my-requests.page';
import { RequestFormPage } from '../pages/request-form.page';
import { SignInPage } from '../pages/sign-in.page';

const slippers = itemRequest({
  matchCount: 1,
  matches: [catalogCard(3, { reference: 'L-10861', title: 'Pink leather ballet slippers', price: '14.00' })],
});
const smartwatch = itemRequest({
  id: '7d3c0f2e-0000-4000-8000-000000000002',
  category: 'Cameras and tech',
  keywords: 'kids smartwatch',
  maxPrice: null,
  minimumCondition: null,
  createdAt: '2026-10-12T12:00:00Z',
  expiresAt: '2027-01-10T12:00:00Z',
});
const scooter = itemRequest({
  id: '7d3c0f2e-0000-4000-8000-000000000003',
  category: 'Sports',
  keywords: 'scooter',
  maxPrice: 50,
  minimumCondition: null,
  createdAt: '2026-10-01T12:00:00Z',
  expiresAt: '2026-12-30T12:00:00Z',
  matchCount: 1,
});
const goggles = itemRequest({
  id: '7d3c0f2e-0000-4000-8000-000000000004',
  category: 'Sports',
  keywords: 'swim goggles',
  maxPrice: null,
  minimumCondition: null,
  expiresAt: '2026-10-18T12:00:00Z',
  status: 'Expired',
});

/** Answers GET /api/me/item-requests with `list`, and records every other call to the requests API. */
async function serveRequests(page: Page, list: Record<string, unknown>): Promise<Request[]> {
  const calls: Request[] = [];
  await page.route('**/api/me/item-requests**', async (route) => {
    const request = route.request();
    if (request.method() === 'GET' && new URL(request.url()).pathname.endsWith('/item-requests')) {
      await route.fulfill({ json: list });
      return;
    }
    calls.push(request);
    await route.fulfill({ json: itemRequest(request.postDataJSON() ?? {}) });
  });
  return calls;
}

test.describe('request items', () => {
  test('an empty search offers Request it, and a visitor signs in and comes back to the filled-in form', async ({
    page,
    unfixturedApiCalls,
  }) => {
    await serveSearch(page, 0);
    await page.goto('/?q=ballet%20slippers');

    const request = page.locator('main .empty').getByRole('link', { name: 'Can’t find it? Request it' });
    await expect(request).toHaveAttribute('href', '/account/requests/new?q=ballet%20slippers');
    await request.click();

    await expect(page).toHaveURL('/sign-in?returnUrl=%2Faccount%2Frequests%2Fnew%3Fq%3Dballet%2520slippers');
    await page.route('**/api/auth/sign-in', async (route) => {
      await signInAs(page, marcus);
      await route.fulfill({ json: marcus });
    });
    await new SignInPage(page).signIn('marcus.bell@example.com', 'correct horse battery');

    const form = new RequestFormPage(page);
    await expect(page).toHaveURL('/account/requests/new?q=ballet%20slippers');
    await expect(form.heading).toHaveText('Request an item');
    await expect(form.keywords).toHaveValue('ballet slippers');
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('an empty category page offers Request it with the category chosen and the size fixed', async ({ page }) => {
    await signInAs(page, marcus);
    await serveSearch(page, 0);
    await page.goto('/dance');

    const empty = page.locator('main .empty');
    await expect(empty.getByRole('heading', { level: 2 })).toHaveText('Nothing in Dance right now');
    await expect(empty.getByRole('link', { name: 'Shop the closet' })).toBeVisible();
    await empty.getByRole('link', { name: 'Can’t find it? Request it' }).click();

    const form = new RequestFormPage(page);
    await expect(page).toHaveURL('/account/requests/new?category=dance');
    await expect(form.category).toHaveValue('Dance');
    await expect(form.size).toContainText('Size: Girls');
    await expect(form.keywords).toHaveValue('');
  });

  test('a catalog with results offers no Request it link', async ({ page }) => {
    await serveSearch(page, 3);
    await page.goto('/?q=tutu');

    await expect(page.locator('main').getByRole('link', { name: 'Can’t find it? Request it' })).toHaveCount(0);
  });

  test('a member saves a request: no size input, and My requests confirms it', async ({ page }) => {
    await signInAs(page, marcus);
    const calls = await serveRequests(page, myItemRequests([slippers]));
    const form = new RequestFormPage(page);
    await form.goto();

    await expect(form.size).toHaveCount(0);
    await form.category.selectOption('Dance');
    await expect(form.size).toContainText('Size: Girls');
    await expect(page.locator('main input#size, main select#size, main [name="size"]')).toHaveCount(0);
    await form.keywords.fill('ballet slippers');
    await expect(page.locator('main .field__counter')).toHaveText('15 / 100');
    await form.maxPrice.fill('20');
    await form.condition.selectOption('Used - Good');
    await form.save.click();

    await expect(page).toHaveURL('/account/requests');
    expect(calls.map((call) => call.method())).toEqual(['POST']);
    expect(calls[0].postDataJSON()).toEqual({
      category: 'Dance',
      keywords: 'ballet slippers',
      maxPrice: 20,
      minimumCondition: 'Used - Good',
    });
    const requests = new MyRequestsPage(page);
    await expect(requests.toast).toContainText('Request saved');
    await expect(requests.request('ballet slippers')).toBeVisible();
  });

  test('the API’s field errors show in a summary and inline, and nothing is lost', async ({ page }) => {
    await signInAs(page, marcus);
    await page.route('**/api/me/item-requests', (route) =>
      route.fulfill(
        problem(422, 'CATEGORY_INVALID', 'Check the highlighted fields', {
          category: { code: 'CATEGORY_INVALID', message: 'Choose a category' },
          keywords: { code: 'REQUEST_KEYWORDS_LENGTH', message: 'Describe the item in 3 to 100 characters' },
          maxPrice: { code: 'PRICE_OUT_OF_RANGE', message: 'Enter a price from $1.00 to $5,000.00' },
        }),
      ),
    );
    const form = new RequestFormPage(page);
    await form.goto();
    await form.keywords.fill('sl');
    await form.maxPrice.fill('0.99');
    await form.save.click();

    await expect(form.summary).toBeFocused();
    await expect(form.summary.getByRole('link')).toHaveText([
      'Choose a category',
      'Describe the item in 3 to 100 characters',
      'Enter a price from $1.00 to $5,000.00',
    ]);
    await expect(form.error('keywords')).toHaveText('Describe the item in 3 to 100 characters');
    await expect(form.keywords).toHaveAttribute('aria-invalid', 'true');
    await expect(form.keywords).toHaveValue('sl');
    await expect(page).toHaveURL('/account/requests/new');
  });

  test('a highest price that is not an amount is refused before sending', async ({ page, unfixturedApiCalls }) => {
    await signInAs(page, marcus);
    const form = new RequestFormPage(page);
    await form.goto('?category=dance&q=tutu');
    await form.maxPrice.fill('twenty');
    await form.save.click();

    await expect(form.error('max')).toHaveText('Enter a price from $1.00 to $5,000.00');
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('saving a 21st open request shows the limit instead of the form', async ({ page }) => {
    await signInAs(page, marcus);
    await page.route('**/api/me/item-requests', (route) =>
      route.fulfill(problem(409, 'REQUEST_LIMIT_REACHED', 'You have 20 open requests')),
    );
    const form = new RequestFormPage(page);
    await form.goto('?category=dance&q=leotard');
    await form.save.click();

    await expect(form.heading).toHaveText('You have 20 open requests');
    await expect(form.state).toContainText('Delete one to add another. Expired requests don’t count.');
    await expect(form.state.getByRole('link', { name: 'Your requests' })).toHaveAttribute('href', '/account/requests');
  });

  test('My requests lists each request with its matches, no match yet or a match no longer available', async ({
    page,
    unfixturedApiCalls,
  }) => {
    await signInAs(page, marcus);
    await serveRequests(page, myItemRequests([slippers, smartwatch, scooter]));
    await page.route('**/photos/**', (route) => route.fulfill({ status: 404 }));
    const requests = new MyRequestsPage(page);
    await requests.goto();

    await expect(requests.heading).toHaveText('Requests');
    await expect(requests.subtitle).toHaveText('3 open requests');
    await expect(requests.requestItem).toHaveAttribute('href', '/account/requests/new');
    await expect(requests.requests.getByRole('heading', { level: 2 })).toHaveText([
      'ballet slippers',
      'kids smartwatch',
      'scooter',
    ]);
    const first = requests.request('ballet slippers');
    await expect(first).toContainText('Dance · Girls’ · Up to $20.00 · Used - Good or better');
    await expect(first).toContainText('Open until Jan 18');
    await expect(first).toContainText('1 match');
    await expect(first.getByRole('link', { name: /Pink leather ballet slippers/ })).toHaveAttribute(
      'href',
      '/items/L-10861',
    );
    await expect(first.getByRole('link', { name: 'Edit ballet slippers' })).toHaveAttribute(
      'href',
      '/account/requests/7d3c0f2e-0000-4000-8000-000000000001/edit',
    );
    await expect(requests.request('kids smartwatch')).toContainText('Cameras and tech · Girls’');
    await expect(requests.request('kids smartwatch')).toContainText(
      'No matches yet. We’ll email you when one is listed.',
    );
    await expect(requests.request('scooter')).toContainText('Your match is no longer available. We’ll keep looking.');
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('an expired request shows Expired and Renew, and renewing at the limit warns', async ({ page }) => {
    await signInAs(page, marcus);
    let renewals = 0;
    await page.route('**/api/me/item-requests**', async (route) => {
      if (route.request().method() === 'PUT') {
        renewals++;
        await route.fulfill(
          renewals === 1
            ? problem(409, 'REQUEST_LIMIT_REACHED', 'You have 20 open requests')
            : { json: itemRequest({ ...goggles, status: 'Open', expiresAt: '2027-01-23T12:00:00Z' }) },
        );
        return;
      }
      await route.fulfill({
        json: myItemRequests(
          renewals > 1 ? [{ ...goggles, status: 'Open', expiresAt: '2027-01-23T12:00:00Z' }] : [goggles],
          renewals > 1 ? 1 : 0,
        ),
      });
    });
    const requests = new MyRequestsPage(page);
    await requests.goto();

    const expired = requests.request('swim goggles');
    await expect(expired.locator('.status')).toHaveText('Expired');
    await expect(expired).toContainText('Expired on Oct 18');
    await expect(expired).toContainText('Renew it to keep looking for 90 more days.');
    await expect(expired.getByRole('link', { name: 'Edit swim goggles' })).toHaveCount(0);

    await expired.getByRole('button', { name: 'Renew swim goggles' }).click();
    await expect(requests.toast).toContainText('You have 20 open requests');
    await expect(requests.toast).toContainText('Delete one to renew this one.');

    await expired.getByRole('button', { name: 'Renew swim goggles' }).click();
    await expect(requests.request('swim goggles')).toContainText('Open until Jan 23');
  });

  test('with 20 open requests the action gives way to the limit line', async ({ page }) => {
    await signInAs(page, marcus);
    await serveRequests(page, myItemRequests([slippers], 20));
    const requests = new MyRequestsPage(page);
    await requests.goto();

    await expect(requests.limitLine).toHaveText('You have 20 open requests. Delete one to add another.');
    await expect(requests.requestItem).toHaveCount(0);
  });

  test('editing loads the request, saves it and returns to My requests', async ({ page }) => {
    await signInAs(page, marcus);
    const sent: Request[] = [];
    await page.route('**/api/me/item-requests/*', async (route) => {
      if (route.request().method() === 'PUT') sent.push(route.request());
      await route.fulfill({ json: slippers });
    });
    await page.route('**/api/me/item-requests', (route) => route.fulfill({ json: myItemRequests([slippers]) }));
    const form = new RequestFormPage(page);
    await form.gotoEdit('7d3c0f2e-0000-4000-8000-000000000001');

    await expect(form.heading).toHaveText('Edit request');
    await expect(form.category).toHaveValue('Dance');
    await expect(form.keywords).toHaveValue('ballet slippers');
    await expect(form.maxPrice).toHaveValue('20.00');
    await expect(form.condition).toHaveValue('Used - Good');
    await form.keywords.fill('pink ballet slippers');
    await form.save.click();

    await expect(page).toHaveURL('/account/requests');
    expect(sent[0].postDataJSON()).toEqual({
      category: 'Dance',
      keywords: 'pink ballet slippers',
      maxPrice: 20,
      minimumCondition: 'Used - Good',
    });
    await expect(new MyRequestsPage(page).toast).toContainText('Request saved');
  });

  test('another member’s request, or a deleted one, is not here', async ({ page }) => {
    await signInAs(page, marcus);
    await page.route('**/api/me/item-requests/*', (route) =>
      route.fulfill(problem(404, 'REQUEST_NOT_FOUND', 'Not found')),
    );
    const form = new RequestFormPage(page);
    await form.gotoEdit('7d3c0f2e-0000-4000-8000-000000000009');

    await expect(form.heading).toHaveText('This request isn’t here');
    await expect(form.state.getByRole('link', { name: 'Your requests' })).toBeVisible();
  });

  test('deleting asks first, keeps the request on Keep request, and removes it with a toast', async ({ page }) => {
    await signInAs(page, marcus);
    let deleted = false;
    await page.route('**/api/me/item-requests**', async (route) => {
      if (route.request().method() === 'DELETE') {
        deleted = true;
        await route.fulfill({ status: 204 });
        return;
      }
      await route.fulfill({ json: myItemRequests(deleted ? [smartwatch] : [slippers, smartwatch]) });
    });
    const requests = new MyRequestsPage(page);
    await requests.goto();

    await requests.request('ballet slippers').getByRole('button', { name: 'Delete ballet slippers' }).click();
    await expect(requests.dialog.getByRole('heading', { name: 'Delete this request?' })).toBeVisible();
    await expect(requests.dialog).toContainText('“ballet slippers” stops looking for new listings.');
    await expect(requests.dialog.getByRole('button', { name: 'Keep request' })).toBeFocused();
    await requests.dialog.getByRole('button', { name: 'Keep request' }).click();
    await expect(requests.dialog).toHaveCount(0);
    expect(deleted).toBe(false);

    await requests.request('ballet slippers').getByRole('button', { name: 'Delete ballet slippers' }).click();
    await requests.dialog.getByRole('button', { name: 'Delete', exact: true }).click();

    await expect(requests.toast).toContainText('Request deleted');
    await expect(requests.request('ballet slippers')).toHaveCount(0);
    await expect(requests.subtitle).toHaveText('1 open request');
  });

  test('a failed delete keeps the request and offers Try again', async ({ page }) => {
    await signInAs(page, marcus);
    await page.route('**/api/me/item-requests**', (route) =>
      route.request().method() === 'DELETE'
        ? route.fulfill(problem(500, 'SERVER_ERROR', 'Server error'))
        : route.fulfill({ json: myItemRequests([slippers]) }),
    );
    const requests = new MyRequestsPage(page);
    await requests.goto();
    await requests.request('ballet slippers').getByRole('button', { name: 'Delete ballet slippers' }).click();
    await requests.dialog.getByRole('button', { name: 'Delete', exact: true }).click();

    await expect(requests.dialog).toContainText('Couldn’t delete');
    await expect(requests.dialog).toContainText('Your request is still open.');
    await expect(requests.dialog.getByRole('button', { name: 'Try again' })).toBeVisible();
  });

  test('no requests yet shows Tilly and one action', async ({ page }) => {
    await signInAs(page, marcus);
    await serveRequests(page, myItemRequests([]));
    const requests = new MyRequestsPage(page);
    await requests.goto();

    await expect(requests.empty.getByRole('heading', { level: 2 })).toHaveText('No requests yet');
    await expect(requests.empty).toContainText(
      'Ask for something that isn’t in the closet yet. We’ll email you when it’s listed.',
    );
    await expect(requests.empty.getByRole('img', { name: /^Tilly/ })).toBeVisible();
    await expect(requests.empty.getByRole('link', { name: 'Request an item' })).toHaveAttribute(
      'href',
      '/account/requests/new',
    );
    await expect(requests.subtitle).toHaveCount(0);
  });

  test('loading shows skeleton requests and a failure offers Try again', async ({ page }) => {
    await signInAs(page, marcus);
    let release: () => void = () => undefined;
    const held = new Promise<void>((resolve) => (release = resolve));
    let calls = 0;
    await page.route('**/api/me/item-requests', async (route) => {
      calls++;
      if (calls === 1) {
        await held;
        await route.fulfill(problem(500, 'SERVER_ERROR', 'Server error'));
        return;
      }
      await route.fulfill({ json: myItemRequests([smartwatch]) });
    });
    const requests = new MyRequestsPage(page);
    await requests.goto();

    await expect(requests.loading).toHaveAttribute('aria-label', 'Loading requests');
    release();
    await expect(requests.empty).toContainText('Couldn’t load your requests');
    await expect(requests.empty).toContainText('Ref 5c1d-0e7a');
    await requests.tryAgain.click();
    await expect(requests.request('kids smartwatch')).toBeVisible();
  });

  test('a visitor opening My requests is sent to sign in and back', async ({ page }) => {
    const requests = new MyRequestsPage(page);
    await requests.goto();

    await expect(page).toHaveURL('/sign-in?returnUrl=%2Faccount%2Frequests');
  });

  test('on a phone the form and the requests take the full width in one column', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 800 });
    await signInAs(page, marcus);
    await serveRequests(page, myItemRequests([smartwatch]));
    const form = new RequestFormPage(page);
    await form.goto('?category=dance');

    const price = (await form.maxPrice.boundingBox())!;
    const condition = (await form.condition.boundingBox())!;
    expect(condition.y).toBeGreaterThan(price.y);
    const keywords = (await form.keywords.boundingBox())!;
    expect(keywords.width).toBeGreaterThan(375 - 2 * 24 - 2);

    const requests = new MyRequestsPage(page);
    await requests.goto();
    const block = (await requests.request('kids smartwatch').boundingBox())!;
    expect(block.width).toBeGreaterThan(375 - 2 * 24 - 2);
  });

  test('the item request unsubscribe link says which emails stopped', async ({ page }) => {
    await page.route('**/api/notifications/unsubscribe', (route) =>
      route.fulfill({ json: { category: 'ItemRequests', unsubscribed: true } }),
    );
    await page.goto('/emails/unsubscribe?token=abc.def');

    await expect(page.getByRole('heading', { level: 1 })).toHaveText('You’re unsubscribed');
    await expect(page.locator('main')).toContainText('You’re unsubscribed from item request emails.');
  });
});
