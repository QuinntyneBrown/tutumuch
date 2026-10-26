import { Routes } from '@angular/router';

const page = () => import('./catalog/catalog-page').then((m) => m.CatalogPage);

/** Lazy routes of the catalog feature area, mounted at /: the catalog, one page per category and the listing detail. */
export const routes: Routes = [
  { path: '', pathMatch: 'full', loadComponent: page, title: 'Tutu Much' },
  { path: 'shoes', loadComponent: page, data: { pageCategory: 'Shoes' }, title: 'Shoes · Tutu Much' },
  { path: 'tops', loadComponent: page, data: { pageCategory: 'Tops' }, title: 'Tops · Tutu Much' },
  { path: 'outerwear', loadComponent: page, data: { pageCategory: 'Outerwear' }, title: 'Outerwear · Tutu Much' },
  { path: 'bottoms', loadComponent: page, data: { pageCategory: 'Bottoms' }, title: 'Bottoms · Tutu Much' },
  {
    path: 'items/:reference',
    loadComponent: () => import('./listing-detail/listing-detail-page').then((m) => m.ListingDetailPage),
    title: 'Item · Tutu Much',
  },
];
