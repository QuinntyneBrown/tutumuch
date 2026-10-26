// Acceptance Test
// Traces to: L2-040, L2-041, L2-054
// Description: Anyone opens /members/{displayName} and sees the avatar, display name, rating
// summary ("No reviews yet"), completed sales, member-since month and the Active listings, 24 at
// a time with Load more. A closed or unknown member shows "This member isn’t available"; any
// other failure shows the error with its reference and Try again.

import {
  expect,
  listingDetail,
  marcus,
  memberProfile,
  problem,
  serveListing,
  serveProfile,
  signInAs,
  test,
} from '../fixtures';
import { ListingDetailPage } from '../pages/listing-detail.page';
import { MemberProfilePage } from '../pages/member-profile.page';

test.describe('view member profile', () => {
  test('a visitor sees the public profile with no reviews yet', async ({ page, unfixturedApiCalls }) => {
    await serveProfile(page, memberProfile());
    const profile = new MemberProfilePage(page);
    await profile.goto();

    await expect(profile.heading).toHaveText('tutu_tessa');
    await expect(page).toHaveTitle('tutu_tessa · TuTuMuch');
    await expect(profile.header.locator('.avatar')).toHaveText('TU');
    await expect(profile.header).toContainText('No reviews yet');
    await expect(profile.stat('Sales')).toHaveText('141');
    await expect(profile.stat('Member since')).toHaveText('Jan 2025');
    await expect(profile.forSale.locator('.section-head')).toContainText('4');
    await expect(profile.cards).toHaveCount(4);
    await expect(profile.cards.first()).toHaveAttribute('href', '/items/L-10001');
    await expect(profile.reviews).toContainText('No reviews yet');
    await expect(profile.loadMore).toHaveCount(0);
    await expect(page.locator('main')).not.toContainText('@');
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('shows the public location and bio when the member has them', async ({ page }) => {
    await serveProfile(page, memberProfile({ publicLocation: 'Atlanta, GA', bio: 'Clean pairs, real photos.' }));
    const profile = new MemberProfilePage(page);
    await profile.goto();

    await expect(profile.header).toContainText('Atlanta, GA');
    await expect(profile.header).toContainText('Clean pairs, real photos.');
  });

  test('a member with nothing for sale shows the empty section', async ({ page }) => {
    await serveProfile(
      page,
      memberProfile({ displayName: 'twirly_tara', activeListingCount: 0, completedSales: 0 }),
      0,
    );
    const profile = new MemberProfilePage(page);
    await profile.goto('twirly_tara');

    await expect(profile.forSale).toContainText('No items for sale');
    await expect(profile.forSale).toContainText('twirly_tara hasn’t listed anything yet.');
    await expect(profile.cards).toHaveCount(0);
  });

  test('listings arrive 24 at a time and Load more appends the next page', async ({ page }) => {
    const pages = await serveProfile(page, memberProfile({ activeListingCount: 30 }), 30);
    const profile = new MemberProfilePage(page);
    await profile.goto();

    await expect(profile.cards).toHaveCount(24);
    await expect(profile.forSale).toContainText('Showing 24 of 30');

    await profile.loadMore.click();

    await expect(profile.cards).toHaveCount(30);
    await expect(profile.loadMore).toHaveCount(0);
    expect(pages).toEqual([1, 2]);
  });

  test('a closed or unknown member is unavailable', async ({ page }) => {
    await page.route('**/api/members/gone_guy**', (route) =>
      route.fulfill(problem(404, 'MEMBER_UNAVAILABLE', 'Not found')),
    );
    const profile = new MemberProfilePage(page);
    await profile.goto('gone_guy');

    await expect(profile.heading).toHaveText('This member isn’t available');
    await expect(page.getByText('The account may have been closed.')).toBeVisible();
    await expect(page.locator('main').getByRole('link', { name: 'Back to shop' })).toHaveAttribute('href', '/');
  });

  test('a failure shows the reference and Try again loads the profile', async ({ page }) => {
    let fail = true;
    await serveProfile(page, memberProfile());
    await page.route('**/api/members/tutu_tessa', (route) =>
      fail ? route.fulfill(problem(500, 'INTERNAL_ERROR', 'Something went wrong')) : route.fallback(),
    );
    const profile = new MemberProfilePage(page);
    await profile.goto();

    await expect(profile.heading).toHaveText('Couldn’t load this profile');
    await expect(page.locator('main .ref')).toBeVisible();
    fail = false;
    await page.getByRole('button', { name: 'Try again' }).click();

    await expect(profile.heading).toHaveText('tutu_tessa');
  });

  test('the seller link on a listing opens the seller profile', async ({ page }) => {
    await signInAs(page, marcus);
    await serveListing(page, listingDetail());
    await serveProfile(page, memberProfile());
    const detail = new ListingDetailPage(page);
    await detail.goto();

    await detail.seller.getByRole('link', { name: 'tutu_tessa' }).click();

    await expect(page).toHaveURL('/members/tutu_tessa');
    await expect(new MemberProfilePage(page).heading).toHaveText('tutu_tessa');
  });
});
