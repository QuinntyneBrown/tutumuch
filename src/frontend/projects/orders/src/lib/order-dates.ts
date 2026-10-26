import { OrderTimelineStep } from '@tutumuch/api';
import { TimelineStep } from '@tutumuch/components';

const DAYS = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

/** "Sat 24 Oct" in UTC, the zone Tutu Much API counts business days in (ADR orders/0003). */
export function formatDay(iso: string): string {
  const date = new Date(iso);
  return `${DAYS[date.getUTCDay()]} ${date.getUTCDate()} ${MONTHS[date.getUTCMonth()]}`;
}

/** The API's timeline as tm-order-timeline steps. */
export function timelineSteps(timeline: OrderTimelineStep[]): TimelineStep[] {
  return timeline.map((step) => ({ label: step.status, date: step.at ? formatDay(step.at) : null }));
}
