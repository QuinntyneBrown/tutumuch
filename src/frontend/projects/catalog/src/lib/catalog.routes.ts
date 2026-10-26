import { Routes } from '@angular/router';

const page = () => import('./catalog/catalog-page').then((m) => m.CatalogPage);

/** Lazy routes of the catalog feature area, mounted at /: the catalog, one page per category and the listing detail. */
export const routes: Routes = [
  { path: '', pathMatch: 'full', loadComponent: page, title: 'TuTuMuch' },
  { path: 'dresses', loadComponent: page, data: { pageCategory: 'Dresses' }, title: 'Dresses · TuTuMuch' },
  { path: 'tops', loadComponent: page, data: { pageCategory: 'Tops' }, title: 'Tops · TuTuMuch' },
  { path: 'outerwear', loadComponent: page, data: { pageCategory: 'Outerwear' }, title: 'Outerwear · TuTuMuch' },
  { path: 'bottoms', loadComponent: page, data: { pageCategory: 'Bottoms' }, title: 'Bottoms · TuTuMuch' },
  {
    path: 'items/:reference',
    loadComponent: () => import('./listing-detail/listing-detail-page').then((m) => m.ListingDetailPage),
    title: 'Item · TuTuMuch',
  },
];
