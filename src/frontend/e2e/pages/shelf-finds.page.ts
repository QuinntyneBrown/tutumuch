import { Locator, Page } from '@playwright/test';

/** Shelf finds at /finds (mocks pages/shelf-finds). */
export class ShelfFindsPage {
  readonly heading: Locator;
  readonly cards: Locator;
  readonly loadMore: Locator;
  readonly post: Locator;
  readonly empty: Locator;
  readonly tryAgain: Locator;

  constructor(private readonly page: Page) {
    const main = page.locator('main');
    this.heading = main.getByRole('heading', { level: 1 });
    this.cards = main.locator('.find-grid .find-card');
    this.loadMore = main.getByRole('button', { name: /Load more/ });
    this.post = main.locator('.page-header').getByRole('link', { name: 'Post a shelf find' });
    this.empty = main.locator('.empty');
    this.tryAgain = main.getByRole('button', { name: 'Try again' });
  }

  async goto(): Promise<void> {
    await this.page.goto('/finds');
  }
}

/** One shelf find at /finds/{reference} (mocks pages/find-detail). */
export class FindDetailPage {
  readonly heading: Locator;
  readonly price: Locator;
  readonly maps: Locator;
  readonly notice: Locator;
  readonly grabBox: Locator;
  readonly grabButton: Locator;
  readonly markGone: Locator;
  readonly withdraw: Locator;
  readonly soldNotice: Locator;
  readonly centerState: Locator;
  readonly dialog: Locator;

  constructor(private readonly page: Page) {
    const main = page.locator('main');
    this.heading = main.getByRole('heading', { level: 1 });
    this.price = main.locator('.detail__price');
    this.maps = main.getByRole('link', { name: /Open in maps/ });
    this.notice = main.locator('.alert', { hasText: 'Prices and stock change' });
    this.grabBox = main.getByRole('region', { name: 'Grab it for me' });
    this.grabButton = main.getByRole('link', { name: 'Grab it for me' });
    this.markGone = main.getByRole('button', { name: 'Mark as gone' });
    this.withdraw = main.getByRole('button', { name: 'Withdraw' });
    this.soldNotice = main.locator('.sold-notice');
    this.centerState = main.locator('.center-state');
    this.dialog = page.getByRole('dialog');
  }

  async goto(reference = 'F-1001'): Promise<void> {
    await this.page.goto(`/finds/${reference}`);
  }
}

/** Post a shelf find at /finds/new (mocks pages/post-find). */
export class PostFindPage {
  readonly heading: Locator;
  readonly title: Locator;
  readonly category: Locator;
  readonly price: Locator;
  readonly store: Locator;
  readonly address: Locator;
  readonly seenOn: Locator;
  readonly note: Locator;
  readonly offerGrab: Locator;
  readonly grabFee: Locator;
  readonly submit: Locator;
  readonly errorSummary: Locator;
  readonly centerState: Locator;

  constructor(private readonly page: Page) {
    const main = page.locator('main');
    this.heading = main.getByRole('heading', { level: 1 });
    this.title = page.getByLabel('Title');
    this.category = page.getByLabel('Category');
    this.price = page.getByLabel('Shelf price');
    this.store = page.getByLabel('Store name');
    this.address = page.getByLabel('Store address');
    this.seenOn = page.getByLabel('Seen on');
    this.note = page.getByLabel('Note');
    this.offerGrab = page.getByRole('switch', { name: 'Offer to grab it' });
    this.grabFee = page.getByLabel('Grab fee');
    this.submit = page.getByRole('button', { name: 'Post find' });
    this.errorSummary = page.locator('.form-summary');
    this.centerState = main.locator('.center-state');
  }

  async goto(): Promise<void> {
    await this.page.goto('/finds/new');
  }
}

/** "Grab it for me" at /finds/{reference}/grab (mocks pages/grab). */
export class GrabPage {
  readonly heading: Locator;
  readonly fullName: Locator;
  readonly street: Locator;
  readonly city: Locator;
  readonly state: Locator;
  readonly zip: Locator;
  readonly country: Locator;
  readonly submit: Locator;
  readonly summary: Locator;
  readonly depositNote: Locator;
  readonly acknowledge: Locator;
  readonly testCard: Locator;
  readonly formAlert: Locator;
  readonly errorSummary: Locator;
  readonly centerState: Locator;

  constructor(private readonly page: Page) {
    const main = page.locator('main');
    this.heading = main.getByRole('heading', { level: 1 });
    this.fullName = page.getByLabel('Full name');
    this.street = page.getByLabel('Street address');
    this.city = page.getByLabel('City');
    this.state = page.getByLabel('State');
    this.zip = page.getByLabel('ZIP code');
    this.country = page.getByLabel('Country');
    this.submit = main.getByRole('button', { name: /^Pay|^See the total/ });
    this.summary = page.getByRole('complementary', { name: 'Summary' });
    this.depositNote = main.locator('.alert', { hasText: 'How the deposit works' });
    this.acknowledge = page.getByRole('checkbox', { name: /deposit is non-refundable/ });
    this.testCard = page.getByLabel('Test card');
    this.formAlert = page.locator('form .alert').first();
    this.errorSummary = page.locator('.form-summary');
    this.centerState = main.locator('.center-state');
  }

  async goto(reference = 'F-1001'): Promise<void> {
    await this.page.goto(`/finds/${reference}/grab`);
  }

  async fillBrooklyn(): Promise<void> {
    await this.fullName.fill('Marcus Bell');
    await this.street.fill('214 Kent Ave');
    await this.city.fill('Brooklyn');
    await this.state.selectOption('NY');
    await this.zip.fill('11249');
  }
}

/** The shopper's grabs at /account/grabs and one grab at /account/grabs/{id} (mocks pages/my-grabs, pages/grab-detail). */
export class MyGrabsPage {
  readonly heading: Locator;
  readonly rows: Locator;
  readonly empty: Locator;
  readonly status: Locator;
  readonly cancel: Locator;
  readonly received: Locator;
  readonly summary: Locator;
  readonly centerState: Locator;
  readonly dialog: Locator;

  constructor(private readonly page: Page) {
    const main = page.locator('main');
    this.heading = main.getByRole('heading', { level: 1 });
    this.rows = main.locator('.order-list > li');
    this.empty = main.locator('.empty');
    this.status = main.locator('.alert').first();
    this.cancel = main.getByRole('button', { name: 'Cancel grab' });
    this.received = main.getByRole('button', { name: 'I’ve got it' });
    this.summary = page.getByRole('complementary', { name: 'Summary' });
    this.centerState = main.locator('.center-state');
    this.dialog = page.getByRole('dialog');
  }

  async goto(id?: string): Promise<void> {
    await this.page.goto(id ? `/account/grabs/${id}` : '/account/grabs');
  }
}

/** The administrator's grabs at /admin/grabs (mocks pages/admin-grabs). */
export class AdminGrabsPage {
  readonly heading: Locator;
  readonly subtitle: Locator;
  readonly grabs: Locator;
  readonly empty: Locator;
  readonly state: Locator;
  readonly dialog: Locator;

  constructor(private readonly page: Page) {
    const main = page.locator('main');
    this.heading = main.getByRole('heading', { level: 1 });
    this.subtitle = main.locator('.page-header .subtitle');
    this.grabs = main.locator('.request-list > li');
    this.empty = main.locator('.empty');
    this.state = main.locator('.center-state');
    this.dialog = page.getByRole('dialog');
  }

  async goto(): Promise<void> {
    await this.page.goto('/admin/grabs');
  }
}
