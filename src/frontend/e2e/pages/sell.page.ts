import { Locator, Page, expect } from '@playwright/test';

import { imageFile } from '../fixtures';

/** Sell an item at /sell (mocks pages/sell) and the verify-email-prompt dialog it opens. */
export class SellPage {
  readonly heading: Locator;
  readonly photoInput: Locator;
  readonly addPhotosTile: Locator;
  readonly photoTiles: Locator;
  readonly rejectedTiles: Locator;
  readonly photoError: Locator;
  readonly photoAnnouncement: Locator;
  readonly category: Locator;
  readonly type: Locator;
  readonly fixedSize: Locator;
  readonly title: Locator;
  readonly brand: Locator;
  readonly condition: Locator;
  readonly flaws: Locator;
  readonly description: Locator;
  readonly price: Locator;
  readonly shipping: Locator;
  readonly earnings: Locator;
  readonly confirm: Locator;
  readonly saveDraft: Locator;
  readonly publish: Locator;
  readonly errorSummary: Locator;
  readonly dialog: Locator;

  constructor(private readonly page: Page) {
    this.heading = page.getByRole('heading', { level: 1 });
    this.photoInput = page.locator('#photos');
    this.addPhotosTile = page.locator('.uploader__add');
    this.photoTiles = page.locator('.uploader__tile');
    this.rejectedTiles = page.locator('.uploader__rejected');
    this.photoError = page.locator('#photos-error');
    this.photoAnnouncement = page.locator('tm-photo-uploader [aria-live="polite"]');
    this.category = page.getByLabel('Category');
    this.type = page.getByLabel('Type');
    this.fixedSize = page.locator('#size');
    this.title = page.getByLabel('Title');
    this.brand = page.getByLabel('Brand');
    this.condition = page.getByRole('group', { name: 'Condition' });
    this.flaws = page.getByLabel('Describe the flaws');
    this.description = page.getByLabel('Description');
    this.price = page.locator('#price');
    this.shipping = page.getByLabel('Shipping');
    this.earnings = page.locator('.earnings');
    this.confirm = page.locator('#confirm');
    this.saveDraft = page.getByRole('button', { name: 'Save draft' });
    this.publish = page.getByRole('button', { name: 'Publish' });
    this.errorSummary = page.getByRole('alert').filter({ hasText: 'Fix these to continue' });
    this.dialog = page.getByRole('dialog');
  }

  async goto(path = '/sell'): Promise<void> {
    await this.page.goto(path);
  }

  /** Chooses files in the uploader and waits until every accepted one shows its thumbnail. */
  async addPhotos(...names: string[]): Promise<void> {
    const before = await this.photoTiles.count();
    await this.photoInput.setInputFiles(names.map((name) => imageFile(name)));
    await expect(this.photoTiles.locator('img')).toHaveCount(before + names.length);
  }

  photo(n: number): Locator {
    return this.photoTiles.nth(n - 1);
  }

  /** Needs servePhotoUploads; uploads one photo first. */
  async fillComplete(): Promise<void> {
    await this.addPhotos('front.jpg');
    await this.category.selectOption('Shoes');
    await this.type.selectOption('Sneakers');
    await this.title.fill("Air Force 1 '07, white");
    await this.brand.fill('Nike');
    await this.condition.getByLabel('Used - Good').check();
    await this.description.fill('Light creasing, soles have plenty left. Cleaned and ready to wear.');
    await this.price.fill('60.00');
    await this.shipping.fill('8.00');
    await this.confirm.check();
  }

  fieldError(id: string): Locator {
    return this.page.locator(`#${id}-error`);
  }
}
