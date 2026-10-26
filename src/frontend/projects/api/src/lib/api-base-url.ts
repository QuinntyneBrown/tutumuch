import { InjectionToken } from '@angular/core';

/** Base URL of Tutu Much API, without a trailing slash. */
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL');
