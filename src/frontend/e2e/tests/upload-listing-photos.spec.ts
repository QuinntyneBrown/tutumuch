// Acceptance Test
// Traces to: L2-017, L2-065
// Description: A seller adds up to 8 photos to a listing, reorders them by dragging or with the
// arrow keys, and removes them; the first is the cover. Files over 10 MB and files TuTuMuch API
// refuses show a rejected tile and a clear message. A save sends the photo IDs in order.

import { Page, Request } from '@playwright/test';
import {
  expect,
  imageFile,
  marcus,
  myListing,
  photo,
  problem,
  serveCatalogue,
  servePhotoUploads,
  signInAs,
  test,
} from '../fixtures';
import { SellPage } from '../pages/sell.page';

test.describe('upload listing photos', () => {
  let uploaded: string[];

  test.beforeEach(async ({ page }) => {
    await signInAs(page, marcus);
    await serveCatalogue(page);
    uploaded = await servePhotoUploads(page);
  });

  /** Answers draft saves and returns the last request body. */
  async function captureSaves(page: Page): Promise<() => Record<string, unknown> | null> {
    let last: Request | null = null;
    await page.route('**/api/listings', async (route) => {
      last = route.request();
      await route.fulfill({ status: 201, json: myListing(route.request().postDataJSON()) });
    });
    return () => (last ? (last as Request).postDataJSON() : null);
  }

  test('chosen photos upload as tiles with the cover on the first', async ({ page }) => {
    const sell = new SellPage(page);
    await sell.goto();

    await expect(page.getByRole('heading', { name: 'Photos' })).toBeVisible();
    await expect(sell.addPhotosTile).toContainText('Add photos');
    await expect(sell.addPhotosTile).toContainText('0 of 8');
    await expect(
      page.getByText('JPEG, PNG or WebP up to 10 MB. Drag to reorder; the first is the cover.'),
    ).toBeVisible();

    await sell.addPhotos('front.jpg', 'side.png');

    expect(uploaded).toEqual(['front.jpg', 'side.png']);
    await expect(sell.photo(1).getByRole('img', { name: 'Photo 1' })).toBeVisible();
    await expect(sell.photo(1)).toContainText('Cover');
    await expect(sell.photo(2)).not.toContainText('Cover');
    await expect(sell.addPhotosTile).toContainText('2 of 8');
  });

  test('a file over 10 MB is refused before upload with its name, size and a message', async ({ page }) => {
    const sell = new SellPage(page);
    await sell.goto();

    await sell.photoInput.setInputFiles([imageFile('IMG_4471.jpg', Math.round(11.4 * 1024 * 1024))]);

    await expect(sell.rejectedTiles).toHaveCount(1);
    await expect(sell.rejectedTiles).toContainText('IMG_4471.jpg');
    await expect(sell.rejectedTiles).toContainText('11.4 MB');
    await expect(sell.photoError).toHaveText('Photos must be 10 MB or smaller');
    await expect(sell.photoInput).toHaveAttribute('aria-invalid', 'true');
    expect(uploaded).toEqual([]);

    await page.getByRole('button', { name: 'Remove IMG_4471.jpg' }).click();
    await expect(sell.rejectedTiles).toHaveCount(0);
    await expect(sell.photoError).toHaveCount(0);
  });

  test('a file TuTuMuch API refuses shows the reason it gives', async ({ page }) => {
    await page.route('**/api/photo-uploads', (route) =>
      route.fulfill(problem(415, 'PHOTO_TYPE_UNSUPPORTED', 'Use a JPEG, PNG or WebP photo')),
    );
    const sell = new SellPage(page);
    await sell.goto();

    await sell.photoInput.setInputFiles([imageFile('photo.jpg')]);

    await expect(sell.rejectedTiles).toContainText('photo.jpg');
    await expect(sell.photoError).toHaveText('Use a JPEG, PNG or WebP photo');
    await expect(sell.addPhotosTile).toContainText('0 of 8');
  });

  test('at 8 photos the add control is disabled and extra files are not uploaded', async ({ page }) => {
    const sell = new SellPage(page);
    await sell.goto();

    await sell.photoInput.setInputFiles(Array.from({ length: 9 }, (_, i) => imageFile(`p${i + 1}.jpg`)));

    await expect(sell.photoTiles.locator('img')).toHaveCount(8);
    expect(uploaded).toHaveLength(8);
    await expect(sell.addPhotosTile).toContainText('8 of 8');
    await expect(sell.photoInput).toBeDisabled();
  });

  test('dragging the third photo to the front makes it the cover and the save keeps the order', async ({ page }) => {
    const lastSave = await captureSaves(page);
    const sell = new SellPage(page);
    await sell.goto();
    await sell.addPhotos('a.jpg', 'b.jpg', 'c.jpg');

    await sell.photo(3).dragTo(sell.photo(1));

    await expect(sell.photo(1).locator('img')).toHaveAttribute('src', photo('p-3')['url'] as string);
    await sell.category.selectOption('Clothing');
    await sell.saveDraft.click();
    await expect.poll(lastSave).toMatchObject({ photoIds: ['p-3', 'p-1', 'p-2'] });
    await expect(sell.photo(1)).toContainText('Cover');
  });

  test('the arrow keys move a focused photo and the new position is announced', async ({ page }) => {
    const lastSave = await captureSaves(page);
    const sell = new SellPage(page);
    await sell.goto();
    await sell.addPhotos('a.jpg', 'b.jpg');

    await sell.photo(2).focus();
    await page.keyboard.press('ArrowLeft');

    await expect(sell.photoAnnouncement).toHaveText('Photo moved to position 1 of 2. It is now the cover.');
    await expect(sell.photo(1)).toBeFocused();
    await sell.category.selectOption('Shoes');
    await sell.saveDraft.click();
    await expect.poll(lastSave).toMatchObject({ photoIds: ['p-2', 'p-1'] });
  });

  test('a removed photo leaves the listing', async ({ page }) => {
    const lastSave = await captureSaves(page);
    const sell = new SellPage(page);
    await sell.goto();
    await sell.addPhotos('a.jpg', 'b.jpg');

    await page.getByRole('button', { name: 'Remove photo 1' }).click();

    await expect(sell.photoTiles).toHaveCount(1);
    await expect(sell.addPhotosTile).toContainText('1 of 8');
    await expect(sell.photo(1)).toContainText('Cover');
    await sell.category.selectOption('Shoes');
    await sell.saveDraft.click();
    await expect.poll(lastSave).toMatchObject({ photoIds: ['p-2'] });
  });

  test('publishing with no photos is refused with add at least one photo', async ({ page, unfixturedApiCalls }) => {
    const sell = new SellPage(page);
    await sell.goto();

    await sell.publish.click();

    await expect(sell.errorSummary.getByRole('link', { name: 'Add at least one photo' })).toBeVisible();
    await expect(sell.photoError).toHaveText('Add at least one photo');
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('a saved draft opens with its photos in order', async ({ page }) => {
    await page.route('**/api/me/listings/L-10001', (route) =>
      route.fulfill({ json: myListing({ category: 'Shoes', photos: [photo('p-7'), photo('p-8')] }) }),
    );
    const sell = new SellPage(page);

    await sell.goto('/sell/L-10001');

    await expect(sell.photoTiles).toHaveCount(2);
    await expect(sell.photo(1)).toContainText('Cover');
    await expect(sell.addPhotosTile).toContainText('2 of 8');
  });
});
