// Acceptance Test
// Traces to: L2-003, L2-011, L2-012, L2-013, L2-014, L2-015, L2-016, L2-017, L2-018, L2-019, L2-031, L2-051, L2-002
// Description: A verified member opens /sell, sees the size fixed by the category, saves a draft
// and publishes; the client never sends a size. Visitors sign in first, unverified members are
// asked to verify, and the daily limit and load failures show their mocked states.

import { Request } from '@playwright/test';
import {
  expect,
  marcus,
  myListing,
  problem,
  serveCatalogue,
  servePhotoUploads,
  signInAs,
  test,
  unverifiedMarcus,
} from '../fixtures';
import { SellPage } from '../pages/sell.page';

test.describe('sell an item', () => {
  test('a visitor is sent to sign in and comes back to /sell', async ({ page }) => {
    const sell = new SellPage(page);

    await sell.goto();

    await expect(page).toHaveURL('/sign-in?returnUrl=%2Fsell');
  });

  test.describe('as a verified member', () => {
    test.beforeEach(async ({ page }) => {
      await signInAs(page, marcus);
      await serveCatalogue(page);
      await servePhotoUploads(page);
    });

    test('the form opens without approval and offers no size choice', async ({ page }) => {
      const sell = new SellPage(page);
      await sell.goto();

      await expect(sell.heading).toHaveText('Sell an item');
      await expect(page.getByLabel(/size/i).and(page.locator('input, select'))).toHaveCount(0);
      await expect(sell.type).toBeDisabled();
      await expect(sell.fixedSize).toHaveCount(0);
      await expect(sell.condition.getByRole('radio')).toHaveCount(5);
      await expect(page.getByText('This item is for girls')).toBeVisible();
    });

    test('the size shown follows the category as read-only text', async ({ page }) => {
      const sell = new SellPage(page);
      await sell.goto();

      await sell.category.selectOption('Clothing');
      await expect(sell.fixedSize).toContainText("Size: Girls'");
      await expect(sell.fixedSize).toContainText('Set by category');
      await expect(page.getByText('This item is for girls')).toBeVisible();

      await sell.category.selectOption('Shoes');
      await expect(sell.fixedSize).toContainText("Size: Girls'");
      await expect(page.getByText('This item is for girls')).toBeVisible();
      await expect(sell.type.getByRole('option')).toHaveText([
        'Choose a type',
        'Sneakers',
        'Boots',
        'Sandals',
        'Dress shoes',
      ]);
    });

    test('estimated earnings after the fee follow the price', async ({ page }) => {
      const sell = new SellPage(page);
      await sell.goto();

      await expect(sell.earnings).toContainText('You earn after the 10% fee');
      await expect(sell.earnings.locator('strong')).toHaveText('—');
      await sell.price.fill('60.00');
      await sell.shipping.fill('8.00');
      await expect(sell.earnings.locator('strong')).toHaveText('$61.20');
      await sell.price.fill('0.99');
      await expect(sell.earnings.locator('strong')).toHaveText('—');
    });

    test('Used - Fair asks for the flaws', async ({ page }) => {
      const sell = new SellPage(page);
      await sell.goto();

      await expect(sell.flaws).toHaveCount(0);
      await sell.condition.getByLabel('Used - Fair').check();
      await expect(sell.flaws).toBeVisible();
    });

    test('a draft with only a category and title saves, then saves again in place', async ({ page }) => {
      const bodies: unknown[] = [];
      await page.route('**/api/listings', async (route) => {
        bodies.push(route.request().postDataJSON());
        await route.fulfill({ status: 201, json: myListing({ category: 'Shoes', title: 'Grey hoodie' }) });
      });
      let updated: Request | null = null;
      await page.route('**/api/listings/L-10001', async (route) => {
        updated = route.request();
        await route.fulfill({ json: myListing({ category: 'Shoes', title: 'Grey hoodie, heavy' }) });
      });
      const sell = new SellPage(page);
      await sell.goto();

      await sell.category.selectOption('Shoes');
      await sell.title.fill('Grey hoodie');
      await sell.saveDraft.click();

      const saved = page.getByRole('status').filter({ hasText: 'Draft saved' });
      await expect(saved).toContainText('Finish it any time from your listings.');
      await expect(saved.getByRole('link', { name: 'your listings' })).toHaveAttribute('href', '/account/listings');
      expect(bodies).toEqual([{ category: 'Shoes', title: 'Grey hoodie', photoIds: [] }]);

      await sell.title.fill('Grey hoodie, heavy');
      await sell.saveDraft.click();
      await expect.poll(() => updated?.method()).toBe('PUT');
      expect(updated!.postDataJSON()).toEqual({ category: 'Shoes', title: 'Grey hoodie, heavy', photoIds: [] });
    });

    test('a draft needs a category', async ({ page, unfixturedApiCalls }) => {
      const sell = new SellPage(page);
      await sell.goto();

      await sell.saveDraft.click();

      await expect(sell.errorSummary.getByRole('link', { name: 'Choose a category' })).toBeVisible();
      expect(unfixturedApiCalls).toEqual([]);
    });

    test('publishing an incomplete listing lists every problem and focuses the summary', async ({
      page,
      unfixturedApiCalls,
    }) => {
      const sell = new SellPage(page);
      await sell.goto();

      await sell.category.selectOption('Clothing');
      await sell.type.selectOption('Dresses');
      await sell.title.fill('AF1');
      await sell.condition.getByLabel('Used - Fair').check();
      await sell.price.fill('0.99');
      await sell.publish.click();

      await expect(sell.errorSummary).toBeFocused();
      for (const message of [
        'Add at least one photo',
        'Title must be 5 to 80 characters',
        'Brand must be 1 to 50 characters',
        "Describe the item's flaws",
        'Description must be 20 to 2,000 characters',
        'Enter a price from $1.00 to $5,000.00',
        'Enter shipping from $0.00 to $100.00',
        'Confirm this item is for girls',
      ]) {
        await expect(sell.errorSummary.getByRole('link', { name: message })).toBeVisible();
      }
      await expect(sell.title).toHaveAttribute('aria-invalid', 'true');
      await expect(sell.fieldError('confirm')).toHaveText('Confirm this item is for girls');
      expect(unfixturedApiCalls).toEqual([]);
    });

    test('a complete confirmed listing is published without a size and prompts for payouts', async ({ page }) => {
      let created: Record<string, unknown> | null = null;
      let published: unknown;
      await page.route('**/api/listings', async (route) => {
        created = route.request().postDataJSON();
        await route.fulfill({ status: 201, json: myListing(created!) });
      });
      await page.route('**/api/listings/L-10001/publish', async (route) => {
        published = route.request().postDataJSON();
        await route.fulfill({ json: { reference: 'L-10001', status: 'Active', payoutOnboardingRequired: true } });
      });
      const sell = new SellPage(page);
      await sell.goto();

      await sell.fillComplete();
      await sell.publish.click();

      await expect(sell.heading).toHaveText('It’s live');
      await expect(page.getByText('Rainbow tulle tutu, pink')).toBeVisible();
      await expect(page.getByText('Gymboree · Girls’ · $60.00')).toBeVisible();
      await expect(page.getByRole('link', { name: 'Set up payouts' })).toBeVisible();
      await expect(page.getByRole('link', { name: 'View listing' })).toHaveAttribute('href', '/items/L-10001');
      expect(created).toEqual({
        category: 'Clothing',
        subcategory: 'Dresses',
        title: 'Rainbow tulle tutu, pink',
        brand: 'Gymboree',
        condition: 'Used - Good',
        description: 'Light wear, colours still bright. Cleaned and ready to wear.',
        price: 60,
        shippingCost: 8,
        photoIds: ['p-1'],
      });
      expect(created).not.toHaveProperty('size');
      expect(published).toEqual({ sizeConfirmed: true });

      await page.getByRole('link', { name: 'Sell another' }).click();
      await expect(sell.heading).toHaveText('Sell an item');
      await expect(sell.title).toHaveValue('');
      await expect(sell.photoTiles).toHaveCount(0);
    });

    test('a title with markup is shown as text', async ({ page }) => {
      await page.route('**/api/listings', (route) =>
        route.fulfill({ status: 201, json: myListing(route.request().postDataJSON()) }),
      );
      await page.route('**/api/listings/L-10001/publish', (route) =>
        route.fulfill({ json: { reference: 'L-10001', status: 'Active', payoutOnboardingRequired: false } }),
      );
      const sell = new SellPage(page);
      await sell.goto();

      await sell.fillComplete();
      await sell.title.fill('<script>alert(1)</script>');
      await sell.publish.click();

      await expect(page.getByText('<script>alert(1)</script>')).toBeVisible();
      await expect(page.getByRole('link', { name: 'Set up payouts' })).toHaveCount(0);
    });

    test('server field errors from publishing are shown against their fields', async ({ page }) => {
      await page.route('**/api/listings', (route) =>
        route.fulfill({ status: 201, json: myListing(route.request().postDataJSON()) }),
      );
      await page.route('**/api/listings/L-10001/publish', (route) =>
        route.fulfill(
          problem(422, 'TITLE_LENGTH', 'Check the highlighted fields', {
            title: { code: 'TITLE_LENGTH', message: 'Title must be 5 to 80 characters' },
          }),
        ),
      );
      const sell = new SellPage(page);
      await sell.goto();

      await sell.fillComplete();
      await sell.publish.click();

      await expect(sell.errorSummary.getByRole('link', { name: 'Title must be 5 to 80 characters' })).toBeVisible();
      await expect(sell.fieldError('title')).toHaveText('Title must be 5 to 80 characters');
    });

    test('over the daily limit the member is told to come back tomorrow', async ({ page }) => {
      await page.route('**/api/listings', (route) =>
        route.fulfill({ status: 201, json: myListing(route.request().postDataJSON()) }),
      );
      await page.route('**/api/listings/L-10001/publish', (route) =>
        route.fulfill({
          ...problem(429, 'RATE_LIMITED', 'That’s 20 listings today'),
          headers: { 'Retry-After': '3600' },
        }),
      );
      const sell = new SellPage(page);
      await sell.goto();

      await sell.fillComplete();
      await sell.publish.click();

      await expect(sell.heading).toHaveText('That’s 20 listings today');
      await expect(page.getByText('You can list more tomorrow. Drafts still save.')).toBeVisible();
      await expect(page.getByRole('link', { name: 'Your listings' })).toHaveAttribute('href', '/account/listings');
    });

    test('a saved draft opens at /sell/{reference}', async ({ page }) => {
      await page.route('**/api/me/listings/L-10001', (route) =>
        route.fulfill({
          json: myListing({
            category: 'Shoes',
            subcategory: 'Sneakers',
            title: 'Grey hoodie',
            condition: 'Used - Good',
            price: 25,
          }),
        }),
      );
      const sell = new SellPage(page);

      await sell.goto('/sell/L-10001');

      await expect(sell.category).toHaveValue('Shoes');
      await expect(sell.type).toHaveValue('Sneakers');
      await expect(sell.title).toHaveValue('Grey hoodie');
      await expect(sell.condition.getByLabel('Used - Good')).toBeChecked();
      await expect(sell.price).toHaveValue('25.00');
    });
  });

  test('when categories fail to load the form shows a reference and can retry', async ({ page }) => {
    await signInAs(page, marcus);
    let attempts = 0;
    await page.route('**/api/categories', (route) => {
      attempts += 1;
      return attempts === 1 ? route.fulfill(problem(500, 'SERVER_ERROR', 'Server error')) : route.fulfill({ json: [] });
    });
    await page.route('**/api/selling/options', (route) => route.fulfill(problem(500, 'SERVER_ERROR', 'Server error')));
    const sell = new SellPage(page);

    await sell.goto();

    await expect(sell.heading).toHaveText('Couldn’t open the form');
    await expect(page.getByText('Ref 5c1d-0e7a')).toBeVisible();
    await page.getByRole('button', { name: 'Try again' }).click();
    await expect.poll(() => attempts).toBe(2);
  });

  test.describe('as an unverified member', () => {
    test.beforeEach(async ({ page }) => {
      await signInAs(page, unverifiedMarcus);
    });

    test('selling waits for email verification and the link can be resent', async ({ page }) => {
      let requested: unknown;
      await page.route('**/api/auth/verification-email', async (route) => {
        requested = route.request().postDataJSON();
        await route.fulfill({ status: 202 });
      });
      const sell = new SellPage(page);
      await sell.goto();

      await expect(sell.heading).toHaveText('Verify your email to sell');
      await expect(page.getByText('We sent a link to marcus.bell@example.com.')).toBeVisible();
      await page.getByRole('button', { name: 'Resend link' }).click();

      await expect(sell.dialog).toBeVisible();
      await expect(sell.dialog.getByRole('heading', { name: 'Verify your email first' })).toBeVisible();
      await sell.dialog.getByRole('button', { name: 'Resend link' }).click();
      await expect(sell.dialog.getByRole('status')).toContainText('Link sent');
      expect(requested).toEqual({ email: 'marcus.bell@example.com' });

      await sell.dialog.getByRole('button', { name: 'Done' }).click();
      await expect(sell.dialog).toHaveCount(0);
      await expect(page.getByRole('button', { name: 'Resend link' })).toBeFocused();
    });

    test('a fourth link within an hour is refused in the dialog', async ({ page }) => {
      await page.route('**/api/auth/verification-email', (route) =>
        route.fulfill(problem(429, 'RATE_LIMITED', 'You can request 3 links an hour. Try again later.')),
      );
      const sell = new SellPage(page);
      await sell.goto();

      await page.getByRole('button', { name: 'Resend link' }).click();
      await sell.dialog.getByRole('button', { name: 'Resend link' }).click();

      await expect(sell.dialog).toContainText('Too many requests');
      await expect(sell.dialog).toContainText('You can request 3 links an hour. Try again later.');
    });

    test('Escape closes the dialog', async ({ page }) => {
      const sell = new SellPage(page);
      await sell.goto();

      await page.getByRole('button', { name: 'Resend link' }).click();
      await expect(sell.dialog).toBeVisible();
      await page.keyboard.press('Escape');

      await expect(sell.dialog).toHaveCount(0);
    });
  });
});
