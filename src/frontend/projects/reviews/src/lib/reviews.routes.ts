import { Routes } from '@angular/router';

/** Lazy routes of the reviews feature area, mounted at /members: the public member profile. */
export const routes: Routes = [
  {
    path: ':displayName',
    loadComponent: () => import('./member-profile/member-profile-page').then((m) => m.MemberProfilePage),
    title: 'Member · Tutu Much',
  },
];
