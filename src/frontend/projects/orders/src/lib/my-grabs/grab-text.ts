import { GrabStatus } from '@tutumuch/api';
import { StatusTone } from '@tutumuch/components';

/** The tl-status tone of a grab (mocks pages/my-grabs): waiting, moving, done, or ended with a refund. */
export function grabTone(status: GrabStatus): StatusTone {
  switch (status) {
    case 'Requested':
    case 'PaymentPending':
      return 'reserved';
    case 'Accepted':
    case 'Bought':
      return 'info';
    case 'Received':
      return 'active';
    default:
      return 'removed';
  }
}
