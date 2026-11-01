import { ListingStatus, MyListingRow, MyListings } from '@tutumuch/api';
import { FilterChipOption, StatusTone, timeLeft } from '@tutumuch/components';

const DAYS = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
const DAY_MS = 24 * 60 * 60 * 1000;

export const STATUSES: ListingStatus[] = ['Draft', 'Active', 'Reserved', 'Sold', 'Removed'];

/** The status in the URL query (`?status=sold`), or null for All or an unknown value. */
export function statusFromQuery(value: string | null): ListingStatus | null {
  return STATUSES.find((status) => status.toLowerCase() === value?.toLowerCase()) ?? null;
}

/** "All 5", "Draft 2" ... in the order of the mocks. */
export function filterOptions(counts: MyListings['counts']): FilterChipOption[] {
  return [
    { value: 'all', label: 'All', count: counts.all },
    ...STATUSES.map((status) => ({
      value: status.toLowerCase(),
      label: status,
      count: counts[status.toLowerCase() as keyof MyListings['counts']],
    })),
  ];
}

/** "5 listings · 1 active". */
export function listingsSubtitle(counts: MyListings['counts']): string {
  return `${counts.all} ${counts.all === 1 ? 'listing' : 'listings'} · ${counts.active} active`;
}

/** The title, or "Untitled draft · Tops" for a draft without one. */
export function rowTitle(row: MyListingRow): string {
  return row.title ?? `Untitled draft · ${row.category}`;
}

/** "$60" when the cents are zero, "$38.50" otherwise, "—" when no price is set. */
export function rowPrice(price: number | null): string {
  if (price === null) {
    return '—';
  }
  const digits = Number.isInteger(price) ? 0 : 2;
  return `$${price.toLocaleString('en-US', { minimumFractionDigits: digits, maximumFractionDigits: digits })}`;
}

/** "Today", "Yesterday" or "Sat 24 Oct", by UTC calendar day like the order pages. */
export function rowDate(iso: string, now: Date): string {
  const date = new Date(iso);
  const days = Math.round(
    (Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate()) -
      Date.UTC(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate())) /
      DAY_MS,
  );
  if (days === 0) {
    return 'Today';
  }
  if (days === 1) {
    return 'Yesterday';
  }
  return `${DAYS[date.getUTCDay()]} ${date.getUTCDate()} ${MONTHS[date.getUTCMonth()]}`;
}

/** Where a row opens: a draft in the sell form, a sold listing on its sale, any other on its detail page. */
export function rowLink(row: MyListingRow): string[] {
  if (row.status === 'Draft') {
    return ['/sell', row.reference];
  }
  if (row.status === 'Sold' && row.saleOrderId) {
    return ['/sales', row.saleOrderId];
  }
  return ['/items', row.reference];
}

export function statusTone(status: ListingStatus): StatusTone {
  return status.toLowerCase() as StatusTone;
}

/** The info toast of mocks notifications/toast for a seller with drafts (L2-019). */
export const draftExpiryTitle = 'Drafts older than 90 days are deleted';
export const draftExpiryText = 'We’ll email you a week before.';

/**
 * The line under an auction row's price (L2-114, L2-117): "Auction · 3 bids · Ends in 2d 4h" while
 * it runs, "Auction ended, no bids" or "Winner didn’t pay" once Removed; null for a fixed price.
 */
export function auctionLine(row: MyListingRow, now = Date.now()): string | null {
  if (row.format !== 'Auction') return null;
  if (row.removalReason === 'AuctionUnsold') return 'Auction ended, no bids';
  if (row.removalReason === 'AuctionUnpaid') return 'Winner didn’t pay';
  const bids = row.bidCount ?? 0;
  const count = bids === 1 ? '1 bid' : `${bids} bids`;
  if (row.status !== 'Active' || !row.auctionEndsAt) return `Auction · ${count}`;
  return `Auction · ${count} · ${timeLeft(row.auctionEndsAt, now).text}`;
}
