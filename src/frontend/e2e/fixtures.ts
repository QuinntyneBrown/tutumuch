import { Page, Route, test as base, expect } from '@playwright/test';

/** The signed-in member as GET /api/auth/me returns it. */
export interface MemberFixture {
  displayName: string;
  email: string;
  emailVerified: boolean;
  isAdministrator: boolean;
}

export const marcus: MemberFixture = {
  displayName: 'marcus_12',
  email: 'marcus.bell@example.com',
  emailVerified: true,
  isAdministrator: false,
};

/** The owner, who holds the Administrator role and is the only seller in launch Phases 1 and 2. */
export const administrator: MemberFixture = {
  displayName: 'ana_ruiz',
  email: 'ana.ruiz@tutumuch.test',
  emailVerified: true,
  isAdministrator: true,
};

/** GET /api/launch for a launch phase (L2-090); every test runs in Phase 3 unless it serves another. */
export function launchPhase(phase: 1 | 2 | 3): Record<string, unknown> {
  return { phase, categoryMenusVisible: phase !== 1, memberSellingOpen: phase === 3 };
}

/** Answers GET /api/launch with the phase; call before the first navigation. */
export async function serveLaunchPhase(page: Page, phase: 1 | 2 | 3): Promise<void> {
  await page.route('**/api/launch', (route) => route.fulfill({ json: launchPhase(phase) }));
}

export const unverifiedMarcus: MemberFixture = { ...marcus, emailVerified: false };

/** GET /api/categories: the L2-011 table. */
export const categories = [
  {
    name: 'Clothing',
    subcategories: ['Dresses', 'Tops', 'Bottoms', 'Outerwear', 'Pajamas', 'Swimwear'],
    acceptedSize: "Girls'",
    sizeLabel: 'Girls',
  },
  {
    name: 'Shoes',
    subcategories: ['Sneakers', 'Boots', 'Sandals', 'Dress shoes'],
    acceptedSize: "Girls'",
    sizeLabel: 'Girls',
  },
  {
    name: 'Dance',
    subcategories: ['Tutus', 'Leotards', 'Dance shoes', 'Dance bags'],
    acceptedSize: "Girls'",
    sizeLabel: 'Girls',
  },
  {
    name: 'Sports',
    subcategories: ['Soccer', 'Volleyball', 'Gymnastics', 'Swimming', 'Bikes and scooters'],
    acceptedSize: "Girls'",
    sizeLabel: 'Girls',
  },
  {
    name: 'Cameras and tech',
    subcategories: ['Cameras', 'Headphones', 'Tablet accessories'],
    acceptedSize: "Girls'",
    sizeLabel: 'Girls',
  },
  {
    name: 'Toys and crafts',
    subcategories: ['Dolls', 'Arts and crafts', 'Games and puzzles', 'Books'],
    acceptedSize: "Girls'",
    sizeLabel: 'Girls',
  },
];

/** GET /api/selling/options. */
export const sellingOptions = {
  feePercent: 10,
  conditions: ['New with tags', 'New without tags', 'Used - Excellent', 'Used - Good', 'Used - Fair'],
  suggestedBrands: ['Gymboree', 'Crewcuts', 'Hanna Andersson'],
  maxPhotos: 8,
};

/** Answers the two catalogue calls the sell form needs. */
export async function serveCatalogue(page: Page): Promise<void> {
  await page.route('**/api/categories', (route) => route.fulfill({ json: categories }));
  await page.route('**/api/selling/options', (route) => route.fulfill({ json: sellingOptions }));
}

/** A listing as the listings endpoints return it to its seller. */
export function myListing(fields: Record<string, unknown>): Record<string, unknown> {
  const category = (fields['category'] as string) ?? 'Clothing';
  return {
    reference: 'L-10001',
    status: 'Draft',
    category,
    subcategory: null,
    size: "Girls'",
    title: null,
    description: null,
    brand: null,
    condition: null,
    flaws: null,
    price: null,
    shippingCost: null,
    createdAt: '2026-10-25T12:00:00Z',
    updatedAt: '2026-10-25T12:00:00Z',
    publishedAt: null,
    photos: [],
    ...fields,
  };
}

