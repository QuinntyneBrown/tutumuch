import { OrderList, OrderRow, OrderStatus } from '@tutumuch/api';
import { StatusTone } from '@tutumuch/components';

import { formatDay } from '../order-dates';

export type OrderHistoryList = 'purchases' | 'sales';

/** The meta line of a row (mocks pages/purchases, pages/sales): the other party, the ship-by date of a Paid sale, the date paid. */
export function orderMeta(list: OrderHistoryList, row: OrderRow): string {
  if (list === 'purchases') {
    return `from ${row.otherParty} · ${formatDay(row.paidAt)}`;
  }
  const shipBy = row.shipBy ? ` · ship by ${formatDay(row.shipBy)}` : '';
  return `to ${row.otherParty}${shipBy} · ${formatDay(row.paidAt)}`;
}

/** The subtitle under the heading, "1 on the way · 1 to confirm" or "1 to ship"; empty when nothing waits. */
export function orderSubtitle(list: OrderHistoryList, counts: OrderList['counts']): string {
  const parts =
    list === 'purchases'
      ? [
          counts.onTheWay ? `${counts.onTheWay} on the way` : '',
          counts.toConfirm ? `${counts.toConfirm} to confirm` : '',
        ]
      : [counts.toShip ? `${counts.toShip} to ship` : ''];
  return parts.filter(Boolean).join(' · ');
}

/** Completed reads as done, Cancelled and Refunded as ended; every open status as info. */
export function statusTone(status: OrderStatus): StatusTone {
  return status === 'Completed' ? 'active' : status === 'Cancelled' || status === 'Refunded' ? 'removed' : 'info';
}
