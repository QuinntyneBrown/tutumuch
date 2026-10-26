import { PayoutRow } from '@tutumuch/api';

import { formatMoney } from '../money';
import { formatDay } from '../order-dates';

/** The meta line of a history row (mocks pages/payouts): why it is held, or the fee and where it is. */
export function payoutMeta(row: PayoutRow): string {
  if (row.status === 'Held') {
    const why =
      row.holdReason === 'OnboardingIncomplete' ? 'Held until you set up payouts' : 'Held until the order completes';
    return `${row.orderReference} · ${why}`;
  }
  const where: Record<string, string> = {
    Releasable: 'Releasing soon',
    Sent: 'On its way',
    Paid: row.paidAt ? formatDay(row.paidAt) : 'Paid',
    Failed: 'Returned by the bank',
  };
  return `${row.orderReference} · Fee ${formatMoney(row.fee)} · ${where[row.status]}`;
}

/** The status caption at the end of a row: Pending until paid. */
export function payoutCaption(row: PayoutRow): string {
  return row.status === 'Paid' ? 'Paid' : row.status === 'Failed' ? 'Failed' : 'Pending';
}

/** The total of the payouts on their way, for the info banner. */
export function sentAmount(history: PayoutRow[]): string {
  return formatMoney(
    String(history.filter((row) => row.status === 'Sent').reduce((sum, row) => sum + Number(row.amount), 0)),
  );
}