/** An uploaded photo as POST /api/photo-uploads returns it; the thumbnail is the 400 px WebP. */
export function photo(id: string): Record<string, unknown> {
  const hash = id.padEnd(64, '0').replace(/[^0-9a-f]/g, 'a');
  return {
    id,
    url: `/photos/${hash}/400.webp`,
    width: 1200,
    height: 1600,
    versions: [400, 800, 1600].flatMap((size) =>
      ['WebP', 'Jpeg'].map((format) => ({
        size,
        width: Math.min(size, 1200),
        height: Math.round((1600 * Math.min(size, 1200)) / 1200),
        format,
        url: `/photos/${hash}/${size}.${format === 'WebP' ? 'webp' : 'jpg'}`,
      })),
    ),
  };
}

/** A 1x1 PNG served for every photo version. */
const PIXEL = Buffer.from(
  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR4nGNgYGD4DwABBAEAwS2OUAAAAABJRU5ErkJggg==',
  'base64',
);

/**
 * Answers POST /api/photo-uploads with photos p-1, p-2... in upload order, and serves every
 * photo version. Returns the names of the uploaded files.
 */
export async function servePhotoUploads(page: Page): Promise<string[]> {
  const uploaded: string[] = [];
  await page.route('**/api/photo-uploads', async (route) => {
    const name = /filename="([^"]+)"/.exec(route.request().postDataBuffer()?.toString('latin1') ?? '')?.[1] ?? '';
    uploaded.push(name);
    await route.fulfill({ status: 201, json: photo(`p-${uploaded.length}`) });
  });
  await page.route('**/photos/**', (route) => route.fulfill({ contentType: 'image/png', body: PIXEL }));
  return uploaded;
}

/** A small file the uploader accepts; the fixtures decide what TuTuMuch API says about it. */
export function imageFile(name: string, bytes = 2048): { name: string; mimeType: string; buffer: Buffer } {
  return { name, mimeType: name.endsWith('.png') ? 'image/png' : 'image/jpeg', buffer: Buffer.alloc(bytes, 1) };
}

/** An RFC 9457 problem body as TuTuMuch API writes it (L2-068). */
export function problem(
  status: number,
  code: string,
  title: string,
  errors?: Record<string, { code: string; message: string }>,
): { status: number; contentType: string; body: string } {
  return {
    status,
    contentType: 'application/problem+json',
    body: JSON.stringify({ title, status, code, correlationId: '5c1d0e7a9b2f4c3d', errors }),
  };
}

/**
 * Answers GET /api/auth/me and POST /api/auth/refresh with this member, and GET /api/payouts with
 * a summary that shows no payout banner (a test that needs one routes it again).
 */
export async function signInAs(page: Page, member: MemberFixture): Promise<void> {
  const answer = (route: Route) => route.fulfill({ json: member });
  await page.route('**/api/auth/me', answer);
  await page.route('**/api/auth/refresh', answer);
  await page.route('**/api/payouts', (route) =>
    route.fulfill({ json: payoutSummary({ history: [], pending: '0.00' }) }),
  );
}

/**
 * Every test gets a page whose calls to TuTuMuch API are answered by fixtures (an auto fixture, so it
 * is in place before any beforeEach hook adds its own routes). The session endpoints
 * answer 401 (a visitor) unless a test signs a member in, and no third-party sign-in is offered
 * unless a test says so. Other unfixtured API calls are answered 501 and recorded so a test can
 * assert none happened.
 */
/** The visitor's cookie choice before the test starts; `unknown` shows the consent banner (L2-056). */
export type ConsentFixture = 'unknown' | 'necessary' | 'analytics';

