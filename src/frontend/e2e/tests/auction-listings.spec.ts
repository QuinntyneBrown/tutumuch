// Acceptance Test
// Traces to: L2-113, L2-114, L2-115, L2-116, L2-117, L2-118, L2-120
// Description: A seller chooses Auction on the sell form with a starting bid and a length. An
// auction's card and detail page show the current bid, the bids and the time left, and Place bid
// instead of Buy now. A member bids in the Place bid dialog; a refused bid keeps the amount and
// shows why, and a bid that lost a race fills in the new lowest bid. Each viewer sees only their own
// standing. The winner pays through checkout, an unsold auction is relisted from Your listings,
// and a seller cannot change an auction once it has bids.

import { Page, Route } from '@playwright/test';

import {
  catalogCard,
  checkout,
  checkoutStart,
  expect,
  listingDetail,
  marcus,
  myListing,
  pricedCheckout,
  problem,
  serveCatalogue,
  serveCheckout,
  serveListing,
  servePhotoUploads,
  signInAs,
  test,
} from '../fixtures';
import { CatalogPage } from '../pages/catalog.page';
import { ListingDetailPage } from '../pages/listing-detail.page';
import { MyListingsPage } from '../pages/my-listings.page';
import { PlaceBidDialog } from '../pages/place-bid.page';
import { SellPage } from '../pages/sell.page';

const NOW = new Date('2026-11-01T12:00:00Z');
/** 2 days and 4 hours after NOW. */
const ENDS_AT = '2026-11-03T16:00:00Z';

/** An auction as GET /api/catalog/listings/{reference} returns it for this viewer. */
function auction(fields: Record<string, unknown> = {}): Record<string, unknown> {
  return {
    currentPrice: '12.00',
    startingBid: '12.00',
    bidCount: 0,
    endsAt: ENDS_AT,
    minimumBid: '12.00',
    ended: false,
    viewerIsHighestBidder: false,
    viewerHasBid: false,
    viewerIsWinner: false,
    payBy: null,
    ...fields,
  };
}

function auctionListing(fields: Record<string, unknown> = {}, auctionFields: Record<string, unknown> = {}) {
  const summary = auction(auctionFields);
  return listingDetail({
    title: 'Sequin swan recital tutu',
    brand: 'Capezio',
    price: summary['currentPrice'],
    shippingCost: '6.00',
    format: 'Auction',
    auction: summary,
    ...fields,
  });
}

const withBids = { currentPrice: '18.00', bidCount: 3, minimumBid: '19.00' };

/** Answers POST /api/listings/L-10001/bids with each response in turn; returns the bodies sent. */
async function serveBids(page: Page, ...responses: ((route: Route) => Promise<void>)[]): Promise<unknown[]> {
  const bodies: unknown[] = [];
  await page.route('**/api/listings/L-10001/bids', async (route) => {
    bodies.push(route.request().postDataJSON());
    await responses[Math.min(bodies.length, responses.length) - 1](route);
  });
  return bodies;
}

