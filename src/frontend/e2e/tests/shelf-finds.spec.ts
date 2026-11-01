// Acceptance Test
// Traces to: L2-113, L2-114, L2-115, L2-116, L2-121
// Description: Anyone browses shelf finds from the top bar, 24 at a time, and opens one with the
// store, its address and an "Open in maps" link; a find that offers a grab shows the box, a gone
// one the Gone notice and a withdrawn one the not-found state. A member who may sell posts a find;
// only an administrator sees "Offer to grab it". The poster marks their find gone or withdraws it.

import { Page } from '@playwright/test';

import {
  administrator,
  expect,
  findCard,
  findDetail,
  marcus,
  problem,
  serveCatalog,
  serveLaunchPhase,
  signInAs,
  test,
} from '../fixtures';
import { FindDetailPage, PostFindPage, ShelfFindsPage } from '../pages/shelf-finds.page';

/** Answers GET /api/finds with pages of `total` cards. */
async function serveFinds(
  page: Page,
  total: number,
  fields: (n: number) => Record<string, unknown> = () => ({}),
): Promise<void> {
  await page.route(/\/api\/finds\?/, (route) => {
    const p = Number(new URL(route.request().url()).searchParams.get('page') ?? 1);
    const items = Array.from({ length: Math.max(0, Math.min(24, total - (p - 1) * 24)) }, (_, i) => {
      const n = (p - 1) * 24 + i + 1;
      return findCard(n, fields(n));
    });
    return route.fulfill({ json: { items, page: p, pageSize: 24, totalCount: total } });
  });
}

async function serveFind(page: Page, body: Record<string, unknown> | null): Promise<void> {
  await page.route('**/api/finds/F-1001', (route) =>
    route.request().method() === 'GET'
      ? route.fulfill(body ? { json: body } : problem(404, 'FIND_NOT_FOUND', 'Not found'))
      : route.fallback(),
  );
}

test.describe('shelf finds', () => {
  test.use({ viewport: { width: 1280, height: 900 } });

  test('a visitor opens Shelf finds from the top bar and loads the next page', async ({ page }) => {
    await serveCatalog(page, 4);
    await serveFinds(page, 30, (n) => (n === 1 ? { grabAvailable: true, title: 'Glitter ballet flats, pink' } : {}));
    await page.goto('/');
    await page.getByRole('navigation', { name: 'Primary' }).getByRole('link', { name: 'Shelf finds' }).click();
    const finds = new ShelfFindsPage(page);

    await expect(finds.heading).toHaveText('Shelf finds');
    await expect(finds.cards).toHaveCount(24);
    await expect(finds.cards.first()).toContainText('Glitter ballet flats, pink');
    await expect(finds.cards.first()).toContainText('$24.99');
    await expect(finds.cards.first()).toContainText('Target');
    await expect(finds.cards.first()).toContainText('Seen Sat 24 Oct');
    await expect(finds.cards.first()).toContainText('Grab available');
    await expect(finds.cards.nth(1)).not.toContainText('Grab available');
    await expect(finds.post).toHaveCount(0);
    await finds.loadMore.click();
    await expect(finds.cards).toHaveCount(30);
    await expect(
      page.getByRole('navigation', { name: 'Primary' }).getByRole('link', { name: 'Shelf finds' }),
    ).toHaveAttribute('aria-current', 'page');
  });

  test('the footer links to Shelf finds and an empty list says so', async ({ page }) => {
    await serveFinds(page, 0);
    await serveCatalog(page, 4);
    await page.goto('/');
    await page.getByRole('contentinfo').getByRole('link', { name: 'Shelf finds' }).click();
    const finds = new ShelfFindsPage(page);

    await expect(finds.empty).toContainText('No shelf finds yet');
    await expect(finds.empty).toContainText('When someone spots something great in a store, it shows up here.');
  });

  test('a failed load shows the reference and Try again loads the list', async ({ page }) => {
    let calls = 0;
    await page.route(/\/api\/finds\?/, (route) => {
      calls++;
      return calls === 1
        ? route.fulfill(problem(500, 'INTERNAL_ERROR', 'Something went wrong'))
        : route.fulfill({ json: { items: [findCard(1)], page: 1, pageSize: 24, totalCount: 1 } });
    });
    const finds = new ShelfFindsPage(page);
    await finds.goto();

    await expect(finds.empty).toContainText('Couldn’t load shelf finds');
    await finds.tryAgain.click();
    await expect(finds.cards).toHaveCount(1);
  });

  test('one card per row under 576px', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 800 });
    await serveFinds(page, 4);
    const finds = new ShelfFindsPage(page);
    await finds.goto();

    await expect(finds.cards).toHaveCount(4);
    const [first, second] = await Promise.all([finds.cards.nth(0).boundingBox(), finds.cards.nth(1).boundingBox()]);
    expect(second!.y).toBeGreaterThan(first!.y);
    expect(Math.round(second!.x)).toBe(Math.round(first!.x));
  });

  test('a member who may sell sees Post a shelf find', async ({ page }) => {
    await signInAs(page, marcus);
    await serveFinds(page, 2);
    const finds = new ShelfFindsPage(page);
    await finds.goto();

    await expect(finds.post).toHaveAttribute('href', '/finds/new');
  });
});