/** The fake analytics provider: its script origin and the cookie its script sets. */
export const ANALYTICS_ORIGIN = 'https://analytics.tutumuch.invalid';
export const ANALYTICS_COOKIE = '_tma';

export const test = base.extend<{ consent: ConsentFixture; unfixturedApiCalls: string[] }>({
  /** Tests start with "Necessary only" chosen, so the banner stays out of their way. */
  consent: ['necessary', { option: true }],
  unfixturedApiCalls: [
    async ({ page, consent, baseURL }, use) => {
      if (consent !== 'unknown') {
        await page
          .context()
          .addCookies([
            { name: 'tm_consent', value: `v1.${consent}`, url: baseURL!, secure: true, sameSite: 'Strict' },
          ]);
      }
      await page.route(`${ANALYTICS_ORIGIN}/**`, (route) =>
        route.fulfill({
          contentType: 'text/javascript',
          body: `document.cookie = '${ANALYTICS_COOKIE}=1; Path=/; Max-Age=3600';
            window.tutumuchAnalytics = { pageView: function () {}, optOut: function () {} };`,
        }),
      );
      const calls: string[] = [];
      await page.route('**/api/**', (route) => {
        calls.push(route.request().url());
        return route.fulfill({ status: 501, json: { error: 'no fixture for this request' } });
      });
      const visitor = (route: Route) => route.fulfill(problem(401, 'UNAUTHENTICATED', 'Sign in to continue'));
      await page.route('**/api/auth/me', visitor);
      await page.route('**/api/auth/refresh', visitor);
      await page.route('**/api/auth/providers', (route) => route.fulfill({ json: { google: false } }));
      await serveLaunchPhase(page, 3);
      await use(calls);
    },
    { auto: true },
  ],
});

export { expect };

/** One catalog card as GET /api/catalog/listings returns it; n picks the reference and title. */
/** The catalog cards cycle through the categories: [category, subcategory, title]. */
const cardKinds = [
  ['Clothing', 'Dresses', 'Floral party dress'],
  ['Shoes', 'Sneakers', 'Glitter sneakers'],
  ['Dance', 'Tutus', 'Pink tulle tutu'],
  ['Sports', 'Soccer', 'Pink shin guards'],
  ['Cameras and tech', 'Cameras', 'Instant camera'],
  ['Toys and crafts', 'Dolls', 'Rag doll'],
] as const;

export function catalogCard(n: number, fields: Record<string, unknown> = {}): Record<string, unknown> {
  const [category, subcategory, name] = cardKinds[(n - 1) % cardKinds.length];
  const hash = String(n).padStart(64, 'c');
  const srcset = (ext: string) =>
    `/photos/${hash}/400.${ext} 400w, /photos/${hash}/800.${ext} 800w, /photos/${hash}/1600.${ext} 1200w`;
  return {
    id: `00000000-0000-0000-0000-${String(n).padStart(12, '0')}`,
    reference: `L-${10000 + n}`,
    title: `${name} no. ${n}`,
    brand: 'Gymboree',
    category,
    subcategory,
    sizeLabel: 'Girls',
    condition: 'Used - Excellent',
    price: '85.00',
    status: 'Active',
    publishedAt: '2026-10-25T12:00:00Z',
    coverPhoto: {
      width: 1200,
      height: 1600,
      alt: `Photo 1 of Gymboree ${name} no. ${n}`,
      src: `/photos/${hash}/400.jpg`,
      webp: srcset('webp'),
      jpeg: srcset('jpg'),
    },
    ...fields,
  };
}

/** A catalog page of cards numbered from (page - 1) * 24 + 1, out of total. */
export function catalogPage(total: number, page = 1, pageSize = 24): Record<string, unknown> {
  const first = (page - 1) * pageSize + 1;
  const count = Math.max(0, Math.min(pageSize, total - first + 1));
  return { items: Array.from({ length: count }, (_, i) => catalogCard(first + i)), page, pageSize, totalCount: total };
}

