import { ApiProblem, ItemRequest } from '@tutumuch/api';

/** "Jan 18": the day a request expires or expired, by UTC calendar day like the order pages. */
export function requestDay(iso: string): string {
  return new Date(iso).toLocaleDateString('en-US', { month: 'short', day: 'numeric', timeZone: 'UTC' });
}

/** "$20.00", with thousands separators. */
export function requestMoney(value: number): string {
  return `$${value.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

/** "Dance · Girls’ · Up to $20.00 · Used - Good or better" (mocks pages/my-requests). */
export function requestFacts(request: ItemRequest): string {
  const parts = [request.category, request.size.replace("'", '’')];
  if (request.maxPrice !== null) parts.push(`Up to ${requestMoney(request.maxPrice)}`);
  if (request.minimumCondition) parts.push(`${request.minimumCondition} or better`);
  return parts.join(' · ');
}

/** True when the API said the visitor is not signed in; requests need a session (L2-105). */
export function needsSignIn(problem: ApiProblem): boolean {
  return problem.status === 401;
}
