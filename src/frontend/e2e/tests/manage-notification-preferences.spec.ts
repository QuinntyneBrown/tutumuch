// Acceptance Test
// Traces to: L2-043, L2-068
// Description: The Emails page at /account/emails shows account security and order emails as
// always on and locked, review emails on and marketing off until the member opts in. Saving sends
// the two optional choices with the ETag it read in If-Match and shows "Email settings saved"; a
// failed save keeps the member's unsaved choice and offers "Try again" in an error toast, and
// settings changed elsewhere are reloaded. The page has loading and error states, and a visitor
// is sent to sign in.

import { Page, Request } from '@playwright/test';

import { expect, marcus, problem, signInAs, test } from '../fixtures';
import { EmailSettingsPage } from '../pages/email-settings.page';

/** GET /api/me/notification-preferences as TuTuMuch API returns it. */
function preferences(reviews = true, marketing = false): Record<string, unknown> {
  return {
    categories: [
      { key: 'accountSecurity', enabled: true, locked: true },
      { key: 'orders', enabled: true, locked: true },
      { key: 'reviews', enabled: reviews, locked: false },
      { key: 'marketing', enabled: marketing, locked: false },
    ],
  };
}

/** Answers GET with the preferences and ETag "0" and PUT with what was sent; returns the PUT requests. */
async function servePreferences(page: Page): Promise<Request[]> {
  const saves: Request[] = [];
  await page.route('**/api/me/notification-preferences', async (route) => {
    if (route.request().method() === 'PUT') {
      saves.push(route.request());
      const sent = route.request().postDataJSON() as { reviewEmails: boolean; marketingEmails: boolean };
      await route.fulfill({ json: preferences(sent.reviewEmails, sent.marketingEmails), headers: { ETag: '"1"' } });
      return;
    }
    await route.fulfill({ json: preferences(), headers: { ETag: '"0"' } });
  });
  return saves;
}