/**
 * Answers GET /api/catalog/listings with pages of `total` cards and serves the photos. Returns
 * the query strings of the requests, in order.
 */
export async function serveCatalog(page: Page, total: number): Promise<URLSearchParams[]> {
  const requests: URLSearchParams[] = [];
  await page.route('**/api/catalog/listings**', (route) => {
    const query = new URL(route.request().url()).searchParams;
    requests.push(query);
    return route.fulfill({ json: catalogPage(total, Number(query.get('page') ?? 1)) });
  });
  await page.route('**/api/catalog/brands', (route) => route.fulfill({ json: catalogBrands }));
  await page.route('**/photos/**', (route) => route.fulfill({ contentType: 'image/png', body: PIXEL }));
  return requests;
}

/** GET /api/catalog/brands: 8 brands, most listed first. */
export const catalogBrands = [
  { name: 'Gymboree', listingCount: 212 },
  { name: 'Crewcuts', listingCount: 180 },
  { name: 'Hanna Andersson', listingCount: 96 },
  { name: 'Primary', listingCount: 54 },
  { name: 'Mini Boden', listingCount: 41 },
  { name: 'Mini Boden', listingCount: 30 },
  { name: 'Janie and Jack', listingCount: 12 },
  { name: 'Birkenstock', listingCount: 9 },
];

/**
 * Answers the search-and-filter calls: GET /api/catalog/listings with `total` cards (0 shows the
 * no-results state), /listings/count with `count`, and /brands. Returns the listing and count
 * query strings, in order.
 */
export async function serveSearch(
  page: Page,
  total: number,
  count = total,
): Promise<{ listings: URLSearchParams[]; counts: URLSearchParams[] }> {
  const listings = await serveCatalog(page, total);
  const counts: URLSearchParams[] = [];
  await page.route('**/api/catalog/listings/count**', (route) => {
    counts.push(new URL(route.request().url()).searchParams);
    return route.fulfill({ json: { totalCount: count } });
  });
  return { listings, counts };
}

/** A listing as GET /api/catalog/listings/{reference} returns it, seen by another member. */
export function listingDetail(fields: Record<string, unknown> = {}, photos = 3): Record<string, unknown> {
  const photo = (i: number) => {
    const hash = String(i).padStart(64, 'd');
    const srcset = (ext: string) =>
      `/photos/${hash}/400.${ext} 400w, /photos/${hash}/800.${ext} 800w, /photos/${hash}/1600.${ext} 1200w`;
    return {
      width: 1200,
      height: 1600,
      alt: `Photo ${i} of Gymboree Floral party dress, pink`,
      src: `/photos/${hash}/400.jpg`,
      webp: srcset('webp'),
      jpeg: srcset('jpg'),
    };
  };
  return {
    id: '00000000-0000-0000-0000-000000010001',
    reference: 'L-10001',
    title: 'Floral party dress, pink',
    brand: 'Gymboree',
    category: 'Clothing',
    subcategory: 'Dresses',
    sizeLabel: 'Girls',
    condition: 'Used - Excellent',
    flaws: null,
    description: 'Worn a handful of times to parties. No stains, tulle still full. Original gift box included.',
    price: '85.00',
    shippingCost: '9.00',
    total: '94.00',
    status: 'Active',
    publishedAt: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(),
    photos: Array.from({ length: photos }, (_, i) => photo(i + 1)),
    seller: { displayName: 'tutu_tessa', completedSalesCount: 0, ratingAverage: null, publishedReviewCount: 0 },
    viewer: { relation: 'OtherMember' },
    showcase: false,
    ...fields,
  };
}

/** Answers GET /api/catalog/listings/L-10001 with this body and serves the photos. */
export async function serveListing(page: Page, body: Record<string, unknown>): Promise<void> {
  await page.route('**/api/catalog/listings/L-10001', (route) => route.fulfill({ json: body }));
  await page.route('**/photos/**', (route) => route.fulfill({ contentType: 'image/png', body: PIXEL }));
}

