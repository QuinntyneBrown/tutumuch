import { Routes } from '@angular/router';

/** Lazy routes of the privacy feature area, mounted at /privacy. */
export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./privacy-page/privacy-page').then((m) => m.PrivacyPage),
    title: 'Privacy · TuTuMuch',
  },
];
