import { Locator, Page } from '@playwright/test';

/** One filter form, in the sidebar or the filter sheet: category, subcategory, brand, condition, price. */
export class FilterForm {
  readonly minPrice: Locator;
  readonly maxPrice: Locator;
  readonly priceError: Locator;
  readonly allBrands: Locator;

  constructor(readonly root: Locator) {
    this.minPrice = root.getByLabel('Minimum price in dollars');
    this.maxPrice = root.getByLabel('Maximum price in dollars');
    this.priceError = root.locator('.field__error');
    this.allBrands = root.getByRole('button', { name: /^All \d+ brands$/ });
  }

  group(legend: string): Locator {
    return this.root.getByRole('group', { name: legend, exact: true });
  }

  option(legend: string, label: string): Locator {
    return this.group(legend).getByLabel(label, { exact: true });
  }
}

/** The search box, filter sidebar, filter sheet, toolbar chips and sort of the catalog (mocks pages/catalog, dialogs/filters). */
export class CatalogFilters {
  readonly searchBox: Locator;
  readonly eyebrow: Locator;
  readonly sidebarRoot: Locator;
  readonly sidebar: FilterForm;
  readonly toggle: Locator;
  readonly sheetRoot: Locator;
  readonly sheet: FilterForm;
  readonly sheetClose: Locator;
  readonly sheetClear: Locator;
  readonly sheetApply: Locator;
  readonly errorSummary: Locator;
  readonly chips: Locator;
  readonly clearAll: Locator;
  readonly sort: Locator;
  readonly noResults: Locator;
  readonly clearSearch: Locator;

  constructor(private readonly page: Page) {
    this.searchBox = page.getByRole('searchbox', { name: 'Search Tutu Much' });
    this.eyebrow = page.locator('main .eyebrow');
    this.sidebarRoot = page.getByRole('complementary', { name: 'Filters' });
    this.sidebar = new FilterForm(this.sidebarRoot);
    this.toggle = page.getByRole('button', { name: 'Filters', exact: true });
    this.sheetRoot = page.getByRole('dialog', { name: 'Filters' });
    this.sheet = new FilterForm(this.sheetRoot);
    this.sheetClose = this.sheetRoot.getByRole('button', { name: 'Close filters' });
    this.sheetClear = this.sheetRoot.getByRole('button', { name: 'Clear', exact: true });
    this.sheetApply = this.sheetRoot.getByRole('button', { name: /^Show( \d+)? items?$/ });
    this.errorSummary = this.sheetRoot.getByRole('alert');
    const listings = page.getByRole('region', { name: 'Listings' });
    this.chips = listings.locator('.chip');
    this.clearAll = listings.getByRole('button', { name: 'Clear all' });
    this.sort = listings.getByLabel('Sort');
    this.noResults = listings.locator('.empty');
    this.clearSearch = listings.getByRole('button', { name: 'Clear search and filters' });
  }

  removeChip(label: string): Locator {
    return this.page.getByRole('button', { name: `Remove filter ${label}` });
  }

  async search(term: string): Promise<void> {
    await this.searchBox.fill(term);
    await this.searchBox.press('Enter');
  }
}
