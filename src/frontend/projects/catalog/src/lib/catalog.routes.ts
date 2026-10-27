import { inject } from '@angular/core';
import { CanActivateFn, Router, Routes } from '@angular/router';
import { LaunchState } from '@tutumuch/api';

const page = () => import('./catalog/catalog-page').then((m) => m.CatalogPage);

/** Category pages are not offered in launch Phase 1: their URLs redirect to the catalog (L2-093). */
const categoryMenusOpen: CanActivateFn = () =>
  inject(LaunchState).categoryMenusVisible() || inject(Router).createUrlTree(['/']);

/** Lazy routes of the catalog feature area, mounted at /: the catalog, one page per category (from launch Phase 2) and the listing detail. */
export const routes: Routes = [
  { path: '', pathMatch: 'full', loadComponent: page, title: 'TuTuMuch' },
  {
    path: 'clothing',
    loadComponent: page,
    data: { pageCategory: 'Clothing' },
    canActivate: [categoryMenusOpen],
    title: 'Clothing · TuTuMuch',
  },
  {
    path: 'shoes',
    loadComponent: page,
    data: { pageCategory: 'Shoes' },
    canActivate: [categoryMenusOpen],
    title: 'Shoes · TuTuMuch',
  },
  {
    path: 'dance',
    loadComponent: page,
    data: { pageCategory: 'Dance' },
    canActivate: [categoryMenusOpen],
    title: 'Dance · TuTuMuch',
  },
  {
    path: 'sports',
    loadComponent: page,
    data: { pageCategory: 'Sports' },
    canActivate: [categoryMenusOpen],
    title: 'Sports · TuTuMuch',
  },
  {
    path: 'cameras-and-tech',
    loadComponent: page,
    data: { pageCategory: 'Cameras and tech' },
    canActivate: [categoryMenusOpen],
    title: 'Cameras and tech · TuTuMuch',
  },
  {
    path: 'toys-and-crafts',
    loadComponent: page,
    data: { pageCategory: 'Toys and crafts' },
    canActivate: [categoryMenusOpen],
    title: 'Toys and crafts · TuTuMuch',
  },
  {
    path: 'items/:reference',
    loadComponent: () => import('./listing-detail/listing-detail-page').then((m) => m.ListingDetailPage),
    title: 'Item · TuTuMuch',
  },
];
