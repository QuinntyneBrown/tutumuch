import { Routes } from '@angular/router';

/** Lazy routes of the marketing feature area, mounted at /welcome (L2-078). */
export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./welcome/welcome-page').then((m) => m.WelcomePage),
    title: 'Twirl it forward · TuTuMuch',
    data: { shell: 'marketing' },
  },
];