/** The saved address of marcus_12 as TuTuMuch API returns it. */
export const brooklyn = {
  fullName: 'Marcus Bell',
  line1: '214 Kent Ave',
  line2: 'Apt 3R',
  city: 'Brooklyn',
  state: 'NY',
  zip: '11249',
  country: 'US',
};

/** When the fixtures' checkouts start; tests install the page clock at this time. */
export const checkoutStart = new Date('2026-10-25T12:00:00Z');

/** A checkout of L-10001 as /api/checkouts returns it: 9:41 left, priced for the saved address. */
export function checkout(fields: Record<string, unknown> = {}): Record<string, unknown> {
  const hash = '1'.padStart(64, 'd');
  return {
    id: 'c-1',
    status: 'Open',
    expiresAt: new Date(checkoutStart.getTime() + (9 * 60 + 41) * 1000).toISOString(),
    item: {
      reference: 'L-10001',
      title: 'Floral party dress, pink',
      brand: 'Gymboree',
      size: "Girls'",
      condition: 'Used - Excellent',
      photo: {
        width: 1200,
        height: 1600,
        alt: '',
        src: `/photos/${hash}/400.jpg`,
        webp: `/photos/${hash}/400.webp 400w`,
        jpeg: `/photos/${hash}/400.jpg 400w`,
      },
    },
    itemPrice: '85.00',
    shippingCost: '9.00',
    salesTax: null,
    total: null,
    address: null,
    savedAddress: brooklyn,
    lastDeclineReason: null,
    orderId: null,
    ...fields,
  };
}

/** The checkout once an address is set: $8.34 tax, $102.34 total. */
export function pricedCheckout(fields: Record<string, unknown> = {}): Record<string, unknown> {
  return checkout({ address: brooklyn, salesTax: '8.34', total: '102.34', ...fields });
}

/** GET /api/purchases/o-1. */
export const purchase = {
  id: 'o-1',
  reference: 'TM-48377',
  status: 'Paid',
  item: checkout()['item'],
  total: '102.34',
  cardBrand: 'Visa',
  cardLast4: '4242',
  sellerDisplayName: 'tutu_tessa',
  paidAt: '2026-10-25T12:01:00Z',
};

/** One request the checkout page sent, with its body and headers. */
export interface SentRequest {
  method: string;
  url: string;
  body: unknown;
  headers: Record<string, string>;
}

/**
 * Answers the checkout endpoints for c-1 and serves the photos. start answers POST /api/checkouts;
 * the PUT address answers the priced checkout unless `address` is given; payments answer 202; each
 * GET of the checkout answers the next of `polls` (the last repeats). Returns every request sent.
 */
export async function serveCheckout(
  page: Page,
  options: {
    start?: { status: number; contentType?: string; body: string } | Record<string, unknown>;
    address?: { status: number; contentType: string; body: string };
    pay?: { status: number; contentType: string; body: string };
    polls?: Record<string, unknown>[];
  } = {},
): Promise<SentRequest[]> {
  const sent: SentRequest[] = [];
  const record = (route: Route) => {
    const request = route.request();
    sent.push({
      method: request.method(),
      url: request.url(),
      body: request.postDataJSON(),
      headers: request.headers(),
    });
  };
  const polls = [...(options.polls ?? [pricedCheckout({ status: 'Paid', orderId: 'o-1' })])];
  await page.route('**/api/checkouts', (route) => {
    record(route);
    const start = options.start ?? checkout();
    return route.fulfill('status' in start && 'body' in start ? (start as never) : { status: 201, json: start });
  });
  await page.route('**/api/checkouts/c-1/address', (route) => {
    record(route);
    return route.fulfill(options.address ?? { json: pricedCheckout() });
  });
  await page.route('**/api/checkouts/c-1/payments', (route) => {
    record(route);
    return route.fulfill(options.pay ?? { status: 202, json: { id: 'p-1', status: 'Pending' } });
  });
  await page.route('**/api/checkouts/c-1', (route) => {
    record(route);
    if (route.request().method() === 'DELETE') return route.fulfill({ status: 204 });
    const next = polls.length > 1 ? polls.shift()! : polls[0];
    return route.fulfill({ json: next });
  });
  await page.route('**/photos/**', (route) => route.fulfill({ contentType: 'image/png', body: PIXEL }));
  return sent;
}

