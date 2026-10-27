// Acceptance Test
// Traces to: L2-025, L2-026, L2-028, L2-063
// Description: Anyone opens /items/{reference} and sees the photos, details, price with shipping
// and total, and the seller summary. Buy now leads a member to checkout and a visitor to sign in
// and back; the seller, a Reserved and a Sold listing get no Buy now. One column with a sticky
// Buy now bar and a swipeable gallery below 992 px, two columns from 992 px.

import { catalogCard, expect, listingDetail, marcus, problem, serveListing, signInAs, test } from '../fixtures';
import { ListingDetailPage } from '../pages/listing-detail.page';
import { SignInPage } from '../pages/sign-in.page';

test.describe('view listing detail', () => {
  test('a member sees the whole listing and Buy now leads to checkout', async ({ page, unfixturedApiCalls }) => {
    await signInAs(page, marcus);
    await serveListing(page, listingDetail());
    const detail = new ListingDetailPage(page);
    await detail.goto();

    await expect(detail.heading).toHaveText('Floral party dress, pink');
    await expect(page).toHaveTitle('Floral party dress, pink · Gymboree · TuTuMuch');
    await expect(detail.breadcrumb.getByRole('link', { name: 'Clothing' })).toHaveAttribute('href', '/clothing');
    await expect(detail.breadcrumb).toContainText('Clothing');
    await expect(detail.info).toContainText('Gymboree');
    await expect(detail.info.getByLabel('Size Girls')).toBeVisible();
    await expect(detail.price).toContainText('$85.00');
    await expect(detail.price).toContainText('$9.00 shipping · $94.00 total before tax');
    await expect(detail.info).toContainText('Worn a handful of times to parties.');
    await expect(detail.facts).toContainText("Girls'");
    await expect(detail.facts).toContainText('Clothing · Dresses');
    await expect(detail.facts).toContainText('Used - Excellent');
    await expect(detail.facts.locator('time')).toHaveText('2 hours ago');
    await expect(detail.flaws).toHaveCount(0);
    await expect(detail.seller.getByRole('link', { name: 'tutu_tessa' })).toHaveAttribute(
      'href',
      '/members/tutu_tessa',
    );
    await expect(detail.seller).toContainText('New seller');
    await expect(detail.buyBar).toContainText('$94.00');
    await expect(detail.buyNow).toHaveAttribute('href', '/checkout/L-10001');
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('choosing a thumbnail shows that photo', async ({ page }) => {
    await serveListing(page, listingDetail());
    const detail = new ListingDetailPage(page);
    await detail.goto();

    await expect(detail.thumbs).toHaveCount(3);
    await expect(detail.thumbs.first()).toHaveAttribute('aria-current', 'true');
    await expect(detail.gallery.locator('img').first()).toHaveAttribute(
      'alt',
      'Photo 1 of Gymboree Floral party dress, pink',
    );
    await expect(detail.gallery.locator('img').first()).toHaveAttribute('loading', 'eager');

    await detail.thumbs.nth(1).click();

    await expect(detail.thumbs.nth(1)).toHaveAttribute('aria-current', 'true');
    await expect(detail.thumbs.first()).not.toHaveAttribute('aria-current', 'true');
    await expect.poll(() => detail.shownPhotoAlt()).toBe('Photo 2 of Gymboree Floral party dress, pink');

    await detail.thumbs.nth(1).press('ArrowRight');

    await expect(detail.thumbs.nth(2)).toBeFocused();
    await expect(detail.thumbs.nth(2)).toHaveAttribute('aria-current', 'true');
  });

  test('a used - fair listing shows its flaws and text is never HTML', async ({ page }) => {
    await serveListing(
      page,
      listingDetail({
        condition: 'Used - Fair',
        flaws: 'Small scuff on the left toe box.',
        description: '<b>Bold</b> claims about the tulle',
      }),
    );
    const detail = new ListingDetailPage(page);
    await detail.goto();

    await expect(detail.flaws).toHaveText('Flaws: Small scuff on the left toe box.');
    await expect(detail.info).toContainText('<b>Bold</b> claims about the tulle');
  });

  test('a visitor is sent to sign in by Buy now and comes back to the listing', async ({ page }) => {
    await serveListing(page, listingDetail({ viewer: { relation: 'Anonymous' } }));
    await page.route('**/api/auth/sign-in', async (route) => {
      await signInAs(page, marcus);
      await route.fulfill({ json: marcus });
    });
    const detail = new ListingDetailPage(page);
    await detail.goto();

    await expect(detail.buyNow).toHaveAttribute('href', '/sign-in?returnUrl=%2Fitems%2FL-10001');
    await detail.buyNow.click();

    const signIn = new SignInPage(page);
    await expect(signIn.heading).toHaveText('Sign in to buy');
    await signIn.signIn('marcus.bell@example.com', 'correct horse battery');

    await expect(page).toHaveURL('/items/L-10001');
    await expect(detail.heading).toHaveText('Floral party dress, pink');
  });

  test('the seller sees Edit listing and no Buy now', async ({ page }) => {
    await signInAs(page, marcus);
    await serveListing(page, listingDetail({ viewer: { relation: 'Seller' } }));
    const detail = new ListingDetailPage(page);
    await detail.goto();

    await expect(detail.editListing).toHaveAttribute('href', '/items/L-10001/edit');
    await expect(detail.buyNow).toHaveCount(0);
  });

  test('a reserved listing says it is in a checkout and offers no Buy now', async ({ page }) => {
    await serveListing(page, listingDetail({ status: 'Reserved' }));
    const detail = new ListingDetailPage(page);
    await detail.goto();

    await expect(detail.stamp).toHaveText('Reserved');
    await expect(detail.reservedAlert).toContainText('In someone’s checkout');
    await expect(detail.reservedAlert).toContainText('If they don’t finish within 10 minutes, it’s back on sale.');
    await expect(detail.buyNow).toHaveCount(0);
    await expect(detail.buyBar).toHaveCount(0);
  });

  test('a sold listing is stamped Sold and shows more items in the category', async ({ page }) => {
    await serveListing(page, listingDetail({ status: 'Sold' }));
    const requests: URLSearchParams[] = [];
    await page.route('**/api/catalog/listings?**', (route) => {
      requests.push(new URL(route.request().url()).searchParams);
      return route.fulfill({
        json: {
          items: [catalogCard(1), catalogCard(2), catalogCard(3), catalogCard(4), catalogCard(5)],
          page: 1,
          pageSize: 24,
          totalCount: 5,
        },
      });
    });
    const detail = new ListingDetailPage(page);
    await detail.goto();

    await expect(detail.stamp).toHaveText('Sold');
    await expect(detail.buyNow).toHaveCount(0);
    await expect(detail.similar.getByRole('listitem')).toHaveCount(4);
    await expect(detail.similar.getByRole('link').first()).toHaveAttribute('href', '/items/L-10002');
    expect(requests[0].get('category')).toBe('clothing');
  });

  test('a hidden or missing listing shows This item isn’t available', async ({ page }) => {
    await page.route('**/api/catalog/listings/L-10001', (route) =>
      route.fulfill(problem(404, 'LISTING_NOT_FOUND', 'Not found')),
    );
    const detail = new ListingDetailPage(page);
    await detail.goto();

    await expect(detail.heading).toHaveText('This item isn’t available');
    await expect(page.locator('main')).toContainText('It may have been sold or taken down.');
    await expect(page.getByRole('link', { name: 'Find similar' })).toHaveAttribute('href', '/');
  });

  test('a server error shows the reference and Try again loads the listing', async ({ page }) => {
    let calls = 0;
    await page.route('**/api/catalog/listings/L-10001', (route) => {
      calls += 1;
      return calls === 1
        ? route.fulfill(problem(500, 'SERVER_ERROR', 'Server error'))
        : route.fulfill({ json: listingDetail() });
    });
    await page.route('**/photos/**', (route) => route.fulfill({ status: 204 }));
    const detail = new ListingDetailPage(page);
    await detail.goto();

    await expect(detail.heading).toHaveText('Couldn’t load this item');
    await expect(page.locator('main')).toContainText('Ref 5c1d-0e7a');
    await expect(page.getByRole('link', { name: 'Shop the closet' })).toHaveAttribute('href', '/');

    await page.getByRole('button', { name: 'Try again' }).click();

    await expect(detail.heading).toHaveText('Floral party dress, pink');
  });

  test('a skeleton shows while the listing loads', async ({ page }) => {
    let release!: () => void;
    const held = new Promise<void>((resolve) => (release = resolve));
    await page.route('**/api/catalog/listings/L-10001', async (route) => {
      await held;
      await route.fulfill({ json: listingDetail() });
    });
    const detail = new ListingDetailPage(page);
    await detail.goto();

    await expect(detail.loading).toBeVisible();
    await expect(detail.heading).toHaveText('Loading listing');

    release();

    await expect(detail.loading).toHaveCount(0);
    await expect(detail.heading).toHaveText('Floral party dress, pink');
  });

  test.describe('on a phone', () => {
    test.use({ viewport: { width: 375, height: 700 } });

    test('one column, swipeable gallery and a Buy now bar that stays on screen', async ({ page }) => {
      await serveListing(page, listingDetail());
      const detail = new ListingDetailPage(page);
      await detail.goto();
      await expect(detail.heading).toBeVisible();

      const galleryBox = (await detail.gallery.boundingBox())!;
      const infoBox = (await detail.info.boundingBox())!;
      expect(infoBox.y).toBeGreaterThan(galleryBox.y + galleryBox.height - 1);

      await detail.track.evaluate((track) => track.scrollTo({ left: track.clientWidth * 2 }));
      await expect(detail.thumbs.nth(2)).toHaveAttribute('aria-current', 'true');

      await page.mouse.wheel(0, 400);
      await expect(detail.buyNow).toBeInViewport();
      const buyBox = (await detail.buyNow.boundingBox())!;
      expect(buyBox.y + buyBox.height).toBeLessThanOrEqual(700 + 1);
      expect(buyBox.height).toBeGreaterThanOrEqual(44);
      for (const thumb of await detail.thumbs.all()) {
        const box = (await thumb.boundingBox())!;
        expect(box.width).toBeGreaterThanOrEqual(44);
        expect(box.height).toBeGreaterThanOrEqual(44);
      }
    });
  });

  test.describe('on a desktop', () => {
    test.use({ viewport: { width: 1280, height: 800 } });

    test('two columns with the gallery on the left', async ({ page }) => {
      await serveListing(page, listingDetail());
      const detail = new ListingDetailPage(page);
      await detail.goto();
      await expect(detail.heading).toBeVisible();

      const galleryBox = (await detail.gallery.boundingBox())!;
      const infoBox = (await detail.info.boundingBox())!;
      expect(galleryBox.x + galleryBox.width).toBeLessThanOrEqual(infoBox.x);
      expect(Math.abs(galleryBox.y - infoBox.y)).toBeLessThan(40);
    });
  });
});
