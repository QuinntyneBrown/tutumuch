/**
 * The return URL to follow after signing in or joining, or "/" (L2-109): only a same-site path,
 * one that starts with a single "/" and not "//" or "/\", which browsers treat as another host.
 */
export function safeReturnUrl(url: string | null | undefined): string {
  const value = url ?? '';
  return value.startsWith('/') && !value.startsWith('//') && !value.startsWith('/\\') ? value : '/';
}

/** The return URL of "Request this item" on a showcase listing (L2-109). */
export function showcaseRequestUrl(reference: string): string {
  return `/items/${reference}?request=1`;
}

/** True when the return URL comes back to a showcase request, so sign in and join can say so. */
export function isShowcaseRequestUrl(url: string | null | undefined): boolean {
  return /^\/items\/[^/?#]+\?request=1$/.test(url ?? '');
}
