// Acceptance Test
// Traces to: L2-088, L2-001, L2-006
// Description: "Continue with Google" shows on /sign-in and /join only when TuTuMuch API has a
// Google client, leaves for the API's start endpoint with returnUrl, and comes back with the
// callback's verdict; a new person finishes joining at /join/google under the L2-001 rules. The
// API and the trip to Google are faked with page.route; Google is never reached.

import { Page } from '@playwright/test';

import { expect, marcus, problem, serveCatalog, test } from '../fixtures';
import { GoogleJoinPage } from '../pages/google-join.page';
import { JoinPage } from '../pages/join.page';
import { ShellPage } from '../pages/shell.page';
import { SignInPage } from '../pages/sign-in.page';

/** GET /api/auth/providers. */
async function serveProviders(page: Page, google: boolean): Promise<void> {
  await page.route('**/api/auth/providers', (route) => route.fulfill({ json: { google } }));
}

/** The API's start endpoint, standing in for the whole trip to Google: it sends the browser straight back. */
async function serveGoogleStart(page: Page, backTo: string): Promise<string[]> {
  const starts: string[] = [];
  await page.route('**/api/auth/google/start**', (route) => {
    starts.push(route.request().url());
    return route.fulfill({ status: 302, headers: { location: backTo } });
  });
  return starts;
}

test.describe('continue with Google', () => {
  test('is hidden on sign-in and join without a Google client', async ({ page }) => {
    await serveProviders(page, false);
    const signIn = new SignInPage(page);
    await signIn.goto();

    await expect(signIn.submit).toBeVisible();
    await expect(signIn.google).toHaveCount(0);
    await expect(signIn.divider).toHaveCount(0);

    const join = new JoinPage(page);
    await join.goto();
    await expect(join.submit).toBeVisible();
    await expect(join.google).toHaveCount(0);
  });

  test('is hidden when the providers call fails', async ({ page }) => {
    await page.route('**/api/auth/providers', (route) =>
      route.fulfill(problem(500, 'UNEXPECTED', 'Something went wrong')),
    );
    const signIn = new SignInPage(page);
    await signIn.goto();

    await expect(signIn.submit).toBeVisible();
    await expect(signIn.google).toHaveCount(0);
  });

  test('comes first on sign-in with the divider, keeps returnUrl and leaves for the API', async ({ page }) => {
    await serveProviders(page, true);
    const starts = await serveGoogleStart(page, '/sign-in?google=cancelled');
    const signIn = new SignInPage(page);
    await signIn.goto('/items/L-20931');

    await expect(signIn.google).toBeVisible();
    await expect(signIn.divider).toBeVisible();
    await expect(signIn.google).toHaveAttribute('href', '/api/auth/google/start?returnUrl=%2Fitems%2FL-20931');
    await signIn.email.focus();
    await page.keyboard.press('Shift+Tab');
    await expect(signIn.google).toBeFocused();

    await signIn.google.click();

    await expect(signIn.status).toContainText('Google sign-in was cancelled');
    expect(starts).toEqual([
      `http://localhost:${new URL(page.url()).port}/api/auth/google/start?returnUrl=%2Fitems%2FL-20931`,
    ]);
    await expect(signIn.submit).toBeEnabled();
  });

  test('shows on join above the form', async ({ page }) => {
    await serveProviders(page, true);
    const join = new JoinPage(page);
    await join.goto();

    await expect(join.google).toBeVisible();
    await expect(join.google).toHaveAttribute('href', '/api/auth/google/start?returnUrl=%2F');
    const googleBox = await join.google.boundingBox();
    const nameBox = await join.displayName.boundingBox();
    expect(googleBox!.y).toBeLessThan(nameBox!.y);
  });

  test('an unsafe returnUrl is sent as given and the API makes it safe', async ({ page }) => {
    await serveProviders(page, true);
    const signIn = new SignInPage(page);
    await signIn.goto('https://evil.example/');

    await expect(signIn.google).toHaveAttribute('href', '/api/auth/google/start?returnUrl=%2F');
  });

  test('a refused Google sign-in comes back with the reference', async ({ page }) => {
    await serveProviders(page, true);
    const signIn = new SignInPage(page);
    await signIn.goto(undefined, { google: 'failed', ref: '7e3b41c9a0f1' });

    await expect(signIn.alert).toContainText('Couldn’t sign in with Google');
    await expect(signIn.alert).toContainText('Ref 7e3b-41c9');
    await expect(signIn.google).toBeVisible();
  });

  test('an existing unverified account is explained', async ({ page }) => {
    await serveProviders(page, true);
    const signIn = new SignInPage(page);
    await signIn.goto(undefined, { google: 'existing' });

    await expect(page.getByText('An account with this email already exists')).toBeVisible();
    await expect(page.getByText('Sign in with your password and verify your email first')).toBeVisible();
  });
});

