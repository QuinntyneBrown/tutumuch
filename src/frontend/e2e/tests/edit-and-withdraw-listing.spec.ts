// Acceptance Test
// Traces to: L2-013, L2-020, L2-035
// Description: The seller edits an Active listing at /items/{reference}/edit: the form opens
// prefilled, saves with the ETag it read in If-Match, and a stale save is refused and reloads
// instead of overwriting. A Reserved listing is locked while a buyer is in checkout, and a sold or
// withdrawn listing says why it cannot be edited. The seller withdraws a listing from its detail
// page after a confirmation, and "Edit first" in the Relist dialog opens the edit page.

import { Page, Route } from '@playwright/test';

import {
  expect,
  listingDetail,
  marcus,
  myListing,
  photo,
  problem,
  sale,
  serveCatalogue,
  serveListing,
  signInAs,
  test,
} from '../fixtures';
import { RelistDialog } from '../pages/cancel-order.page';
import { EditListingPage, WithdrawListingDialog } from '../pages/edit-listing.page';
import { ListingDetailPage } from '../pages/listing-detail.page';
import { SaleDetailPage } from '../pages/sale-detail.page';

const arizona = myListing({
  status: 'Active',
  category: 'Shoes',
  subcategory: 'Sandals',
  title: 'Arizona sandals',
  brand: 'Birkenstock',
  condition: 'Used - Fair',
  flaws: 'Footbed shows wear at the heel.',
  description: 'Comfortable and broken in. Regular width.',
  price: 45,
  shippingCost: 8,
  publishedAt: '2026-10-25T12:00:00Z',
  removalReason: null,
  photos: [photo('p-1'), photo('p-2'), photo('p-3')],
});

/** Answers GET /api/me/listings/L-10001 with each body in turn (the last one repeats) and the ETag "3", "4"... */
async function serveMine(page: Page, ...bodies: Record<string, unknown>[]): Promise<void> {
  let read = 0;
  await page.route('**/api/me/listings/L-10001', (route) => {
    const body = bodies[Math.min(read, bodies.length - 1)];
    read++;
    return route.fulfill({ json: body, headers: { ETag: `"${2 + read}"` } });
  });
}

async function openEdit(page: Page, ...bodies: Record<string, unknown>[]): Promise<EditListingPage> {
  await serveMine(page, ...(bodies.length ? bodies : [arizona]));
  const edit = new EditListingPage(page);
  await edit.goto();
  return edit;
}

/** Records each PUT /api/listings/L-10001 with its If-Match header and answers with <answer>. */
async function recordPuts(
  page: Page,
  answer: (route: Route) => Promise<void>,
): Promise<{ body: Record<string, unknown>; ifMatch: string | undefined }[]> {
  const sent: { body: Record<string, unknown>; ifMatch: string | undefined }[] = [];
  await page.route('**/api/listings/L-10001', (route) => {
    if (route.request().method() !== 'PUT') return route.fallback();
    sent.push({ body: route.request().postDataJSON(), ifMatch: route.request().headers()['if-match'] });
    return answer(route);
  });
  return sent;
}

