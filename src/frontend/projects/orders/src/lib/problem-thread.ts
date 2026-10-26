import { OrderProblem } from '@tutumuch/api';
import { ThreadEntry } from '@tutumuch/components';

import { formatDay } from './order-dates';

/**
 * The buyer's claim, with its photos, and the seller's reply as `tl-thread` entries. The captions
 * name the buyer and the seller as the viewer knows them, for example "You" for the buyer's own
 * claim, or "marcus_12 · buyer" on the administrator's page.
 */
export function problemThread(problem: OrderProblem, buyer: string, seller: string): ThreadEntry[] {
  const entries: ThreadEntry[] = [
    {
      caption: `${buyer} · ${formatDay(problem.openedAt)}`,
      text: problem.description,
      photos: problem.photos.map((photo, i) => ({ src: photo.url, alt: `Buyer photo ${i + 1}` })),
    },
  ];
  if (problem.sellerResponse && problem.sellerRespondedAt) {
    entries.push({ caption: `${seller} · ${formatDay(problem.sellerRespondedAt)}`, text: problem.sellerResponse });
  }
  return entries;
}
