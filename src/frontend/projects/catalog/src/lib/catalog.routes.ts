import { inject } from '@angular/core';
import { CanActivateFn, Router, Routes, UrlMatcher, UrlSegment } from '@angular/router';
import { AuthState, LaunchState } from '@tutumuch/api';

const page = () => import('./catalog/catalog-page').then((m) => m.CatalogPage);

/** Category pages are not offered in launch Phase 1: their URLs redirect to the catalog (L2-093). */
const categoryMenusOpen: CanActivateFn = () =>
  inject(LaunchState).categoryMenusVisible() || inject(Router).createUrlTree(['/']);

/**
 * Item requests and showcase requests need a session (L2-105, L2-110): a visitor goes to sign in with the whole URL, query
 * string included, as returnUrl, and comes back after signing in.
 */
const signedIn: CanActivateFn = async (_route, state) => {
  const auth = inject(AuthState);
  const router = inject(Router);
  await auth.load();
  return auth.member() ? true : router.createUrlTree(['/sign-in'], { queryParams: { returnUrl: state.url } });
};

/**
 * A find's page matches only a find reference such as F-1001, so /finds/new falls through to the
 * post form of @tutumuch/listings (L2-113, L2-115).
 */
const findReference: UrlMatcher = (segments: UrlSegment[]) =>
  segments.length === 2 && segments[0].path === 'finds' && /^F-\d+$/.test(segments[1].path)
    ? { consumed: segments, posParams: { reference: segments[1] } }
    : null;

/**
 * Lazy routes of the catalog feature area, mounted at /: the catalog, one page per category (from
 * launch Phase 2), the listing detail, shelf finds and their pages (L1-025), the member's item requests (L1-023), which work in every
 * launch phase, and the administrator's showcase requests in the admin shell (L1-024).
 */
export const routes: Routes = [
  { path: '', pathMatch: 'full', loadComponent: page, data: { shell: 'wide' }, title: 'TuTuMuch' },
  {
    path: 'clothing',
    loadComponent: page,
    data: { pageCategory: 'Clothing', shell: 'wide' },
    canActivate: [categoryMenusOpen],
    title: 'Clothing · TuTuMuch',
  },
  {
    path: 'shoes',
    loadComponent: page,
    data: { pageCategory: 'Shoes', shell: 'wide' },
    canActivate: [categoryMenusOpen],
    title: 'Shoes · TuTuMuch',
  },
  {
    path: 'dance',
    loadComponent: page,
    data: { pageCategory: 'Dance', shell: 'wide' },
    canActivate: [categoryMenusOpen],
    title: 'Dance · TuTuMuch',
  },
  {
    path: 'sports',
    loadComponent: page,
    data: { pageCategory: 'Sports', shell: 'wide' },
    canActivate: [categoryMenusOpen],
    title: 'Sports · TuTuMuch',
  },
  {
    path: 'cameras-and-tech',
    loadComponent: page,
    data: { pageCategory: 'Cameras and tech', shell: 'wide' },
    canActivate: [categoryMenusOpen],
    title: 'Cameras and tech · TuTuMuch',
  },
  {
    path: 'toys-and-crafts',
    loadComponent: page,
    data: { pageCategory: 'Toys and crafts', shell: 'wide' },
    canActivate: [categoryMenusOpen],
    title: 'Toys and crafts · TuTuMuch',
  },
  {
    path: 'account/requests',
    canActivate: [signedIn],
    loadComponent: () => import('./my-requests/my-requests-page').then((m) => m.MyRequestsPage),
    title: 'Requests · TuTuMuch',
  },
  {
    path: 'account/requests/new',
    canActivate: [signedIn],
    loadComponent: () => import('./request-form/request-form-page').then((m) => m.RequestFormPage),
    title: 'Request an item · TuTuMuch',
  },
  {
    path: 'account/requests/:id/edit',
    canActivate: [signedIn],
    loadComponent: () => import('./request-form/request-form-page').then((m) => m.RequestFormPage),
    title: 'Edit request · TuTuMuch',
  },
  {
    path: 'admin/requests',
    canActivate: [signedIn],
    loadComponent: () => import('./showcase-requests/showcase-requests-page').then((m) => m.ShowcaseRequestsPage),
    title: 'Showcase requests · TuTuMuch admin',
    data: { shell: 'admin' },
  },
  {
    path: 'finds',
    pathMatch: 'full',
    loadComponent: () => import('./shelf-finds/shelf-finds-page').then((m) => m.ShelfFindsPage),
    data: { shell: 'wide' },
    title: 'Shelf finds · TuTuMuch',
  },
  {
    matcher: findReference,
    loadComponent: () => import('./find-detail/find-detail-page').then((m) => m.FindDetailPage),
    title: 'Shelf find · TuTuMuch',
  },
  {
    path: 'items/:reference',
    loadComponent: () => import('./listing-detail/listing-detail-page').then((m) => m.ListingDetailPage),
    title: 'Item · TuTuMuch',
  },
];