test.describe('edit listing', () => {
  test.beforeEach(async ({ page }) => {
    await signInAs(page, marcus);
    await serveCatalogue(page);
    await page.route('**/photos/**', (route) => route.fulfill({ contentType: 'image/png', body: Buffer.alloc(0) }));
  });

  test('the form opens with every field of the active listing', async ({ page, unfixturedApiCalls }) => {
    const edit = await openEdit(page);

    await expect(edit.heading).toHaveText('Edit listing');
    await expect(edit.listingLink).toHaveText('Arizona sandals');
    await expect(edit.listingLink).toHaveAttribute('href', '/items/L-10001');
    await expect(edit.status).toHaveText('Active');
    await expect(edit.form.category).toHaveValue('Shoes');
    await expect(edit.form.type).toHaveValue('Sandals');
    await expect(edit.form.title).toHaveValue('Arizona sandals');
    await expect(edit.form.flaws).toHaveValue('Footbed shows wear at the heel.');
    await expect(edit.form.price).toHaveValue('45.00');
    await expect(edit.form.photoTiles).toHaveCount(3);
    await expect(edit.form.confirm).toBeChecked();
    await expect(edit.cancel).toHaveAttribute('href', '/items/L-10001');
    await expect(edit.form.publish).toHaveCount(0);
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('saving sends the ETag it read and shows the saved state', async ({ page }) => {
    const sent = await recordPuts(page, (route) =>
      route.fulfill({ json: { ...arizona, price: 40 }, headers: { ETag: '"4"' } }),
    );
    const edit = await openEdit(page);

    await edit.form.price.fill('40');
    await edit.saveChanges.click();

    await expect(edit.savedAlert).toBeVisible();
    await expect(edit.savedAlert.getByRole('link', { name: 'View listing' })).toHaveAttribute('href', '/items/L-10001');
    expect(sent).toHaveLength(1);
    expect(sent[0].ifMatch).toBe('"3"');
    expect(sent[0].body).toMatchObject({ price: 40, sizeConfirmed: true, photoIds: ['p-1', 'p-2', 'p-3'] });
    expect(sent[0].body).not.toHaveProperty('size');
    expect(sent[0].body).not.toHaveProperty('status');

    await edit.form.price.fill('39');
    await edit.saveChanges.click();
    await expect.poll(() => sent.length).toBe(2);
    expect(sent[1].ifMatch).toBe('"4"');
  });

  test('a stale save is refused and Reload shows the latest version', async ({ page }) => {
    const sent = await recordPuts(page, (route) =>
      route.fulfill(problem(412, 'LISTING_CHANGED', 'This listing changed since you opened it')),
    );
    const edit = await openEdit(page, arizona, { ...arizona, price: 50 });

    await edit.form.price.fill('40');
    await edit.saveChanges.click();

    await expect(edit.changedAlert).toContainText('Reload to see the latest version, then make your changes again.');
    expect(sent).toHaveLength(1);
    await edit.reload.click();
    await expect(edit.form.price).toHaveValue('50.00');
    await expect(edit.changedAlert).toHaveCount(0);
  });

  test('client checks stop an invalid save before it is sent', async ({ page }) => {
    const sent = await recordPuts(page, (route) => route.fulfill({ json: arizona }));
    const edit = await openEdit(page);

    await edit.form.flaws.fill('');
    await edit.form.price.fill('5200');
    await edit.saveChanges.click();

    await expect(edit.form.errorSummary).toContainText("Describe the item's flaws");
    await expect(edit.form.errorSummary).toContainText('Enter a price from $1.00 to $5,000.00');
    expect(sent).toHaveLength(0);
  });

  test('a reserved listing is locked while a buyer is in checkout', async ({ page }) => {
    const edit = await openEdit(page, { ...arizona, status: 'Reserved' });

    await expect(edit.heading).toHaveText('This item is in someone’s checkout');
    await expect(edit.centerState).toContainText('You can edit it again if they don’t finish within 10 minutes.');
    await expect(page.getByRole('link', { name: 'Back to listing' })).toHaveAttribute('href', '/items/L-10001');
    await expect(edit.form.title).toHaveCount(0);
  });

  test('a save refused because a checkout reserved the listing shows the locked state', async ({ page }) => {
    await recordPuts(page, (route) =>
      route.fulfill(problem(409, 'LISTING_RESERVED', "This item is in someone's checkout")),
    );
    const edit = await openEdit(page);

    await edit.form.price.fill('40');
    await edit.saveChanges.click();

    await expect(edit.heading).toHaveText('This item is in someone’s checkout');
  });

  test('a sold listing says it can no longer be edited', async ({ page }) => {
    const edit = await openEdit(page, { ...arizona, status: 'Sold' });

    await expect(edit.heading).toHaveText('This item has sold');
    await expect(edit.centerState).toContainText('Sold listings can’t be edited.');
    await expect(page.getByRole('link', { name: 'Your sales' })).toHaveAttribute('href', '/account/sales');
  });

  test('a withdrawn listing says it is gone for good', async ({ page }) => {
    const edit = await openEdit(page, { ...arizona, status: 'Removed', removalReason: 'Withdrawn' });

    await expect(edit.heading).toHaveText('This listing was withdrawn');
    await expect(edit.centerState).toContainText('Withdrawn listings can’t be edited or relisted.');
    await expect(page.getByRole('link', { name: 'Your listings' })).toHaveAttribute('href', '/account/listings');
  });

  test('a failed load offers another try', async ({ page }) => {
    let calls = 0;
    await page.route('**/api/me/listings/L-10001', (route) => {
      calls++;
      return calls === 1
        ? route.fulfill(problem(500, 'SERVER_ERROR', 'Something went wrong'))
        : route.fulfill({ json: arizona, headers: { ETag: '"3"' } });
    });
    const edit = new EditListingPage(page);
    await edit.goto();

    await expect(edit.heading).toHaveText('Couldn’t load this listing');
    await expect(edit.centerState).toContainText('Your listing is safe. Try again in a moment.');
    await edit.tryAgain.click();
    await expect(edit.form.title).toHaveValue('Arizona sandals');
  });
});

test.describe('withdraw listing', () => {
  test.beforeEach(async ({ page }) => {
    await signInAs(page, marcus);
    await serveListing(page, listingDetail({ viewer: { relation: 'Seller' } }));
  });

  test('the seller withdraws their listing after confirming', async ({ page, unfixturedApiCalls }) => {
    let withdrawn = 0;
    await page.route('**/api/listings/L-10001/withdraw', (route) => {
      withdrawn++;
      return route.fulfill({ json: { reference: 'L-10001', status: 'Removed' } });
    });
    await page.route('**/api/me/listings', (route) =>
      route.fulfill({
        json: { items: [], counts: { all: 0, draft: 0, active: 0, reserved: 0, sold: 0, removed: 0 } },
      }),
    );
    const detail = new ListingDetailPage(page);
    await detail.goto();

    await detail.withdraw.click();
    const dialog = new WithdrawListingDialog(page, 'Air Max 90, white');
    await expect(dialog.root).toContainText('It leaves the shop right away. This can’t be undone.');
    await expect(dialog.keep).toBeFocused();
    await dialog.withdraw.click();

    await expect(page).toHaveURL(/\/account\/listings$/);
    expect(withdrawn).toBe(1);
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('Keep listing closes the dialog without withdrawing', async ({ page }) => {
    let withdrawn = 0;
    await page.route('**/api/listings/L-10001/withdraw', (route) => {
      withdrawn++;
      return route.fulfill({ json: { reference: 'L-10001', status: 'Removed' } });
    });
    const detail = new ListingDetailPage(page);
    await detail.goto();

    await detail.withdraw.click();
    const dialog = new WithdrawListingDialog(page, 'Air Max 90, white');
    await dialog.keep.click();

    await expect(dialog.root).toBeHidden();
    expect(withdrawn).toBe(0);
  });

  test('a failed withdraw keeps the listing live and offers another try', async ({ page }) => {
    let release: () => void = () => undefined;
    const held = new Promise<void>((resolve) => (release = resolve));
    await page.route('**/api/listings/L-10001/withdraw', async (route) => {
      await held;
      await route.fulfill(problem(500, 'SERVER_ERROR', 'Something went wrong'));
    });
    const detail = new ListingDetailPage(page);
    await detail.goto();

    await detail.withdraw.click();
    const dialog = new WithdrawListingDialog(page, 'Air Max 90, white');
    await dialog.withdraw.click();

    await expect(dialog.withdraw).toHaveAttribute('aria-busy', 'true');
    await expect(dialog.keep).toBeDisabled();
    release();
    await expect(dialog.alert).toContainText('Couldn’t withdraw');
    await expect(dialog.alert).toContainText('Your listing is still live.');
    await expect(dialog.withdraw).toHaveText('Try again');
    await expect(page).toHaveURL(/\/items\/L-10001$/);
  });
});

test.describe('relist: edit first', () => {
  test('Edit first in the Relist dialog opens the edit page', async ({ page }) => {
    await signInAs(page, marcus);
    await serveCatalogue(page);
    await page.route('**/photos/**', (route) => route.fulfill({ contentType: 'image/png', body: Buffer.alloc(0) }));
    await page.route('**/api/sales/o-1', (route) =>
      route.fulfill({ json: sale({ status: 'Cancelled', canRelist: true }) }),
    );
    await serveMine(page, { ...arizona, status: 'Removed', removalReason: 'OrderCancelled' });
    const salePage = new SaleDetailPage(page);
    await salePage.goto();

    await salePage.relistItem.click();
    await new RelistDialog(page).root.getByRole('link', { name: 'Edit first' }).click();

    await expect(page).toHaveURL(/\/items\/L-10001\/edit$/);
    await expect(new EditListingPage(page).form.title).toHaveValue('Arizona sandals');
  });
});
