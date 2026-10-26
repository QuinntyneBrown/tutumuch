import { Routes } from '@angular/router';

/** Lazy routes of the listings feature area, mounted at /: the sell form, the edit form and the seller's listings. */
export const routes: Routes = [
  {
    path: 'sell',
    loadComponent: () => import('./sell/sell-page').then((m) => m.SellPage),
    title: 'Sell an item · TuTuMuch',
  },
  {
    path: 'sell/:reference',
    loadComponent: () => import('./sell/sell-page').then((m) => m.SellPage),
    title: 'Sell an item · TuTuMuch',
  },
  {
    path: 'items/:reference/edit',
    loadComponent: () => import('./edit-listing/edit-listing-page').then((m) => m.EditListingPage),
    title: 'Edit listing · TuTuMuch',
  },
  {
    path: 'account/listings',
    loadComponent: () => import('./my-listings/my-listings-page').then((m) => m.MyListingsPage),
    title: 'Your listings · TuTuMuch',
  },
];
