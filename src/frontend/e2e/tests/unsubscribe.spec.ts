// Acceptance Test
// Traces to: L2-043
// Description: The unsubscribe link in a marketing email opens /emails/unsubscribe, which posts
// the token once from script, without signing in, and confirms that news from TuTuMuch stopped while
// security and order emails continue. An invalid link says so and points to the email settings;
// a failure shows the reference and "Try again".

import { Request } from '@playwright/test';

import { expect, problem, test } from '../fixtures';
import { UnsubscribePage } from '../pages/unsubscribe.page';

const token = 'AAECAwQFBgcICQoLDA0ODwMAAAAAZ1zZAA.c2lnbmF0dXJl';

test.describe('one-click unsubscribe', () => {
  test('a visitor following the link is unsubscribed without signing in', async ({ page, unfixturedApiCalls }) => {
    const posts: Request[] = [];
    await page.route('**/api/notifications/unsubscribe', async (route) => {
      posts.push(route.request());
      await route.fulfill({ json: { category: 'Marketing', unsubscribed: true } });
    });
    const unsubscribe = new UnsubscribePage(page);
    await unsubscribe.goto(token);

    await expect(unsubscribe.heading).toHaveText('You’re unsubscribed');
    await expect(unsubscribe.lead).toHaveText(
      'You won’t get news from TuTuMuch any more. Account security and order emails still arrive.',
    );
    await expect(unsubscribe.manageEmails).toHaveAttribute('href', '/account/emails');
    expect(posts).toHaveLength(1);
    expect(posts[0].method()).toBe('POST');
    expect(posts[0].postDataJSON()).toEqual({ token });
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('an invalid link says so and points to the email settings', async ({ page }) => {
    await page.route('**/api/notifications/unsubscribe', (route) =>
      route.fulfill(problem(400, 'UNSUBSCRIBE_LINK_INVALID', 'This unsubscribe link isn’t valid')),
    );
    const unsubscribe = new UnsubscribePage(page);
    await unsubscribe.goto('tampered');

    await expect(unsubscribe.heading).toHaveText('This link doesn’t work');
    await expect(unsubscribe.lead).toHaveText(
      'It may be incomplete. You can turn off news from TuTuMuch in your email settings.',
    );
    await expect(unsubscribe.manageEmails).toBeVisible();
  });

  test('a failure shows the reference and Try again unsubscribes', async ({ page }) => {
    let calls = 0;
    await page.route('**/api/notifications/unsubscribe', async (route) => {
      calls++;
      if (calls === 1) {
        await route.fulfill(problem(500, 'INTERNAL', 'Something went wrong'));
        return;
      }
      await route.fulfill({ json: { category: 'Marketing', unsubscribed: true } });
    });
    const unsubscribe = new UnsubscribePage(page);
    await unsubscribe.goto(token);

    await expect(unsubscribe.heading).toHaveText('Couldn’t unsubscribe you');
    await expect(unsubscribe.ref).toHaveText('Ref 5c1d-0e7a');
    await unsubscribe.tryAgain.click();
    await expect(unsubscribe.heading).toHaveText('You’re unsubscribed');
    expect(calls).toBe(2);
  });
});
