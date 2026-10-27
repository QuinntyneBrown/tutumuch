import { inject } from '@angular/core';
import { CanActivateFn, Router, Routes } from '@angular/router';
import { AuthState, LaunchState } from '@tutumuch/api';

const page = () => import('./catalog/catalog-page').then((m) => m.CatalogPage);

/** Category pages are not offered in launch Phase 1: their URLs redirect to the catalog (L2-093). */
const categoryMenusOpen: CanActivateFn = () =>
  inject(LaunchState).categoryMenusVisible() || inject(Router).createUrlTree(['/']);

/**
 * Item requests need a session (L2-105): a visitor goes to sign in with the whole URL, query
 * string included, as returnUrl, and comes back after signing in.
 */
const signedIn: CanActivateFn = async (_route, state) => {
  const auth = inject(AuthState);
  const router = inject(Router);
  await auth.load();
  return auth.member() ? true : router.createUrlTree(['/sign-in'], { queryParams: { returnUrl: state.url } });
};

/**
 * Lazy routes of the catalog feature area, mounted at /: the catalog, one page per category (from
 * launch Phase 2), the listing detail, and the member's item requests (L1-023), which work in
 * every launch phase.
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
    path: 'items/:reference',
    loadComponent: () => import('./listing-detail/listing-detail-page').then((m) => m.ListingDetailPage),
    title: 'Item · TuTuMuch',
  },
];