/** The order timeline as the order endpoints return it: Paid on Sat 24 Oct, later steps as given. */
export function timeline(shipped: string | null = null, delivered: string | null = null): Record<string, unknown>[] {
  return [
    { status: 'Paid', at: '2026-10-24T15:00:00Z' },
    { status: 'Shipped', at: shipped },
    { status: 'Delivered', at: delivered },
    { status: 'Completed', at: null },
  ];
}

export const uspsTracking = {
  carrier: 'USPS',
  number: '9400111899223456120477',
  url: 'https://tools.usps.com/go/TrackConfirmAction?tLabels=9400111899223456120477',
};

/** GET /api/sales/o-1: a Paid sale of the unicorn hoodie to twirly_tara, ship by Fri 30 Oct (mocks pages/sale-detail). */
export function sale(fields: Record<string, unknown> = {}): Record<string, unknown> {
  return {
    id: 'o-1',
    reference: 'TM-48302',
    status: 'Paid',
    paidAt: '2026-10-24T15:00:00Z',
    shipBy: '2026-10-30T15:00:00Z',
    item: {
      ...(checkout()['item'] as object),
      title: 'Unicorn hoodie, lilac',
      brand: 'Champion',
      size: "Girls'",
    },
    itemPrice: '38.00',
    shippingCost: '7.00',
    salesTax: '3.15',
    total: '48.15',
    buyerDisplayName: 'twirly_tara',
    shipTo: {
      fullName: 'Tom Reyes',
      line1: '1450 Larimer St',
      line2: 'Apt 6B',
      city: 'Denver',
      state: 'CO',
      zip: '80202',
      country: 'US',
    },
    timeline: timeline(),
    tracking: null,
    payout: null,
    cancellation: null,
    cancellationRequest: null,
    canRelist: false,
    problem: null,
    ...fields,
  };
}

/** GET /api/purchases/o-1 as the buyer's order page reads it: the Paid order from tutu_tessa. */
export function buyerOrder(fields: Record<string, unknown> = {}): Record<string, unknown> {
  return {
    ...purchase,
    paidAt: '2026-10-24T15:00:00Z',
    shipBy: '2026-10-30T15:00:00Z',
    itemPrice: '85.00',
    shippingCost: '9.00',
    salesTax: '8.34',
    timeline: timeline(),
    tracking: null,
    autoCompletesAt: null,
    cancellation: null,
    cancellationRequest: null,
    canReportProblem: false,
    problemDeadline: null,
    problem: null,
    ...fields,
  };
}

/** A buyer's open problem as the order, sale and admin endpoints return it (mocks order-detail/problem). */
export function orderProblem(fields: Record<string, unknown> = {}): Record<string, unknown> {
  return {
    reference: 'P-1001',
    reason: 'WrongSize',
    reasonLabel: 'Wrong size',
    description: 'Label inside says 11.5, not 12.',
    photos: [{ id: 'ph-1', url: '/api/orders/o-1/problem/photos/ph-1' }],
    status: 'Open',
    openedAt: '2026-10-24T18:00:00Z',
    sellerResponse: null,
    sellerRespondedAt: null,
    outcome: null,
    resolutionReason: null,
    resolvedAt: null,
    ...fields,
  };
}

