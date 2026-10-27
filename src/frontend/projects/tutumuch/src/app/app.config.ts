import { ApplicationConfig, inject, provideAppInitializer, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { LaunchState, provideApi } from '@tutumuch/api';
import { provideAnalytics } from '@tutumuch/privacy';

import { environment } from '../environments/environment';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    provideApi(environment.apiBaseUrl),
    provideAnalytics(environment.analytics),
    // The launch phase decides menus and seller entry points, so it is known before the first render (L2-090).
    provideAppInitializer(() => inject(LaunchState).load()),
  ],
};
