/** Development: ng serve proxies /api to TuTuMuch.Api (projects/tutumuch/proxy.conf.json), so cookies stay same-origin. */
export const environment = {
  apiBaseUrl: '/api',
  analytics: { scriptUrl: 'https://analytics.tutumuch.invalid/script.js', cookieNames: ['_tma'] },
};
