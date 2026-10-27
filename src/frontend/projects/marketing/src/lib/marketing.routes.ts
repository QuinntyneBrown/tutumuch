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

/** The static pages linked from the footer, mounted at the root: Marketplace rules and Terms. */
export const infoRoutes: Routes = [
  {
    path: 'rules',
    loadComponent: () => import('./rules/rules-page').then((m) => m.RulesPage),
    title: 'Marketplace rules · TuTuMuch',
  },
  {
    path: 'terms',
    loadComponent: () => import('./terms/terms-page').then((m) => m.TermsPage),
    title: 'Terms · TuTuMuch',
  },
];