test.describe('manage notification preferences', () => {
  test('security and order emails are locked on, reviews on and marketing off by default', async ({
    page,
    unfixturedApiCalls,
  }) => {
    await signInAs(page, marcus);
    await servePreferences(page);
    const emails = new EmailSettingsPage(page);
    await emails.goto();

    await expect(emails.heading).toHaveText('Emails');
    await expect(emails.lead).toHaveText('Security and order emails are always on.');
    await expect(emails.rows).toHaveCount(4);
    await expect(emails.row('Account security')).toContainText('Sign-in alerts, password resets');
    await expect(emails.row('Orders')).toContainText('Sales, shipping, delivery, refunds');
    await expect(emails.row('Reviews')).toContainText('When someone reviews you');
    await expect(emails.row('News from TuTuMuch')).toContainText('New arrivals for girls, now and then');
    await expect(emails.security).toBeChecked();
    await expect(emails.security).toBeDisabled();
    await expect(emails.orders).toBeChecked();
    await expect(emails.orders).toBeDisabled();
    await expect(emails.reviews).toBeChecked();
    await expect(emails.reviews).toBeEnabled();
    await expect(emails.marketing).not.toBeChecked();
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('saving sends the two optional choices with If-Match and confirms', async ({ page, unfixturedApiCalls }) => {
    await signInAs(page, marcus);
    const saves = await servePreferences(page);
    let release: () => void = () => undefined;
    const held = new Promise<void>((resolve) => (release = resolve));
    await page.route('**/api/me/notification-preferences', async (route) => {
      if (route.request().method() !== 'PUT') return route.fallback();
      await held;
      await route.fallback();
    });
    const emails = new EmailSettingsPage(page);
    await emails.goto();

    await emails.reviews.click();
    await emails.marketing.check();
    await emails.save.click();

    await expect(emails.save).toHaveAttribute('aria-busy', 'true');
    await expect(emails.reviews).toBeDisabled();
    await expect(emails.marketing).toBeDisabled();
    release();
    await expect(emails.saved).toBeVisible();
    await expect(emails.save).not.toHaveAttribute('aria-busy', 'true');
    await expect(emails.reviews).not.toBeChecked();
    await expect(emails.marketing).toBeChecked();
    expect(saves).toHaveLength(1);
    expect(saves[0].postDataJSON()).toEqual({ reviewEmails: false, marketingEmails: true });
    expect(await saves[0].headerValue('If-Match')).toBe('"0"');
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('a failed save keeps the choice and offers Try again in an error toast', async ({ page }) => {
    await signInAs(page, marcus);
    const saves = await servePreferences(page);
    let failures = 1;
    await page.route('**/api/me/notification-preferences', async (route) => {
      if (route.request().method() !== 'PUT' || failures-- <= 0) return route.fallback();
      await route.fulfill(problem(500, 'INTERNAL', 'Something went wrong'));
    });
    const emails = new EmailSettingsPage(page);
    await emails.goto();

    await emails.reviews.click();
    await emails.save.click();

    await expect(emails.toast).toHaveClass(/toast--danger/);
    await expect(emails.toast).toHaveAttribute('role', 'alert');
    await expect(emails.toast).toContainText('Something went wrong');
    await expect(emails.toast).toContainText('Ref 5c1d-0e7a');
    await expect(emails.reviews).not.toBeChecked();
    await expect(emails.reviews).toBeEnabled();
    await expect(emails.saved).toHaveCount(0);

    await emails.toast.getByRole('button', { name: 'Try again' }).click();
    await expect(emails.saved).toBeVisible();
    await expect(emails.toast).toHaveCount(0);
    expect(saves).toHaveLength(1);
    expect(saves[0].postDataJSON()).toEqual({ reviewEmails: false, marketingEmails: false });
  });

  test('settings changed elsewhere are reloaded before the member saves again', async ({ page }) => {
    await signInAs(page, marcus);
    let reads = 0;
    await page.route('**/api/me/notification-preferences', async (route) => {
      if (route.request().method() === 'PUT') {
        await route.fulfill(
          problem(412, 'NOTIFICATION_PREFERENCES_CHANGED', 'Your email settings changed since you opened them'),
        );
        return;
      }
      reads++;
      await route.fulfill({ json: preferences(true, reads > 1), headers: { ETag: `"${reads - 1}"` } });
    });
    const emails = new EmailSettingsPage(page);
    await emails.goto();

    await emails.reviews.click();
    await emails.save.click();

    await expect(emails.toast).toHaveClass(/toast--warning/);
    await expect(emails.toast).toContainText('Your email settings changed since you opened them');
    await expect(emails.marketing).toBeChecked();
    await expect(emails.reviews).toBeChecked();
    expect(reads).toBe(2);
  });

  test('loading shows skeleton rows and a load failure offers Try again', async ({ page }) => {
    await signInAs(page, marcus);
    let calls = 0;
    let release: () => void = () => undefined;
    const held = new Promise<void>((resolve) => (release = resolve));
    await page.route('**/api/me/notification-preferences', async (route) => {
      calls++;
      if (calls === 1) {
        await held;
        await route.fulfill(problem(500, 'INTERNAL', 'Something went wrong'));
        return;
      }
      await route.fulfill({ json: preferences(), headers: { ETag: '"0"' } });
    });
    const emails = new EmailSettingsPage(page);
    await emails.goto();

    await expect(emails.loading.locator('li')).toHaveCount(4);
    release();
    await expect(emails.error).toContainText('Couldn’t load email settings');
    await expect(emails.error).toContainText('Something went wrong on our side.');
    await expect(emails.error).toContainText('Ref 5c1d-0e7a');

    await emails.tryAgain.click();
    await expect(emails.rows).toHaveCount(4);
  });

  test('a visitor is sent to sign in and back', async ({ page }) => {
    await page.route('**/api/me/notification-preferences', (route) =>
      route.fulfill(problem(401, 'UNAUTHENTICATED', 'Sign in to continue')),
    );
    const emails = new EmailSettingsPage(page);
    await emails.goto();

    await expect(page).toHaveURL(/\/sign-in\?returnUrl=%2Faccount%2Femails$/);
  });
});
