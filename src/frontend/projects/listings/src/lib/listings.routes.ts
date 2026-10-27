import { inject } from '@angular/core';
import { CanMatchFn, Routes } from '@angular/router';
import { AuthState, LaunchState } from '@tutumuch/api';

/**
 * The seller routes match only when the launch phase lets the caller sell: Phase 3, or an
 * administrator in Phases 1 and 2 (L2-094). Otherwise the same paths show "Selling opens soon".
 */
const sellingOpen: CanMatchFn = async () => {
  const auth = inject(AuthState);
  const launch = inject(LaunchState);
  await auth.load();
  return launch.canSell();
};

const notOpen = () => import('./selling-not-open/selling-not-open-page').then((m) => m.SellingNotOpenPage);
const notOpenTitle = 'Selling opens soon · TuTuMuch';

/** Lazy routes of the listings feature area, mounted at /: the sell form, the edit form and the seller's listings. */
export const routes: Routes = [
  {
    path: 'sell',
    canMatch: [sellingOpen],
    loadComponent: () => import('./sell/sell-page').then((m) => m.SellPage),
    title: 'Sell an item · TuTuMuch',
  },
  {
    path: 'sell/:reference',
    canMatch: [sellingOpen],
    loadComponent: () => import('./sell/sell-page').then((m) => m.SellPage),
    title: 'Sell an item · TuTuMuch',
  },
  {
    path: 'items/:reference/edit',
    canMatch: [sellingOpen],
    loadComponent: () => import('./edit-listing/edit-listing-page').then((m) => m.EditListingPage),
    title: 'Edit listing · TuTuMuch',
  },
  {
    path: 'account/listings',
    canMatch: [sellingOpen],
    loadComponent: () => import('./my-listings/my-listings-page').then((m) => m.MyListingsPage),
    title: 'Your listings · TuTuMuch',
  },
  { path: 'sell', loadComponent: notOpen, title: notOpenTitle },
  { path: 'sell/:reference', loadComponent: notOpen, title: notOpenTitle },
  { path: 'items/:reference/edit', loadComponent: notOpen, title: notOpenTitle },
  { path: 'account/listings', loadComponent: notOpen, title: notOpenTitle },
];
