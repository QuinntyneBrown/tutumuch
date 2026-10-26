import { ListingDetails } from '@tutumuch/api';

/** Raw values of the sell form. Money is kept as typed text until it is checked. */
export interface ListingFormValue {
  category: string;
  subcategory: string;
  title: string;
  brand: string;
  condition: string;
  flaws: string;
  description: string;
  price: string;
  shipping: string;
  confirm: boolean;
}

/** Control ids of the form, in error summary order. The condition links to its first option. */
export const FIELD_IDS = [
  'photos',
  'cat',
  'sub',
  'title',
  'brand',
  'cond-0',
  'flaws',
  'desc',
  'price',
  'ship',
  'confirm',
] as const;

export type FieldId = (typeof FIELD_IDS)[number];

export type FieldErrors = Partial<Record<FieldId, string>>;

/** Request fields of Tutu Much API mapped to control ids, so server errors land on their control. */
export const SERVER_FIELDS: Record<string, FieldId> = {
  photos: 'photos',
  photoIds: 'photos',
  category: 'cat',
  subcategory: 'sub',
  size: 'cat',
  title: 'title',
  brand: 'brand',
  condition: 'cond-0',
  flaws: 'flaws',
  description: 'desc',
  price: 'price',
  shippingCost: 'ship',
  sizeConfirmed: 'confirm',
};

const USED_FAIR = 'Used - Fair';

/** Largest photo Tutu Much API accepts: 10 MB, read as 10 MiB (L2-017). */
export const MAX_PHOTO_BYTES = 10 * 1024 * 1024;

/** "11.4 MB" for a rejected tile. */
export function formatFileSize(bytes: number): string {
  return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
}
const MONEY = /^\d+(\.\d{1,2})?$/;

/** Parses typed money with at most two decimals; null when empty or malformed. */
export function parseMoney(text: string): number | null {
  const value = text.trim();
  return MONEY.test(value) ? Number(value) : null;
}

function inRange(text: string, min: number, max: number): boolean {
  const value = parseMoney(text);
  return value !== null && value >= min && value <= max;
}

/** "This item is labelled in girls' sizing" (L2-014); every category takes girls' sizes. */
export function confirmLabel(category: string): string {
  if (!category) return "The label shows girls' sizing";
  return "This item is labelled in girls' sizing";
}

/**
 * Client checks mirroring Tutu Much API, for fast feedback only; the server decides. Drafts need a
 * category and every present field within its limits (L2-019); publishing adds L2-014 to L2-018,
 * including at least one photo (L2-017).
 */
export function checkListing(value: ListingFormValue, mode: 'draft' | 'publish', photoCount: number): FieldErrors {
  const errors: FieldErrors = {};
  const publish = mode === 'publish';
  if (publish && photoCount < 1) errors.photos = 'Add at least one photo';
  const title = value.title.trim();
  const brand = value.brand.trim();
  const description = value.description.trim();
  const flaws = value.flaws.trim();

  if (!value.category) errors.cat = 'Choose a category';
  if (publish && !value.subcategory) errors.sub = 'Choose a type';
  if (title.length > 80 || (publish && title.length < 5)) errors.title = 'Title must be 5 to 80 characters';
  if (brand.length > 50 || (publish && !brand)) errors.brand = 'Brand must be 1 to 50 characters';
  if (publish && !value.condition) errors['cond-0'] = 'Choose a condition';
  if (value.condition === USED_FAIR) {
    if (flaws.length > 500) errors.flaws = 'Keep the flaws to 500 characters';
    else if (publish && flaws.length < 10) errors.flaws = "Describe the item's flaws";
  }
  if (description.length > 2000 || (publish && description.length < 20)) {
    errors.desc = 'Description must be 20 to 2,000 characters';
  }
  if ((value.price.trim() || publish) && !inRange(value.price, 1, 5000)) {
    errors.price = 'Enter a price from $1.00 to $5,000.00';
  }
  if ((value.shipping.trim() || publish) && !inRange(value.shipping, 0, 100)) {
    errors.ship = 'Enter shipping from $0.00 to $100.00';
  }
  if (publish && !value.confirm) {
    errors.confirm = "Confirm the label shows girls' sizing";
  }
  return errors;
}

/** The request body: present fields only, never a size (L2-013). */
export function toDetails(value: ListingFormValue): ListingDetails {
  const details: ListingDetails = {};
  const text = (field: string) => field.trim() || undefined;
  details.category = value.category || undefined;
  details.subcategory = value.subcategory || undefined;
  details.title = text(value.title);
  details.brand = text(value.brand);
  details.condition = value.condition || undefined;
  details.flaws = value.condition === USED_FAIR ? text(value.flaws) : undefined;
  details.description = text(value.description);
  details.price = parseMoney(value.price) ?? undefined;
  details.shippingCost = parseMoney(value.shipping) ?? undefined;
  return Object.fromEntries(Object.entries(details).filter(([, v]) => v !== undefined)) as ListingDetails;
}

const usd = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' });

export function formatUsd(amount: number): string {
  return usd.format(amount);
}

/**
 * Estimated earnings, (price + shipping) × (1 − fee / 100) rounded half up to the cent, or null
 * while the price is empty or invalid (L2-031). Display only; seller-payouts computes the fee.
 */
export function estimateEarnings(price: string, shipping: string, feePercent: number): string | null {
  if (!inRange(price, 1, 5000)) return null;
  const ship = shipping.trim() ? (inRange(shipping, 0, 100) ? parseMoney(shipping)! : null) : 0;
  if (ship === null) return null;
  const cents = Math.round((parseMoney(price)! + ship) * (100 - feePercent));
  return formatUsd(cents / 100);
}
