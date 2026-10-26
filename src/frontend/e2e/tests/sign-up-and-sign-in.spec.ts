// Acceptance Test
// Traces to: L2-001, L2-002, L2-006, L2-008
// Description: A visitor joins, opens the verification link and signs in; the top bar then shows
// the member. Errors from Tutu Much API appear in the error summary, inline and in alerts.

import { expect, marcus, problem, signInAs, test } from '../fixtures';
import { JoinPage } from '../pages/join.page';
import { ShellPage } from '../pages/shell.page';
import { SignInPage } from '../pages/sign-in.page';
import { VerifyEmailPage } from '../pages/verify-email.page';

test.describe('join', () => {
  test('a visitor joins and is asked to check their email', async ({ page }) => {
    let body: unknown;
    await page.route('**/api/auth/register', async (route) => {
      body = route.request().postDataJSON();
      await route.fulfill({ status: 201 });
    });
    const join = new JoinPage(page);
    await join.goto();

    await join.fill({ displayName: 'marcus_12', email: 'marcus.bell@example.com', password: 'correct horse battery' });
    await join.submit.click();

    await expect(join.heading).toHaveText('Check your email');
    await expect(page.getByText('We sent a link to marcus.bell@example.com. It works for 24 hours.')).toBeVisible();
    expect(body).toEqual({
      displayName: 'marcus_12',
      email: 'marcus.bell@example.com',
      password: 'correct horse battery',
      ageConfirmed: true,
      termsAccepted: true,
    });
  });

  test('client checks list every problem in the summary and inline, focus on the summary', async ({
    page,
    unfixturedApiCalls,
  }) => {
    const join = new JoinPage(page);
    await join.goto();

    await join.fill({
      displayName: 'a b',
      email: 'marcus.bell@example.com',
      password: 'short',
      age: false,
      terms: false,
    });
    await join.submit.click();

    await expect(join.errorSummary).toBeFocused();
    for (const message of [
      'Use 3 to 30 letters, numbers, _ or -',
      'Password must be at least 12 characters',
      'Confirm you’re 18 or older',
      'Accept the terms to continue',
    ]) {
      await expect(join.errorSummary.getByRole('link', { name: message })).toBeVisible();
    }
    await expect(join.password).toHaveAttribute('aria-invalid', 'true');
    await expect(join.fieldError('password')).toHaveText('Password must be at least 12 characters');
    expect(unfixturedApiCalls).toEqual([]);
  });

  test('server field errors are shown against their fields', async ({ page }) => {
    await page.route('**/api/auth/register', (route) =>
      route.fulfill(
        problem(422, 'REGISTRATION_INVALID', 'Check the highlighted fields', {
          email: { code: 'EMAIL_TAKEN', message: 'An account with this email already exists' },
          displayName: { code: 'DISPLAY_NAME_TAKEN', message: 'That display name is taken' },
        }),
      ),
    );
    const join = new JoinPage(page);
    await join.goto();

    await join.fill({ displayName: 'tutu_tessa', email: 'sam@example.com', password: 'correct horse battery' });
    await join.submit.click();

    await expect(
      join.errorSummary.getByRole('link', { name: 'An account with this email already exists' }),
    ).toBeVisible();
    await expect(join.fieldError('display-name')).toHaveText('That display name is taken');
    await expect(join.email).toHaveAttribute('aria-invalid', 'true');
  });
});

test.describe('verify email', () => {
  test('an unused link verifies the member and offers selling', async ({ page }) => {
    let token: unknown;
    await page.route('**/api/auth/verify-email', async (route) => {
      token = route.request().postDataJSON().token;
      await route.fulfill({ status: 200 });
    });
    const verify = new VerifyEmailPage(page);

    await verify.goto('abc123');

    await expect(verify.heading).toHaveText('You’re verified');
    await expect(verify.sellAnItem).toHaveAttribute('href', '/sell');
    expect(token).toBe('abc123');
  });

  test('an expired link says so', async ({ page }) => {
    await page.route('**/api/auth/verify-email', (route) =>
      route.fulfill(problem(400, 'VERIFICATION_LINK_EXPIRED', 'This link has expired')),
    );
    const verify = new VerifyEmailPage(page);

    await verify.goto('old');

    await expect(verify.heading).toHaveText('This link has expired');
    await expect(page.getByRole('button', { name: 'Send a new link' })).toBeVisible();
  });

  test('a used link sends the member to sign in', async ({ page }) => {
    await page.route('**/api/auth/verify-email', (route) =>
      route.fulfill(problem(400, 'VERIFICATION_LINK_USED', 'Link already used')),
    );
    const verify = new VerifyEmailPage(page);

    await verify.goto('used');

    await expect(verify.heading).toHaveText('Link already used');
    await expect(verify.signIn).toBeVisible();
  });
});

test.describe('sign in', () => {
  test('a member signs in, returns to where they came from and the top bar shows them', async ({ page }) => {
    let body: unknown;
    await page.route('**/api/auth/sign-in', async (route) => {
      body = route.request().postDataJSON();
      await signInAs(page, marcus);
      await route.fulfill({ json: marcus });
    });
    const signIn = new SignInPage(page);
    await signIn.goto('/?from=sign-in');

    await signIn.signIn('marcus.bell@example.com', 'correct horse battery');

    await expect(page).toHaveURL('/?from=sign-in');
    expect(body).toEqual({ email: 'marcus.bell@example.com', password: 'correct horse battery' });
    const shell = new ShellPage(page);
    await expect(shell.topBar.getByRole('link', { name: 'Your account, marcus_12' })).toBeVisible();
    await expect(shell.signIn).toHaveCount(0);
  });

  test('a wrong email or password shows one generic message and keeps the email', async ({ page }) => {
    await page.route('**/api/auth/sign-in', (route) =>
      route.fulfill(problem(401, 'SIGN_IN_FAILED', 'Email or password is incorrect')),
    );
    const signIn = new SignInPage(page);
    await signIn.goto();

    await signIn.signIn('marcus.bell@example.com', 'wrong password');

    await expect(signIn.alert).toContainText('Email or password is incorrect');
    await expect(signIn.alert).toContainText('Check both and try again.');
    await expect(signIn.email).toHaveValue('marcus.bell@example.com');
  });

  test('a server failure shows a reference and keeps the input', async ({ page }) => {
    await page.route('**/api/auth/sign-in', (route) => route.fulfill(problem(500, 'SERVER_ERROR', 'Server error')));
    const signIn = new SignInPage(page);
    await signIn.goto();

    await signIn.signIn('marcus.bell@example.com', 'correct horse battery');

    await expect(signIn.alert).toContainText('Something went wrong');
    await expect(signIn.alert).toContainText('Ref 5c1d-0e7a');
    await expect(signIn.email).toHaveValue('marcus.bell@example.com');
  });

  test('the auth pages use the auth shell without the top bar', async ({ page }) => {
    const signIn = new SignInPage(page);
    await signIn.goto();

    await expect(page.getByRole('banner')).toHaveCount(0);
    await expect(page.locator('main#main')).toBeVisible();
    await expect(page.getByRole('link', { name: 'Join' })).toHaveAttribute('href', '/join');
  });
});
