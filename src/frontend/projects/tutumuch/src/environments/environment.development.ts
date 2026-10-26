/** Development: ng serve proxies /api to TutuMuch.Api (projects/tutumuch/proxy.conf.json), so cookies stay same-origin. */
export const environment = {
  apiBaseUrl: '/api',
  analytics: { scriptUrl: 'https://analytics.tutumuch.invalid/script.js', cookieNames: ['_tma'] },
};
