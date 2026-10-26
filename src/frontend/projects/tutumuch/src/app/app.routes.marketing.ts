import { Routes } from '@angular/router';

/**
 * Routes for the public marketing build (`ng build tutumuch --configuration marketing`), served at
 * https://tutumuch.kids while the marketplace is not open yet: the marketing page at / and every other
 * path redirected to it. Swapped in for app.routes.ts by angular.json fileReplacements.
 */
export const routes: Routes = [
  { path: '', loadChildren: () => import('@tutumuch/marketing').then((m) => m.routes) },
  { path: '**', redirectTo: '' },
];
