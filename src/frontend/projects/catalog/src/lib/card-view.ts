import { ListingCard } from '@tutumuch/api';
import { ListingCardView } from '@tutumuch/components';

/** "$85" for whole dollars, "$18.50" otherwise, with thousands separators. */
export function formatPrice(price: string): string {
  const value = Number(price);
  const cents = Math.round(value * 100) % 100 !== 0;
  return `$${value.toLocaleString('en-US', { minimumFractionDigits: cents ? 2 : 0, maximumFractionDigits: cents ? 2 : 0 })}`;
}

/** A catalog card as tm-listing-card shows it, linking to the listing detail. */
export function toCardView(card: ListingCard): ListingCardView {
  return {
    href: `/items/${card.reference}`,
    title: card.title,
    brand: card.brand,
    sizeLabel: card.sizeLabel,
    condition: card.condition,
    price: formatPrice(card.price),
    reserved: card.status === 'Reserved',
    photo: card.coverPhoto,
  };
}