/** GET /api/admin/problems/P-1001: the problem on the Delivered order TM-48140 (mocks pages/problem-detail). */
export function problemDetail(fields: Record<string, unknown> = {}): Record<string, unknown> {
  return {
    problem: orderProblem({
      reason: 'NotAsDescribed',
      reasonLabel: 'Not as described',
      description: 'Pilling all over the front and a small hole by the left cuff.',
      sellerResponse: 'It left here without the hole. Happy to take it back.',
      sellerRespondedAt: '2026-10-25T10:00:00Z',
    }),
    buyerDisplayName: 'marcus_12',
    sellerDisplayName: 'vintage_lou',
    order: {
      id: 'o-1',
      reference: 'TM-48140',
      status: 'Delivered',
      total: '59.88',
      deliveredAt: '2026-10-24T15:00:00Z',
      item: { ...(sale()['item'] as object), title: 'Better Sweater fleece', brand: 'Primary' },
    },
    payout: 'Held',
    ...fields,
  };
}

/** The buyer's request to cancel as the API returns it, Pending unless <status> says otherwise. */
export function cancellationRequest(status = 'Pending'): Record<string, unknown> {
  return { message: 'Ordered the wrong colour, sorry. Can we cancel?', status, requestedAt: '2026-10-24T18:00:00Z' };
}

/** One row of the payouts history: the unicorn hoodie sold as TM-48302, held until the order completes. */
export function payoutRow(fields: Record<string, unknown> = {}): Record<string, unknown> {
  return {
    orderId: 'o-1',
    orderReference: 'TM-48302',
    item: (sale()['item'] as object) ?? {},
    amount: '40.50',
    fee: '4.50',
    status: 'Held',
    holdReason: 'OrderNotCompleted',
    paidAt: null,
    ...fields,
  };
}

/** The shorts sold as TM-47511 and paid out on Mon 5 Oct (mocks pages/payouts/default). */
export const paidPayoutRow = payoutRow({
  orderId: 'o-2',
  orderReference: 'TM-47511',
  item: { ...(sale()['item'] as object), title: 'Tech Fleece shorts', brand: 'Gymboree' },
  amount: '33.30',
  fee: '3.70',
  status: 'Paid',
  holdReason: null,
  paidAt: '2026-10-05T16:00:00Z',
});

/** GET /api/payouts for an onboarded seller with one held and one paid payout (mocks pages/payouts/default). */
export function payoutSummary(fields: Record<string, unknown> = {}): Record<string, unknown> {
  return {
    paidOut: '33.30',
    pending: '40.50',
    onboarding: 'Complete',
    bankDisplay: 'Chase •••• 6021',
    banner: null,
    history: [payoutRow(), paidPayoutRow],
    ...fields,
  };
}

/** One row of GET /api/purchases or /api/sales: the unicorn hoodie paid on Sat 24 Oct, from or to twirly_tara. */
export function orderRow(fields: Record<string, unknown> = {}): Record<string, unknown> {
  return {
    id: 'o-1',
    reference: 'TM-48302',
    status: 'Paid',
    item: (sale()['item'] as object) ?? {},
    total: '48.15',
    otherParty: 'twirly_tara',
    paidAt: '2026-10-24T15:00:00Z',
    shipBy: null,
    ...fields,
  };
}

/** GET /api/purchases or /api/sales: the rows newest first and the counts the subtitle reads. */
export function orderList(
  items: Record<string, unknown>[],
  counts: Record<string, number> = {},
): Record<string, unknown> {
  return { items, counts: { onTheWay: 0, toConfirm: 0, toShip: 0, ...counts } };
}