test.describe('auction listings', () => {
  test.beforeEach(async ({ page }) => {
    await page.clock.setFixedTime(NOW);
  });

  test('a seller puts an item up for auction with a starting bid and a length', async ({ page }) => {
    await signInAs(page, marcus);
    await serveCatalogue(page);
    await servePhotoUploads(page);
    const created: Record<string, unknown>[] = [];
    await page.route('**/api/listings', async (route) => {
      created.push(route.request().postDataJSON());
      await route.fulfill({ status: 201, json: myListing(created.at(-1)!) });
    });
    await page.route('**/api/listings/L-10001', (route) =>
      route.fulfill({ json: myListing(route.request().postDataJSON()) }),
    );
    await page.route('**/api/listings/L-10001/publish', (route) =>
      route.fulfill({ json: { reference: 'L-10001', status: 'Active', payoutOnboardingRequired: false } }),
    );
    const sell = new SellPage(page);
    await sell.goto();

    const format = page.getByRole('group', { name: 'How do you want to sell it?' });
    await expect(format.getByRole('radio', { name: 'Fixed price' })).toBeChecked();
    await expect(page.locator('label[for="price"]')).toHaveText('Price');
    await expect(page.getByRole('group', { name: 'Auction length' })).toHaveCount(0);

    await sell.fillComplete();
    await format.getByRole('radio', { name: 'Auction' }).check();
    await expect(page.locator('label[for="price"]')).toHaveText('Starting bid');
    const length = page.getByRole('group', { name: 'Auction length' });
    await expect(length.getByRole('radio')).toHaveCount(3);

    await sell.publish.click();
    await expect(sell.errorSummary).toContainText('Choose how long the auction runs');
    expect(created).toEqual([]);

    await length.getByLabel('5 days').check();
    await sell.publish.click();

    await expect(sell.heading).toHaveText('It’s live');
    expect(created.at(-1)).toMatchObject({ format: 'Auction', auctionDays: 5, price: 60 });
  });

  test('a card shows the Auction stamp, the current bid and the time left', async ({ page }) => {
    await page.route('**/api/catalog/listings**', (route) =>
      route.fulfill({
        json: {
          items: [
            catalogCard(1, {
              title: 'Sequin swan recital tutu',
              price: '18.00',
              format: 'Auction',
              auction: { startingBid: '12.00', bidCount: 3, endsAt: ENDS_AT },
            }),
            catalogCard(2, {
              title: 'Patent tap shoes',
              price: '14.00',
              format: 'Auction',
              auction: { startingBid: '14.00', bidCount: 0, endsAt: '2026-11-01T12:42:30Z' },
            }),
          ],
          page: 1,
          pageSize: 24,
          totalCount: 2,
        },
      }),
    );
    await page.route('**/api/catalog/brands', (route) => route.fulfill({ json: [] }));
    const catalog = new CatalogPage(page);
    await catalog.goto();

    const swan = catalog.card('Sequin swan recital tutu');
    await expect(swan.locator('.photo__stamp')).toHaveText('Auction');
    await expect(swan.locator('.listing-card__price')).toHaveText('Current bid $18');
    await expect(swan.locator('.listing-card__time')).toHaveText('Ends in 2d 4h');
    const tap = catalog.card('Patent tap shoes');
    await expect(tap.locator('.listing-card__price')).toHaveText('Starting bid $14');
    await expect(tap.locator('.listing-card__time--soon')).toHaveText('Ends in 42 min');
  });

  test('a member places the first bid and becomes the highest bidder', async ({ page, unfixturedApiCalls }) => {
    await signInAs(page, marcus);
    await serveListing(page, auctionListing());
    const bodies = await serveBids(page, (route) =>
      route.fulfill({
        status: 201,
        json: auction({ bidCount: 1, minimumBid: '13.00', viewerIsHighestBidder: true, viewerHasBid: true }),
      }),
    );
    const detail = new ListingDetailPage(page);
    await detail.goto();

    const panel = page.getByRole('region', { name: 'Auction' });
    await expect(panel).toContainText('Starting bid');
    await expect(panel).toContainText('$12.00');
    await expect(panel).toContainText('No bids yet');
    await expect(panel).toContainText('Ends in 2d 4h');
    await expect(panel).toContainText('$6.00 shipping');
    await expect(detail.buyNow).toHaveCount(0);
    await expect(detail.buyBar).toContainText('Starting bid');

    await page.getByRole('button', { name: 'Place bid' }).click();
    const dialog = new PlaceBidDialog(page);
    await expect(dialog.dialog).toContainText(
      'If you win, you pay your bid plus $6.00 shipping and tax within 48 hours.',
    );
    await expect(dialog.dialog).toContainText('Starting bid $12.00 · No bids yet · Ends in 2d 4h');
    await expect(dialog.amount).toHaveValue('12.00');
    await expect(dialog.dialog).toContainText('Bid $12.00 or more');
    await dialog.placeBid.click();

    await expect(dialog.dialog).toHaveCount(0);
    await expect(detail.toast).toContainText('Bid placed. You’re the highest bidder');
    await expect(panel).toContainText('You’re the highest bidder');
    await expect(panel).toContainText('1 bid');
    await expect(page.getByRole('button', { name: 'Place bid' })).toHaveCount(0);
    expect(bodies).toEqual([{ amount: 12 }]);
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('a refused bid keeps the amount and shows why on the field', async ({ page }) => {
    await signInAs(page, marcus);
    await serveListing(page, auctionListing({}, withBids));
    await serveBids(page, (route) =>
      route.fulfill(
        problem(422, 'BID_TOO_LOW', 'Bid $19.00 or more', {
          amount: { code: 'BID_TOO_LOW', message: 'Bid $19.00 or more' },
        }),
      ),
    );
    const detail = new ListingDetailPage(page);
    await detail.goto();
    await page.getByRole('button', { name: 'Place bid' }).click();
    const dialog = new PlaceBidDialog(page);

    await dialog.amount.fill('18.50');
    await dialog.placeBid.click();

    await expect(dialog.amountError).toHaveText('Bid $19.00 or more');
    await expect(dialog.amount).toHaveValue('18.50');
    await expect(dialog.amount).toHaveAttribute('aria-invalid', 'true');
  });

  test('a bid that lost a race fills in the new lowest bid', async ({ page }) => {
    await signInAs(page, marcus);
    let reads = 0;
    await page.route('**/api/catalog/listings/L-10001', (route) => {
      reads++;
      return route.fulfill({
        json:
          reads === 1
            ? auctionListing({}, withBids)
            : auctionListing({}, { ...withBids, currentPrice: '21.00', bidCount: 4, minimumBid: '22.00' }),
      });
    });
    await page.route('**/photos/**', (route) => route.fulfill({ contentType: 'image/png', body: Buffer.alloc(0) }));
    await serveBids(page, (route) =>
      route.fulfill(
        problem(409, 'OUTBID', 'Someone just bid higher', {
          amount: { code: 'OUTBID', message: 'Bid $22.00 or more' },
        }),
      ),
    );
    const detail = new ListingDetailPage(page);
    await detail.goto();
    await page.getByRole('button', { name: 'Place bid' }).click();
    const dialog = new PlaceBidDialog(page);
    await expect(dialog.amount).toHaveValue('19.00');

    await dialog.placeBid.click();

    await expect(dialog.alert).toContainText('Someone just bid higher');
    await expect(dialog.alert).toContainText('The current bid is now $21.00.');
    await expect(dialog.amount).toHaveValue('22.00');
  });

  test('a visitor signs in to bid', async ({ page }) => {
    await serveListing(page, auctionListing({ viewer: { relation: 'Anonymous' } }));
    const detail = new ListingDetailPage(page);
    await detail.goto();

    await expect(page.getByRole('link', { name: 'Place bid' })).toHaveAttribute(
      'href',
      '/sign-in?returnUrl=%2Fitems%2FL-10001',
    );
  });

  test('an outbid member is told so, and under an hour the time left is in minutes', async ({ page }) => {
    await signInAs(page, marcus);
    await serveListing(page, auctionListing({}, { ...withBids, viewerHasBid: true, endsAt: '2026-11-01T12:42:30Z' }));
    const detail = new ListingDetailPage(page);
    await detail.goto();

    const panel = page.getByRole('region', { name: 'Auction' });
    await expect(panel.getByRole('status')).toContainText('You’ve been outbid');
    await expect(panel.getByRole('status')).toContainText('Bid $19.00 or more to get back in.');
    await expect(panel.locator('.auction-panel__time--soon')).toHaveText('Ends in 42 min');
    await expect(page.getByRole('button', { name: 'Place bid' })).toBeVisible();
  });

  test('the seller of an auction with bids can neither bid, edit nor withdraw it', async ({ page }) => {
    await signInAs(page, marcus);
    await serveListing(page, auctionListing({ viewer: { relation: 'Seller' } }, withBids));
    const detail = new ListingDetailPage(page);
    await detail.goto();

    await expect(page.getByRole('region', { name: 'Auction' })).toContainText(
      'This auction has bids, so it can’t be changed',
    );
    await expect(page.getByRole('button', { name: 'Place bid' })).toHaveCount(0);
    await expect(detail.editListing).toHaveCount(0);
    await expect(detail.withdraw).toHaveCount(0);
  });

  test('the seller of an auction without bids can still edit and withdraw it', async ({ page }) => {
    await signInAs(page, marcus);
    await serveListing(page, auctionListing({ viewer: { relation: 'Seller' } }));
    const detail = new ListingDetailPage(page);
    await detail.goto();

    await expect(detail.editListing).toBeVisible();
    await expect(detail.withdraw).toBeVisible();
    await expect(detail.showcaseSwitch).toHaveCount(0);
  });

  test('the seller of an unsold auction is sent to Your listings to relist it', async ({ page }) => {
    await signInAs(page, marcus);
    await serveListing(page, auctionListing({ status: 'Removed', viewer: { relation: 'Seller' } }, { ended: true }));
    const detail = new ListingDetailPage(page);
    await detail.goto();

    await expect(page.getByRole('region', { name: 'Auction' }).getByRole('status')).toContainText(
      'This auction ended with no bids',
    );
    await expect(page.getByRole('link', { name: 'Your listings' }).last()).toHaveAttribute(
      'href',
      '/account/listings?status=removed',
    );
  });

  test('the winner is asked to pay and others see that it ended', async ({ page }) => {
    await signInAs(page, marcus);
    const ended = { ...withBids, ended: true, payBy: '2026-11-05T16:00:00Z' };
    await serveListing(page, auctionListing({ status: 'Reserved' }, { ...ended, viewerIsWinner: true }));
    const detail = new ListingDetailPage(page);
    await detail.goto();

    const panel = page.getByRole('region', { name: 'Auction' });
    await expect(panel).toContainText('Winning bid');
    await expect(panel.getByRole('status')).toContainText('You won');
    await expect(page.getByRole('link', { name: 'Pay now' })).toHaveAttribute('href', '/checkout/L-10001');
    await expect(detail.reservedAlert).toHaveCount(0);

    await page.unroute('**/api/catalog/listings/L-10001');
    await serveListing(page, auctionListing({ status: 'Reserved' }, ended));
    await page.reload();

    await expect(panel.getByRole('status')).toContainText('This auction has ended');
    await expect(panel.getByRole('status')).toContainText('It ended with a winning bid of $18.00.');
    await expect(page.getByRole('link', { name: 'Pay now' })).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Place bid' })).toHaveCount(0);
  });

  test('the winner pays at the winning bid and can come back later', async ({ page }) => {
    await page.clock.setFixedTime(checkoutStart);
    await signInAs(page, marcus);
    const won = checkout({ itemPrice: '18.00', auctionPayBy: '2026-11-05T16:00:00Z' });
    const priced = pricedCheckout({ itemPrice: '18.00', auctionPayBy: '2026-11-05T16:00:00Z' });
    await serveCheckout(page, {
      start: won,
      address: { status: 200, contentType: 'application/json', body: JSON.stringify(priced) },
      polls: [priced],
    });
    await page.route('**/photos/**', (route) => route.fulfill({ contentType: 'image/png', body: Buffer.alloc(0) }));
    await page.goto('/checkout/L-10001');

    await expect(page.locator('.alert').filter({ hasText: 'You won this auction' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Pay later' })).toHaveAttribute('href', '/items/L-10001');
    await expect(page.getByRole('button', { name: 'Cancel and release item' })).toHaveCount(0);
  });

  test('anyone but the winner is told the auction has ended at checkout', async ({ page }) => {
    await signInAs(page, marcus);
    await serveCheckout(page, { start: problem(409, 'AUCTION_ENDED', 'This auction has ended') });
    await page.goto('/checkout/L-10001');

    await expect(page.getByRole('heading', { name: 'This auction has ended' })).toBeVisible();
    await expect(page.getByLabel('Street address')).toHaveCount(0);
  });

  test('an auction row shows its bids and time left, and an unsold one is relisted', async ({ page }) => {
    await signInAs(page, marcus);
    await page.route('**/photos/**', (route) => route.fulfill({ contentType: 'image/png', body: Buffer.alloc(0) }));
    const row = (fields: Record<string, unknown>) => ({
      category: 'Dance',
      price: 12,
      photo: null,
      createdAt: '2026-10-30T12:00:00Z',
      saleOrderId: null,
      format: 'Auction',
      currentBid: null,
      bidCount: 0,
      auctionEndsAt: ENDS_AT,
      removalReason: null,
      canRelist: false,
      ...fields,
    });
    let relisted = false;
    await page.route('**/api/me/listings*', (route) =>
      route.fulfill({
        json: {
          items: [
            row({
              reference: 'L-10002',
              status: 'Active',
              title: 'Sequin swan recital tutu',
              currentBid: 18,
              bidCount: 3,
            }),
            row({
              reference: 'L-10001',
              status: relisted ? 'Active' : 'Removed',
              title: 'Patent tap shoes',
              removalReason: relisted ? null : 'AuctionUnsold',
              canRelist: !relisted,
            }),
          ],
          counts: { all: 2, draft: 0, active: relisted ? 2 : 1, reserved: 0, sold: 0, removed: relisted ? 0 : 1 },
        },
      }),
    );
    await page.route('**/api/listings/L-10001/relist', (route) => {
      relisted = true;
      return route.fulfill({ status: 204 });
    });
    const listings = new MyListingsPage(page);
    await listings.goto();

    await expect(listings.row('Sequin swan recital tutu')).toContainText('$18');
    await expect(listings.row('Sequin swan recital tutu')).toContainText('Auction · 3 bids · Ends in 2d 4h');
    await expect(listings.row('Patent tap shoes')).toContainText('Auction ended, no bids');

    await page.getByRole('button', { name: 'Relist Patent tap shoes' }).click();

    await expect(page.locator('.toast-region .toast').filter({ hasText: 'Relisted' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Relist Patent tap shoes' })).toHaveCount(0);
    expect(relisted).toBe(true);
  });
});
