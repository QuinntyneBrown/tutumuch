// Acceptance Test
// Traces to: L2-056
// Description: A first-time visitor sees the "Cookies" banner and only strictly necessary cookies
// are set. "Necessary only" or "Accept analytics" saves the choice in the tm_consent cookie, shows
// "Cookie choice saved" and closes the banner for good. Nothing from the analytics provider loads
// before consent or after "Necessary only". "Cookie settings" in the footer or the banner reopens
// the choice; turning analytics off there removes the analytics cookies.

import { Page, Request } from '@playwright/test';

import { ANALYTICS_COOKIE, ANALYTICS_ORIGIN, expect, serveCatalog, test } from '../fixtures';
import { CookieConsentPage } from '../pages/cookie-consent.page';

/** Records every request to the analytics provider. */
function analyticsRequests(page: Page): Request[] {
  const requests: Request[] = [];
  page.on('request', (request) => {
    if (request.url().startsWith(ANALYTICS_ORIGIN)) requests.push(request);
  });
  return requests;
}

test.describe('manage cookie consent', () => {
  test.describe('first visit', () => {
    test.use({ consent: 'unknown' });

    test('shows the banner and sets only strictly necessary cookies', async ({ page }) => {
      await serveCatalog(page, 3);
      const requests = analyticsRequests(page);
      const cookies = new CookieConsentPage(page);
      await page.goto('/');

      await expect(cookies.banner).toBeVisible();
      await expect(cookies.banner.getByRole('heading', { name: 'Cookies' })).toBeVisible();
      await expect(cookies.banner).toContainText(
        'We use necessary cookies to run TuTuMuch. Analytics only if you say yes.',
      );
      await expect(cookies.accept).toBeVisible();
      await expect(cookies.necessaryOnly).toBeVisible();
      await expect(cookies.bannerSettings).toBeVisible();
      for (const name of await cookies.cookieNames()) {
        expect(['tl_access', 'tl_refresh', 'tm_consent']).toContain(name);
      }
      expect(requests).toHaveLength(0);
    });

    test('"Necessary only" is remembered and nothing from analytics loads on further pages', async ({ page }) => {
      await serveCatalog(page, 30);
      const requests = analyticsRequests(page);
      const cookies = new CookieConsentPage(page);
      await page.goto('/');

      await cookies.necessaryOnly.click();

      await expect(cookies.banner).toBeHidden();
      await expect(cookies.toast).toContainText('Necessary only. Change it any time in the footer.');
      expect(await cookies.consentCookie()).toBe('v1.necessary');
      for (const path of ['/?category=Dresses', '/?q=tutu', '/sign-in']) {
        await page.goto(path);
        await expect(page.locator('main#main')).toBeVisible();
        await expect(cookies.banner).toBeHidden();
      }
      expect(requests).toHaveLength(0);
      expect(await cookies.cookieNames()).not.toContain(ANALYTICS_COOKIE);
    });

    test('"Accept analytics" loads the analytics provider and sets its cookie', async ({ page }) => {
      await serveCatalog(page, 3);
      const requests = analyticsRequests(page);
      const cookies = new CookieConsentPage(page);
      await page.goto('/');
      expect(requests).toHaveLength(0);

      await cookies.accept.click();

      await expect(cookies.banner).toBeHidden();
      await expect(cookies.toast).toContainText('Analytics on. Change it any time in the footer.');
      expect(await cookies.consentCookie()).toBe('v1.analytics');
      await expect.poll(() => cookies.cookieNames()).toContain(ANALYTICS_COOKIE);
      expect(requests.length).toBeGreaterThan(0);
    });

    test('the banner\'s "Cookie settings" opens the dialog, and saving there closes the banner', async ({ page }) => {
      await serveCatalog(page, 3);
      const cookies = new CookieConsentPage(page);
      await page.goto('/');

      await cookies.bannerSettings.click();

      await expect(cookies.dialog).toBeVisible();
      await expect(cookies.necessary).toBeChecked();
      await expect(cookies.necessary).toBeDisabled();
      await expect(cookies.dialog).toContainText('Sign-in and checkout. Always on.');
      await expect(cookies.analytics).not.toBeChecked();
      await expect(cookies.analytics).toBeFocused();
      await expect(cookies.dialog).toContainText('Helps us see which pages are slow.');

      await cookies.save.click();

      await expect(cookies.dialog).toBeHidden();
      await expect(cookies.banner).toBeHidden();
      expect(await cookies.consentCookie()).toBe('v1.necessary');
    });

    test('a malformed consent cookie counts as no choice', async ({ page, baseURL }) => {
      await page.context().addCookies([{ name: 'tm_consent', value: 'yes', url: baseURL! }]);
      await serveCatalog(page, 3);
      const cookies = new CookieConsentPage(page);
      await page.goto('/');

      await expect(cookies.banner).toBeVisible();
    });
  });

  test.describe('after a choice', () => {
    test('a visitor who chose "Necessary only" sees no banner', async ({ page }) => {
      await serveCatalog(page, 3);
      const requests = analyticsRequests(page);
      const cookies = new CookieConsentPage(page);
      await page.goto('/');

      await expect(page.locator('main#main')).toBeVisible();
      await expect(cookies.banner).toBeHidden();
      expect(requests).toHaveLength(0);
    });

    test.describe('with analytics accepted', () => {
      test.use({ consent: 'analytics' });

      test('turning analytics off in "Cookie settings" from the footer removes the analytics cookies', async ({
        page,
      }) => {
        await serveCatalog(page, 3);
        const cookies = new CookieConsentPage(page);
        await page.goto('/');
        await expect.poll(() => cookies.cookieNames()).toContain(ANALYTICS_COOKIE);

        await cookies.footerSettings.click();
        await expect(cookies.dialog).toBeVisible();
        await expect(cookies.analytics).toBeChecked();
        await cookies.analytics.uncheck();
        await cookies.save.click();

        await expect(cookies.dialog).toBeHidden();
        await expect.poll(() => cookies.cookieNames()).not.toContain(ANALYTICS_COOKIE);
        expect(await cookies.consentCookie()).toBe('v1.necessary');
        await expect(cookies.footerSettings).toBeFocused();
      });

      test('"Cancel" closes the dialog without changing the choice', async ({ page }) => {
        await serveCatalog(page, 3);
        const cookies = new CookieConsentPage(page);
        await page.goto('/');

        await cookies.footerSettings.click();
        await cookies.analytics.uncheck();
        await cookies.cancel.click();

        await expect(cookies.dialog).toBeHidden();
        expect(await cookies.consentCookie()).toBe('v1.analytics');
        await expect.poll(() => cookies.cookieNames()).toContain(ANALYTICS_COOKIE);
      });
    });
  });
});