test.describe('shelf find detail', () => {
  test('a visitor sees the store, the maps link, the notice and the grab box', async ({ page }) => {
    await serveFind(page, findDetail());
    const find = new FindDetailPage(page);
    await find.goto();

    await expect(find.heading).toHaveText('Glitter ballet flats, pink');
    await expect(find.price).toContainText('$40.00');
    await expect(page.locator('main')).toContainText('88 Atlantic Ave, Brooklyn, NY 11201');
    await expect(page.locator('main')).toContainText('Seen in store on Sat 24 Oct');
    await expect(find.maps).toHaveAttribute(
      'href',
      'https://www.google.com/maps/search/?api=1&query=Target%2C%2088%20Atlantic%20Ave%2C%20Brooklyn%2C%20NY%2011201',
    );
    await expect(find.maps).toHaveAttribute('target', '_blank');
    await expect(find.maps).toHaveAttribute('rel', 'noopener noreferrer');
    await expect(find.maps).toContainText('(opens in a new tab)');
    await expect(find.notice).toContainText('Prices and stock change. Check with the store before you go.');
    await expect(find.grabBox).toContainText('TuTuMuch buys it at the store and sends it to you for a $8.00 grab fee.');
    await expect(find.grabButton).toHaveAttribute('href', '/sign-in?returnUrl=%2Ffinds%2FF-1001%2Fgrab');
    await expect(page.locator('main')).toContainText('Clearance end cap by the shoes.');
  });

  test('a signed-in member goes straight to the grab page; an open grab hides the button', async ({ page }) => {
    await signInAs(page, marcus);
    await serveFind(page, findDetail());
    const find = new FindDetailPage(page);
    await find.goto();
    await expect(find.grabButton).toHaveAttribute('href', '/finds/F-1001/grab');

    await page.unroute('**/api/finds/F-1001');
    await serveFind(page, findDetail({ grabOpen: true }));
    await page.reload();
    await expect(find.grabBox).toContainText('Someone already asked TuTuMuch to grab this one');
    await expect(find.grabButton).toHaveCount(0);
  });

  test('a find without a grab offer has no grab box', async ({ page }) => {
    await serveFind(page, findDetail({ grabFee: null }));
    const find = new FindDetailPage(page);
    await find.goto();

    await expect(find.heading).toBeVisible();
    await expect(find.grabBox).toHaveCount(0);
  });

  test('a gone find shows the Gone notice and an expired one says it expired', async ({ page }) => {
    await serveFind(page, findDetail({ status: 'Gone' }));
    const find = new FindDetailPage(page);
    await find.goto();

    await expect(find.soldNotice).toContainText('Gone');
    await expect(find.soldNotice).toContainText('This find is gone');
    await expect(find.soldNotice.getByRole('link', { name: 'See more shelf finds' })).toHaveAttribute('href', '/finds');
    await expect(find.grabBox).toHaveCount(0);

    await page.unroute('**/api/finds/F-1001');
    await serveFind(page, findDetail({ status: 'Expired' }));
    await page.reload();
    await expect(find.soldNotice).toContainText('This find has expired');
  });

  test('a withdrawn or unknown find is not found', async ({ page }) => {
    await serveFind(page, null);
    const find = new FindDetailPage(page);
    await find.goto();

    await expect(find.centerState).toContainText('We couldn’t find that');
  });

  test('a note with markup is shown as text', async ({ page }) => {
    await serveFind(page, findDetail({ note: '<script>alert(1)</script>' }));
    const find = new FindDetailPage(page);
    await find.goto();

    await expect(page.locator('main')).toContainText('<script>alert(1)</script>');
  });

  test('the poster marks their find gone', async ({ page }) => {
    await signInAs(page, marcus);
    await serveFind(page, findDetail({ mine: true, grabFee: null, posterDisplayName: 'marcus_12' }));
    await page.route('**/api/finds/F-1001/gone', (route) =>
      route.fulfill({ json: findDetail({ mine: true, grabFee: null, status: 'Gone' }) }),
    );
    const find = new FindDetailPage(page);
    await find.goto();

    await expect(find.grabBox).toHaveCount(0);
    await find.markGone.click();
    await expect(find.dialog).toContainText('Gone from the shelf? Shoppers will see it’s gone.');
    await find.dialog.getByRole('button', { name: 'Mark as gone' }).click();
    await expect(find.soldNotice).toContainText('This find is gone');
    await expect(page.locator('.toast')).toContainText('Marked as gone');
  });

  test('a find with an open grab cannot be withdrawn', async ({ page }) => {
    await signInAs(page, administrator);
    await serveFind(page, findDetail({ mine: true, grabOpen: true }));
    await page.route('**/api/finds/F-1001', (route) =>
      route.request().method() === 'DELETE'
        ? route.fulfill(problem(409, 'GRAB_IN_PROGRESS', 'TuTuMuch is grabbing this one for a shopper'))
        : route.fallback(),
    );
    const find = new FindDetailPage(page);
    await find.goto();

    await find.withdraw.click();
    await find.dialog.getByRole('button', { name: 'Withdraw' }).click();
    await expect(find.dialog).toContainText('TuTuMuch is grabbing this one for a shopper');
  });

  test('the poster withdraws their find and lands on Shelf finds', async ({ page }) => {
    await signInAs(page, marcus);
    await serveFind(page, findDetail({ mine: true, grabFee: null }));
    await page.route('**/api/finds/F-1001', (route) =>
      route.request().method() === 'DELETE' ? route.fulfill({ status: 204 }) : route.fallback(),
    );
    await serveFinds(page, 0);
    const find = new FindDetailPage(page);
    await find.goto();

    await find.withdraw.click();
    await find.dialog.getByRole('button', { name: 'Withdraw' }).click();
    await expect(page).toHaveURL(/\/finds$/);
    await expect(page.locator('.toast')).toContainText('Find withdrawn');
  });
});

