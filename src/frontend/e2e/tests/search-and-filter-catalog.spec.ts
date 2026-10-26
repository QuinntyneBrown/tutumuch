// Acceptance Test
// Traces to: L2-023, L2-024, L2-062
// Description: A visitor searches the catalog from the top bar, narrows it with the filter
// sidebar (992px and wider) or the filter sheet (full screen under 768px, side panel to 991px),
// sorts it, and reopens the same result set from its URL; a search that matches nothing offers
// to clear the search and filters.

import { expect, serveSearch, test } from '../fixtures';
import { CatalogFilters } from '../pages/catalog-filters.page';
import { CatalogPage } from '../pages/catalog.page';

test.describe('search and filter catalog', () => {
  test('a keyword from the top bar searches the catalog and heads the page', async ({ page, unfixturedApiCalls }) => {
    const { listings } = await serveSearch(page, 2);
    const catalog = new CatalogPage(page);
    const filters = new CatalogFilters(page);
    await catalog.goto();

    await filters.search('  Air Max  ');

    await expect(page).toHaveURL('/?q=Air%20Max');
    await expect(filters.eyebrow).toHaveText('Search');
    await expect(catalog.heading).toHaveText('“Air Max”');
    await expect(catalog.count).toHaveText('2 items');
    expect(listings.at(-1)?.get('q')).toBe('Air Max');
    await expect(filters.searchBox).toHaveValue('Air Max');
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('a keyword longer than 100 characters is cut to 100 before searching', async ({ page }) => {
    const { listings } = await serveSearch(page, 1);
    const filters = new CatalogFilters(page);
    await new CatalogPage(page).goto();

    await filters.search('a'.repeat(120));

    await expect.poll(() => listings.at(-1)?.get('q')).toBe('a'.repeat(100));
  });

  test('the sidebar is shown from 992px and its filters combine into one URL that survives reload', async ({
    page,
  }) => {
    const { listings } = await serveSearch(page, 2);
    const catalog = new CatalogPage(page);
    const filters = new CatalogFilters(page);
    await page.setViewportSize({ width: 1280, height: 900 });
    await catalog.goto();

    await expect(filters.sidebarRoot).toBeVisible();
    await expect(filters.toggle).toBeHidden();
    await filters.sidebar.option('Category', 'Shoes').check();
    await expect(page).toHaveURL('/?category=shoes');
    await filters.sidebar.option('Shoes', 'Boots').check();
    await filters.sidebar.option('Brand', 'Nike').check();
    await filters.sidebar.option('Brand', 'Adidas').check();
    await filters.sidebar.option('Condition', 'New with tags').check();
    await filters.sidebar.minPrice.fill('20');
    await filters.sidebar.maxPrice.fill('200');
    await filters.sort.selectOption({ label: 'Price: low to high' });

    const url =
      '/?category=shoes&subcategory=boots&brand=Nike&brand=Adidas&condition=new-with-tags&min=20&max=200&sort=price-asc';
    await expect(page).toHaveURL(url);
    await expect(catalog.heading).toHaveText('Shop');
    await expect(filters.chips).toHaveText(['Shoes', 'Boots', 'Nike', 'Adidas', 'New with tags', '$20–$200']);
    const sent = listings.at(-1)!;
    expect(sent.get('category')).toBe('shoes');
    expect(sent.getAll('brand')).toEqual(['Nike', 'Adidas']);
    expect(sent.get('sort')).toBe('price-asc');

    await page.reload();

    await expect(filters.sidebar.option('Category', 'Shoes')).toBeChecked();
    await expect(filters.sidebar.option('Shoes', 'Boots')).toBeChecked();
    await expect(filters.sidebar.option('Brand', 'Adidas')).toBeChecked();
    await expect(filters.sidebar.option('Condition', 'New with tags')).toBeChecked();
    await expect(filters.sidebar.minPrice).toHaveValue('20');
    await expect(filters.sort).toHaveValue('price-asc');
    await expect.poll(() => listings.at(-1)?.toString()).toBe(new URL(url, 'http://x').searchParams.toString());
  });

  test('chips remove one filter and Clear all removes every filter and the keyword', async ({ page }) => {
    await serveSearch(page, 2);
    const filters = new CatalogFilters(page);
    await page.goto('/?q=nike&category=shoes&condition=new-with-tags&min=20&max=200');

    await expect(filters.chips).toHaveText(['Shoes', 'New with tags', '$20–$200']);
    await filters.removeChip('New with tags').click();
    await expect(page).toHaveURL('/?q=nike&category=shoes&min=20&max=200');

    await filters.clearAll.click();
    await expect(page).toHaveURL('/');
  });

  test('the sidebar shows the top 6 brands and expands to all of them', async ({ page }) => {
    await serveSearch(page, 2);
    const filters = new CatalogFilters(page);
    await new CatalogPage(page).goto();

    await expect(filters.sidebar.group('Brand').getByRole('checkbox')).toHaveCount(6);
    await filters.sidebar.allBrands.click();
    await expect(filters.sidebar.group('Brand').getByRole('checkbox')).toHaveCount(8);
  });

  test('the sidebar refuses a minimum price above the maximum', async ({ page }) => {
    const { listings } = await serveSearch(page, 2);
    const catalog = new CatalogPage(page);
    const filters = new CatalogFilters(page);
    await catalog.goto();
    await expect(catalog.count).toHaveText('2 items');

    await filters.sidebar.minPrice.fill('200');
    await filters.sidebar.maxPrice.fill('50');

    await expect(filters.sidebar.priceError).toHaveText('Minimum price must be lower than maximum');
    await expect(filters.sidebar.minPrice).toHaveAttribute('aria-invalid', 'true');
    await page.waitForTimeout(600);
    await expect(page).toHaveURL('/');
    expect(listings).toHaveLength(1);
  });

  test('under 768px Filters opens a full-screen sheet with a live count, Clear and apply', async ({ page }) => {
    const { counts } = await serveSearch(page, 2, 38);
    const filters = new CatalogFilters(page);
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto('/?category=shoes');

    await expect(filters.sidebarRoot).toBeHidden();
    await filters.toggle.click();

    await expect(filters.sheetRoot).toBeVisible();
    const box = (await filters.sheetRoot.boundingBox())!;
    expect(box.width).toBe(390);
    expect(box.height).toBe(844);
    await expect(filters.sheet.option('Category', 'Shoes')).toBeChecked();
    await filters.sheet.option('Condition', 'New with tags').check();
    await expect(filters.sheetApply).toHaveText('Show 38 items');
    expect(counts.at(-1)?.getAll('condition')).toEqual(['new-with-tags']);
    await expect(page).toHaveURL('/?category=shoes');

    await filters.sheetClear.click();
    await expect(filters.sheet.option('Category', 'All')).toBeChecked();
    await expect(filters.sheetRoot).toBeVisible();
    await filters.sheet.option('Category', 'Tops').check();
    await filters.sheetApply.click();

    await expect(filters.sheetRoot).toBeHidden();
    await expect(page).toHaveURL('/?category=tops');
    await expect(filters.toggle).toBeFocused();
  });

  test('the sheet refuses an inverted price range with an error summary', async ({ page }) => {
    await serveSearch(page, 2, 38);
    const filters = new CatalogFilters(page);
    await page.setViewportSize({ width: 390, height: 844 });
    await new CatalogPage(page).goto();
    await filters.toggle.click();

    await filters.sheet.minPrice.fill('200');
    await filters.sheet.maxPrice.fill('50');

    await expect(filters.errorSummary).toContainText('Check the price range');
    await expect(filters.errorSummary.getByRole('link')).toHaveText('Minimum price must be lower than maximum');
    await expect(filters.sheet.minPrice).toHaveAttribute('aria-invalid', 'true');
    await expect(filters.sheetApply).toHaveText('Show items');
    await expect(filters.sheetApply).toBeDisabled();

    await page.keyboard.press('Escape');
    await expect(filters.sheetRoot).toBeHidden();
    await expect(page).toHaveURL('/');
  });

  test('from 768px to 991px the filter sheet is a side panel', async ({ page }) => {
    await serveSearch(page, 2);
    const filters = new CatalogFilters(page);
    await page.setViewportSize({ width: 800, height: 900 });
    await new CatalogPage(page).goto();

    await expect(filters.sidebarRoot).toBeHidden();
    await filters.toggle.click();

    await expect.poll(async () => (await filters.sheetRoot.boundingBox())?.x).toBe(0);
    expect((await filters.sheetRoot.boundingBox())?.width).toBe(384);
    await filters.sheetClose.click();
    await expect(filters.sheetRoot).toBeHidden();
  });

  test('a search that matches nothing offers to clear the search and filters', async ({ page }) => {
    await serveSearch(page, 0);
    const catalog = new CatalogPage(page);
    const filters = new CatalogFilters(page);
    await page.goto('/?q=air%20max%2095&category=outerwear');

    await expect(catalog.count).toHaveText('0 items');
    await expect(filters.noResults).toContainText('No items match');
    await expect(filters.noResults).toContainText('Try fewer filters or a different word.');
    await expect(filters.chips).toHaveText(['Outerwear']);

    await filters.clearSearch.click();
    await expect(page).toHaveURL('/');
  });

  test('a URL with an unknown value is replaced by its canonical form', async ({ page }) => {
    const { listings } = await serveSearch(page, 2);
    await page.goto('/?sort=cheapest&condition=mint&category=tops&subcategory=boots&min=abc');

    await expect(page).toHaveURL('/?category=tops');
    await expect.poll(() => listings.at(-1)?.toString()).toBe('category=tops');
  });
});
