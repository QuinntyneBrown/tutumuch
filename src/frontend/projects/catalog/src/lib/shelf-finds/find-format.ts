import { FindCard } from '@tutumuch/api';
import { FindCardView } from '@tutumuch/components';

/** "$40.00": a shelf price is shown to the cent, as on the shelf. */
export function shelfPrice(price: string): string {
  return `$${Number(price).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

/** "Sat 24 Oct" for a "YYYY-MM-DD" day. */
export function findDay(day: string): string {
  return new Date(`${day}T00:00:00Z`).toLocaleDateString('en-GB', {
    weekday: 'short',
    day: 'numeric',
    month: 'short',
    timeZone: 'UTC',
  });
}

/** A find as tl-find-card shows it, linking to its page (L2-114). */
export function toFindCardView(card: FindCard): FindCardView {
  return {
    href: `/finds/${card.reference}`,
    title: card.title,
    category: card.category,
    store: card.storeName,
    seenOn: card.seenOn,
    seenLabel: `Seen ${findDay(card.seenOn)}`,
    price: shelfPrice(card.shelfPrice),
    grabAvailable: card.grabAvailable,
  };
}