test.describe('finish joining with Google', () => {
  test('a new person picks a display name and is signed in on returnUrl', async ({ page }) => {
    await page.route('**/api/auth/google/pending', (route) => route.fulfill({ json: { email: marcus.email } }));
    let body: unknown;
    await page.route('**/api/auth/google/finish', async (route) => {
      body = route.request().postDataJSON();
      await route.fulfill({ json: marcus });
    });
    await serveCatalog(page, 0);
    const join = new GoogleJoinPage(page);
    await join.goto('/');

    await expect(join.heading).toHaveText('Almost there');
    await expect(join.email).toHaveValue(marcus.email);
    await expect(join.email).toHaveAttribute('readonly', '');
    await join.fill({ displayName: 'marcus_12' });
    await join.submit.click();

    await expect(page).toHaveURL(/\/$/);
    await expect(new ShellPage(page).topBar.getByRole('link', { name: 'Your account, marcus_12' })).toBeVisible();
    expect(body).toEqual({ displayName: 'marcus_12', ageConfirmed: true, termsAccepted: true });
  });

  test('client checks list every problem in the summary and inline', async ({ page, unfixturedApiCalls }) => {
    await page.route('**/api/auth/google/pending', (route) => route.fulfill({ json: { email: marcus.email } }));
    const join = new GoogleJoinPage(page);
    await join.goto();

    await join.fill({ displayName: 'a b', age: false, terms: false });
    await join.submit.click();

    await expect(join.errorSummary).toBeFocused();
    for (const message of [
      'Use 3 to 30 letters, numbers, _ or -',
      'Confirm you’re 18 or older',
      'Accept the terms to continue',
    ]) {
      await expect(join.errorSummary.getByRole('link', { name: message })).toBeVisible();
    }
    await expect(join.displayName).toHaveAttribute('aria-invalid', 'true');
    await expect(join.fieldError('display-name')).toHaveText('Use 3 to 30 letters, numbers, _ or -');
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('server field errors are shown against their fields', async ({ page }) => {
    await page.route('**/api/auth/google/pending', (route) => route.fulfill({ json: { email: marcus.email } }));
    await page.route('**/api/auth/google/finish', (route) =>
      route.fulfill(
        problem(422, 'GOOGLE_JOIN_INVALID', 'Check the highlighted fields', {
          displayName: { code: 'DISPLAY_NAME_TAKEN', message: 'That display name is taken' },
        }),
      ),
    );
    const join = new GoogleJoinPage(page);
    await join.goto();

    await join.fill({ displayName: 'tutu_tessa' });
    await join.submit.click();

    await expect(join.errorSummary.getByRole('link', { name: 'That display name is taken' })).toBeVisible();
    await expect(join.fieldError('display-name')).toHaveText('That display name is taken');
  });

  test('an expired pending join says to start again', async ({ page }) => {
    await page.route('**/api/auth/google/pending', (route) =>
      route.fulfill(problem(410, 'GOOGLE_JOIN_EXPIRED', 'Start again')),
    );
    const join = new GoogleJoinPage(page);
    await join.goto();

    await expect(join.heading).toHaveText('That took a little too long');
    await expect(join.startAgain).toHaveAttribute('href', '/sign-in');
    await expect(join.backToSignIn).toHaveAttribute('href', '/sign-in');
  });

  test('a pending join that expires while the form is open says to start again', async ({ page }) => {
    await page.route('**/api/auth/google/pending', (route) => route.fulfill({ json: { email: marcus.email } }));
    await page.route('**/api/auth/google/finish', (route) =>
      route.fulfill(problem(410, 'GOOGLE_JOIN_EXPIRED', 'Start again')),
    );
    const join = new GoogleJoinPage(page);
    await join.goto();

    await join.fill({ displayName: 'marcus_12' });
    await join.submit.click();

    await expect(join.heading).toHaveText('That took a little too long');
  });

  test('a server failure keeps the input and shows a reference', async ({ page }) => {
    await page.route('**/api/auth/google/pending', (route) => route.fulfill({ json: { email: marcus.email } }));
    await page.route('**/api/auth/google/finish', (route) =>
      route.fulfill(problem(500, 'UNEXPECTED', 'Something went wrong')),
    );
    const join = new GoogleJoinPage(page);
    await join.goto();

    await join.fill({ displayName: 'marcus_12' });
    await join.submit.click();

    await expect(join.alert).toContainText('Couldn’t create your account');
    await expect(join.alert).toContainText('Ref 5c1d-0e7a');
    await expect(join.displayName).toHaveValue('marcus_12');
  });
});
