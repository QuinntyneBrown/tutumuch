import { ListingCard } from '@tutumuch/api';
import { ListingCardView, timeLeft } from '@tutumuch/components';

/** "$85" for whole dollars, "$18.50" otherwise, with thousands separators. */
export function formatPrice(price: string): string {
  const value = Number(price);
  const cents = Math.round(value * 100) % 100 !== 0;
  return `$${value.toLocaleString('en-US', { minimumFractionDigits: cents ? 2 : 0, maximumFractionDigits: cents ? 2 : 0 })}`;
}

/** A catalog card as tl-listing-card shows it, linking to the listing detail; an auction shows its time left (L2-114). */
export function toCardView(card: ListingCard, now = Date.now()): ListingCardView {
  const left = card.auction ? timeLeft(card.auction.endsAt, now) : null;
  return {
    href: `/items/${card.reference}`,
    title: card.title,
    brand: card.brand,
    sizeLabel: card.sizeLabel,
    condition: card.condition,
    price: formatPrice(card.price),
    reserved: card.status === 'Reserved',
    photo: card.coverPhoto,
    ...(card.auction && left
      ? {
          auction: {
            label: card.auction.bidCount > 0 ? 'Current bid' : 'Starting bid',
            timeLeft: left.text,
            soon: left.soon,
          },
        }
      : {}),
  };
}
