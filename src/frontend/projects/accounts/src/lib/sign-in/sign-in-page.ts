import { ChangeDetectionStrategy, Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AccountsApi, AuthState, shortRef } from '@tutumuch/api';
import {
  AlertComponent,
  ButtonDirective,
  FormFieldComponent,
  OrDividerComponent,
  ProviderButtonComponent,
} from '@tutumuch/components';

/** What the Google callback sent the visitor back with (L2-088, mocks pages/sign-in google-* states). */
type GoogleResult = 'cancelled' | 'existing' | 'failed';

/** Sign in at /sign-in, returning to returnUrl afterwards (L2-006, L2-088, mocks pages/sign-in). */
@Component({
  selector: 'tl-sign-in-page',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    AlertComponent,
    ButtonDirective,
    FormFieldComponent,
    OrDividerComponent,
    ProviderButtonComponent,
  ],
  templateUrl: './sign-in-page.html',
  styleUrl: './sign-in-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SignInPage implements OnInit {
  private readonly auth = inject(AuthState);
  private readonly api = inject(AccountsApi);
  private readonly router = inject(Router);

  /** Where to go after signing in, bound from the query string; only same-site paths are followed. */
  readonly returnUrl = input<string>('');
  /** The Google callback's verdict, bound from the query string: cancelled, existing or failed. */
  readonly google = input<string>('');
  /** The reference the callback sent with a failure. */
  readonly ref = input<string>('');
  /** Buy now on a listing sends a visitor here with the listing as returnUrl (L2-026). */
  readonly toBuy = computed(() => /^\/items\/[^/]+$/.test(this.returnUrl() ?? ''));

  readonly form = inject(NonNullableFormBuilder).group({ email: '', password: '' });
  readonly busy = signal(false);
  readonly failure = signal<{ kind: 'incorrect' } | { kind: 'error'; ref: string } | null>(null);
  /** True once TuTuMuch API said a Google client is configured; the button renders only then. */
  readonly googleOffered = signal(false);
  readonly leavingForGoogle = signal(false);
  readonly googleResult = computed<GoogleResult | null>(() => {
    const value = this.google();
    return value === 'cancelled' || value === 'existing' || value === 'failed' ? value : null;
  });
  readonly googleRef = computed(() => shortRef(this.ref() ?? ''));
  readonly googleStartUrl = computed(() => this.api.googleStartUrl(this.safeReturnUrl()));

  ngOnInit(): void {
    void this.loadProviders();
  }

  async submit(): Promise<void> {
    if (this.busy()) return;
    const { email, password } = this.form.getRawValue();
    if (!email.trim() || !password) {
      this.failure.set({ kind: 'incorrect' });
      return;
    }
    this.busy.set(true);
    this.failure.set(null);
    const result = await this.auth.signIn(email.trim(), password);
    this.busy.set(false);
    if (result.ok) {
      await this.router.navigateByUrl(this.safeReturnUrl());
      return;
    }
    this.form.controls.password.setValue('');
    this.failure.set(
      result.problem.status === 401
        ? { kind: 'incorrect' }
        : { kind: 'error', ref: shortRef(result.problem.correlationId) },
    );
  }

  private async loadProviders(): Promise<void> {
    const result = await this.api.providers();
    this.googleOffered.set(result.ok && result.value.google);
  }

  private safeReturnUrl(): string {
    // The router binds undefined, not the default, when the query parameter is absent.
    const url = this.returnUrl() ?? '';
    return url.startsWith('/') && !url.startsWith('//') ? url : '/';
  }
}