/** One row of GET /api/me/listings: the Active sandals with a cover photo, first saved Wed 21 Oct. */
export function myListingRow(fields: Record<string, unknown> = {}): Record<string, unknown> {
  const hash = 'a'.repeat(64);
  return {
    reference: 'L-10021',
    status: 'Active',
    title: 'Arizona sandals',
    category: 'Shoes',
    price: 45,
    photo: {
      width: 1200,
      height: 1200,
      alt: '',
      src: `/photos/${hash}/400.jpg`,
      webp: `/photos/${hash}/400.webp 400w`,
      jpeg: `/photos/${hash}/400.jpg 400w`,
    },
    createdAt: '2026-10-21T15:00:00Z',
    saleOrderId: null,
    ...fields,
  };
}

/** GET /api/me/listings: the rows newest first and the count in each status. */
export function myListings(
  items: Record<string, unknown>[],
  counts: Record<string, number> = {},
): Record<string, unknown> {
  return { items, counts: { all: 0, draft: 0, active: 0, reserved: 0, sold: 0, removed: 0, ...counts } };
}

/** GET /api/members/{displayName}: a public profile with no reviews (review authoring is deferred). */
export function memberProfile(fields: Record<string, unknown> = {}): Record<string, unknown> {
  return {
    memberId: '7d0c1a52-3b9e-4f61-9a3d-2c8e5f4b1a07',
    displayName: 'tutu_tessa',
    avatarUrl: null,
    bio: null,
    publicLocation: null,
    memberSince: '2025-01-01',
    rating: { average: null, count: 0 },
    completedSales: 141,
    activeListingCount: 4,
    ...fields,
  };
}

/**
 * Answers the profile and its listings pages of `total` Active listings. Returns the requested
 * listing page numbers, in order.
 */
export async function serveProfile(page: Page, profile: Record<string, unknown>, total = 4): Promise<number[]> {
  const pages: number[] = [];
  const name = profile['displayName'] as string;
  await page.route(`**/api/members/${name}`, (route) => route.fulfill({ json: profile }));
  await page.route(`**/api/members/${name}/listings**`, (route) => {
    const n = Number(new URL(route.request().url()).searchParams.get('page') ?? '1');
    pages.push(n);
    return route.fulfill({ json: catalogPage(total, n) });
  });
  await page.route('**/photos/**', (route) => route.fulfill({ status: 404 }));
  return pages;
}

/** One item request as GET /api/me/item-requests returns it (L2-099): open, no match yet. */
export function itemRequest(fields: Record<string, unknown> = {}): Record<string, unknown> {
  return {
    id: '7d3c0f2e-0000-4000-8000-000000000001',
    category: 'Dance',
    size: "Girls'",
    keywords: 'ballet slippers',
    maxPrice: 20,
    minimumCondition: 'Used - Good',
    createdAt: '2026-10-20T12:00:00Z',
    updatedAt: '2026-10-20T12:00:00Z',
    expiresAt: '2027-01-18T12:00:00Z',
    status: 'Open',
    matchCount: 0,
    matches: [],
    ...fields,
  };
}

/** GET /api/me/item-requests: the requests, how many are open and the limit of 20 (L2-098). */
export function myItemRequests(items: Record<string, unknown>[], openCount = items.length): Record<string, unknown> {
  return { items, openCount, limit: 20 };
}

/** The member's request for the showcase listing L-10001 (L2-109). */
export function showcaseRequest(fields: Record<string, unknown> = {}): Record<string, unknown> {
  return {
    listingReference: 'L-10001',
    note: 'Size 7 if you find one, for her spring recital.',
    requestedAt: '2026-10-24T15:00:00Z',
    updatedAt: '2026-10-24T15:00:00Z',
    ...fields,
  };
}

/** A request on the administrator's list at /admin/requests (L2-110). */
export function adminShowcaseRequest(fields: Record<string, unknown> = {}): Record<string, unknown> {
  return {
    listingReference: 'L-10001',
    listingTitle: 'Gold sequin recital tutu',
    memberDisplayName: 'tutu_tessa',
    memberEmail: 'tessa.bell@example.com',
    note: 'Size 7 if you find one, for her spring recital.',
    requestedAt: '2026-10-25T09:12:00Z',
    ...fields,
  };
}