test.describe('post a shelf find', () => {
  test('a member posts a find and lands on it', async ({ page }) => {
    await signInAs(page, marcus);
    await serveFind(page, findDetail({ mine: true, grabFee: null }));
    const bodies: Record<string, unknown>[] = [];
    await page.route('**/api/finds', (route) => {
      if (route.request().method() !== 'POST') return route.fallback();
      bodies.push(route.request().postDataJSON());
      return route.fulfill({ status: 201, json: findDetail({ mine: true, grabFee: null }) });
    });
    const form = new PostFindPage(page);
    await form.goto();

    await expect(form.heading).toHaveText('Post a shelf find');
    await expect(form.offerGrab).toHaveCount(0);
    await expect(page.locator('main')).toContainText('Never your home.');
    await form.title.fill('Glitter ballet flats, pink');
    await form.category.selectOption('Dance');
    await form.price.fill('40');
    await form.store.fill('Target');
    await form.address.fill('88 Atlantic Ave, Brooklyn, NY 11201');
    await form.note.fill('Clearance end cap by the shoes.');
    await form.submit.click();

    await expect(page).toHaveURL(/\/finds\/F-1001$/);
    await expect(page.locator('.toast')).toContainText('Your find is live');
    expect(bodies[0]).toMatchObject({
      title: 'Glitter ballet flats, pink',
      category: 'Dance',
      shelfPrice: 40,
      storeName: 'Target',
      storeAddress: '88 Atlantic Ave, Brooklyn, NY 11201',
      grabFee: null,
    });
  });

  test('an administrator offers a grab with a fee', async ({ page }) => {
    await signInAs(page, administrator);
    await serveFind(page, findDetail({ mine: true }));
    const bodies: Record<string, unknown>[] = [];
    await page.route('**/api/finds', (route) => {
      if (route.request().method() !== 'POST') return route.fallback();
      bodies.push(route.request().postDataJSON());
      return route.fulfill({ status: 201, json: findDetail({ mine: true }) });
    });
    const form = new PostFindPage(page);
    await form.goto();

    await form.title.fill('Glitter ballet flats, pink');
    await form.category.selectOption('Dance');
    await form.price.fill('40.00');
    await form.store.fill('Target');
    await form.address.fill('88 Atlantic Ave, Brooklyn, NY 11201');
    await form.offerGrab.click();
    await form.grabFee.fill('8');
    await form.submit.click();

    await expect(page).toHaveURL(/\/finds\/F-1001$/);
    expect(bodies[0]).toMatchObject({ grabFee: 8 });
  });

  test('field errors from the API show in the summary', async ({ page }) => {
    await signInAs(page, marcus);
    await page.route('**/api/finds', (route) =>
      route.request().method() === 'POST'
        ? route.fulfill(
            problem(422, 'FIND_INVALID', 'Check the highlighted fields', {
              title: { code: 'TITLE_LENGTH', message: 'Title must be 5 to 80 characters' },
              seenOn: { code: 'SEEN_ON_OUT_OF_RANGE', message: 'Choose a day in the last 7 days' },
            }),
          )
        : route.fallback(),
    );
    const form = new PostFindPage(page);
    await form.goto();
    await form.submit.click();

    await expect(form.errorSummary).toContainText('Title must be 5 to 80 characters');
    await expect(form.errorSummary).toContainText('Choose a day in the last 7 days');
  });

  test('the 21st find of the day shows the limit', async ({ page }) => {
    await signInAs(page, marcus);
    await page.route('**/api/finds', (route) =>
      route.request().method() === 'POST'
        ? route.fulfill(problem(429, 'RATE_LIMITED', "That's 20 finds today"))
        : route.fallback(),
    );
    const form = new PostFindPage(page);
    await form.goto();
    await form.submit.click();

    await expect(form.centerState).toContainText('That’s 20 finds today');
  });

  test('in phase 1 a member sees Selling opens soon', async ({ page }) => {
    await serveLaunchPhase(page, 1);
    await signInAs(page, marcus);
    await page.goto('/finds/new');

    await expect(page.locator('main .center-state')).toContainText('Selling opens soon');
  });

  test('a visitor is sent to sign in and back', async ({ page }) => {
    await page.goto('/finds/new');

    await expect(page).toHaveURL(/\/sign-in\?returnUrl=%2Ffinds%2Fnew$/);
  });
});
