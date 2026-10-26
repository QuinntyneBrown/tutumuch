import { ParamMap, Params } from '@angular/router';
import { CatalogFilterParams } from '@tutumuch/api';

export type CatalogSort = 'newest' | 'price-asc' | 'price-desc';

/** One result set: keyword, filters and sort, held in the page URL (L2-024). Prices are dollar strings. */
export interface CatalogQuery {
  q: string;
  category: string | null;
  subcategories: readonly string[];
  brands: readonly string[];
  conditions: readonly string[];
  min: string;
  max: string;
  sort: CatalogSort;
}

export interface Option {
  slug: string;
  label: string;
}

export interface CategoryOption extends Option {
  sizeLabel: string;
  subcategories: readonly Option[];
}

const option = (label: string): Option => ({ slug: label.toLowerCase().replace(/ /g, '-'), label });

/** The L2-011 categories with their subcategories; the URL carries the slugs. */
export const CATEGORIES: readonly CategoryOption[] = [
  { ...option('Dresses'), sizeLabel: 'Girls', subcategories: ['Dresses', 'Skirts', 'Tutus', 'Rompers'].map(option) },
  { ...option('Tops'), sizeLabel: 'Girls', subcategories: ['Shirts', 'T-shirts', 'Sweaters', 'Hoodies'].map(option) },
  { ...option('Outerwear'), sizeLabel: 'Girls', subcategories: ['Jackets', 'Coats'].map(option) },
  { ...option('Bottoms'), sizeLabel: 'Girls', subcategories: ['Pants', 'Leggings', 'Shorts'].map(option) },
];

/** The L2-016 conditions. */
export const CONDITIONS: readonly Option[] = [
  { slug: 'new-with-tags', label: 'New with tags' },
  { slug: 'new-without-tags', label: 'New without tags' },
  { slug: 'used-excellent', label: 'Used - Excellent' },
  { slug: 'used-good', label: 'Used - Good' },
  { slug: 'used-fair', label: 'Used - Fair' },
];

export const SORTS: readonly { value: CatalogSort; label: string }[] = [
  { value: 'newest', label: 'Newest' },
  { value: 'price-asc', label: 'Price: low to high' },
  { value: 'price-desc', label: 'Price: high to low' },
];

export const MAX_SEARCH_LENGTH = 100;
export const PRICE_RANGE_ERROR = 'Minimum price must be lower than maximum';

const PRICE = /^\d+(\.\d{1,2})?$/;

export const EMPTY_QUERY: CatalogQuery = {
  q: '',
  category: null,
  subcategories: [],
  brands: [],
  conditions: [],
  min: '',
  max: '',
  sort: 'newest',
};

export function categoryOf(query: CatalogQuery): CategoryOption | undefined {
  return CATEGORIES.find((c) => c.slug === query.category);
}

/** Reads a URL into a query, dropping values it does not recognise. */
export function parseQuery(params: ParamMap): CatalogQuery {
  const category = CATEGORIES.find((c) => c.slug === params.get('category'));
  const unique = (values: string[]) => [...new Set(values)];
  const price = (value: string | null) => (value && PRICE.test(value) ? value : '');
  const sort = SORTS.find((s) => s.value === params.get('sort'))?.value ?? 'newest';
  return {
    q: (params.get('q') ?? '').trim().slice(0, MAX_SEARCH_LENGTH),
    category: category?.slug ?? null,
    subcategories: unique(
      params.getAll('subcategory').filter((s) => category?.subcategories.some((o) => o.slug === s)),
    ),
    brands: unique(params.getAll('brand').filter((b) => b.trim() !== '')),
    conditions: unique(params.getAll('condition').filter((c) => CONDITIONS.some((o) => o.slug === c))),
    min: price(params.get('min')),
    max: price(params.get('max')),
    sort,
  };
}

/** The canonical query parameters: fixed order, defaults omitted, so one result set has one URL. */
export function toParams(query: CatalogQuery): Params {
  const params: Params = {};
  if (query.q) params['q'] = query.q;
  if (query.category) params['category'] = query.category;
  if (query.subcategories.length) params['subcategory'] = [...query.subcategories];
  if (query.brands.length) params['brand'] = [...query.brands];
  if (query.conditions.length) params['condition'] = [...query.conditions];
  if (query.min) params['min'] = query.min;
  if (query.max) params['max'] = query.max;
  if (query.sort !== 'newest') params['sort'] = query.sort;
  return params;
}

/** The same parameters for TuTuMuch API, whose names and slugs match the URL. */
export function toApiParams(query: CatalogQuery): CatalogFilterParams {
  return toParams(query) as CatalogFilterParams;
}

const KEYS = ['q', 'category', 'subcategory', 'brand', 'condition', 'min', 'max', 'sort'];

/**
 * The canonical form of a URL's parameters, or null when they already are canonical. Parameters
 * the catalog does not own, such as from=sign-in, are kept as they are.
 */
export function canonicalParams(params: ParamMap, query: CatalogQuery): Params | null {
  const canonical = toParams(query);
  const owned = params.keys.filter((key) => KEYS.includes(key));
  const same =
    owned.length === Object.keys(canonical).length &&
    owned.every((key, i) => key === Object.keys(canonical)[i] && String(params.getAll(key)) === String(canonical[key]));
  if (same) return null;
  const others = Object.fromEntries(
    params.keys.filter((key) => !KEYS.includes(key)).map((key) => [key, params.getAll(key)]),
  );
  return { ...others, ...canonical };
}

/** The inline price error, or null when the range is valid (L2-024 criterion 3). */
export function priceError(query: CatalogQuery): string | null {
  const malformed = [query.min, query.max].some((p) => p !== '' && !PRICE.test(p));
  if (malformed) return 'Enter a price such as 20 or 19.99';
  return query.min && query.max && Number(query.min) > Number(query.max) ? PRICE_RANGE_ERROR : null;
}

export function hasFilters(query: CatalogQuery): boolean {
  return (
    !!query.category ||
    query.subcategories.length + query.brands.length + query.conditions.length > 0 ||
    !!query.min ||
    !!query.max
  );
}

export interface FilterChip {
  label: string;
  without: CatalogQuery;
}

/** One chip per active filter value, in the order of the sidebar (mocks pages/catalog filtered). */
export function chipsOf(query: CatalogQuery): FilterChip[] {
  const category = categoryOf(query);
  const chips: FilterChip[] = [];
  if (category) chips.push({ label: category.label, without: { ...query, category: null, subcategories: [] } });
  for (const slug of query.subcategories) {
    const label = category?.subcategories.find((s) => s.slug === slug)?.label ?? slug;
    chips.push({ label, without: { ...query, subcategories: query.subcategories.filter((s) => s !== slug) } });
  }
  for (const brand of query.brands) {
    chips.push({ label: brand, without: { ...query, brands: query.brands.filter((b) => b !== brand) } });
  }
  for (const slug of query.conditions) {
    const label = CONDITIONS.find((c) => c.slug === slug)?.label ?? slug;
    chips.push({ label, without: { ...query, conditions: query.conditions.filter((c) => c !== slug) } });
  }
  if (query.min || query.max) {
    const label =
      query.min && query.max ? `$${query.min}–$${query.max}` : query.min ? `From $${query.min}` : `Up to $${query.max}`;
    chips.push({ label, without: { ...query, min: '', max: '' } });
  }
  return chips;
}
