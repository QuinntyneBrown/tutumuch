import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: 'welcome', loadChildren: () => import('@tutumuch/marketing').then((m) => m.routes) },
  { path: '', loadChildren: () => import('@tutumuch/catalog').then((m) => m.routes) },
  { path: '', loadChildren: () => import('@tutumuch/listings').then((m) => m.routes) },
  { path: '', loadChildren: () => import('@tutumuch/orders').then((m) => m.routes) },
  { path: 'members', loadChildren: () => import('@tutumuch/reviews').then((m) => m.routes) },
  { path: 'privacy', loadChildren: () => import('@tutumuch/privacy').then((m) => m.routes) },
  { path: '', loadChildren: () => import('@tutumuch/accounts').then((m) => m.routes) },
];
