import { Routes } from '@angular/router';

const page = () => import('./catalog/catalog-page').then((m) => m.CatalogPage);

/** Lazy routes of the catalog feature area, mounted at /: the catalog, one page per category and the listing detail. */
export const routes: Routes = [
  { path: '', pathMatch: 'full', loadComponent: page, title: 'TuTuMuch' },
  { path: 'clothing', loadComponent: page, data: { pageCategory: 'Clothing' }, title: 'Clothing · TuTuMuch' },
  { path: 'shoes', loadComponent: page, data: { pageCategory: 'Shoes' }, title: 'Shoes · TuTuMuch' },
  { path: 'dance', loadComponent: page, data: { pageCategory: 'Dance' }, title: 'Dance · TuTuMuch' },
  { path: 'sports', loadComponent: page, data: { pageCategory: 'Sports' }, title: 'Sports · TuTuMuch' },
  {
    path: 'cameras-and-tech',
    loadComponent: page,
    data: { pageCategory: 'Cameras and tech' },
    title: 'Cameras and tech · TuTuMuch',
  },
  {
    path: 'toys-and-crafts',
    loadComponent: page,
    data: { pageCategory: 'Toys and crafts' },
    title: 'Toys and crafts · TuTuMuch',
  },
  {
    path: 'items/:reference',
    loadComponent: () => import('./listing-detail/listing-detail-page').then((m) => m.ListingDetailPage),
    title: 'Item · TuTuMuch',
  },
];
