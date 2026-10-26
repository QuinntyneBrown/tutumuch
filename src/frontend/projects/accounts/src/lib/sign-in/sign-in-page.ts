import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthState, shortRef } from '@tutumuch/api';
import { AlertComponent, ButtonDirective, FormFieldComponent } from '@tutumuch/components';

/** Sign in at /sign-in, returning to returnUrl afterwards (L2-006, mocks pages/sign-in). */
@Component({
  selector: 'tm-sign-in-page',
  imports: [ReactiveFormsModule, RouterLink, AlertComponent, ButtonDirective, FormFieldComponent],
  templateUrl: './sign-in-page.html',
  styleUrl: './sign-in-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SignInPage {
  private readonly auth = inject(AuthState);
  private readonly router = inject(Router);

  /** Where to go after signing in, bound from the query string; only same-site paths are followed. */
  readonly returnUrl = input<string>('');
  /** Buy now on a listing sends a visitor here with the listing as returnUrl (L2-026). */
  readonly toBuy = computed(() => /^\/items\/[^/]+$/.test(this.returnUrl()));

  readonly form = inject(NonNullableFormBuilder).group({ email: '', password: '' });
  readonly busy = signal(false);
  readonly failure = signal<{ kind: 'incorrect' } | { kind: 'error'; ref: string } | null>(null);

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

  private safeReturnUrl(): string {
    const url = this.returnUrl();
    return url.startsWith('/') && !url.startsWith('//') ? url : '/';
  }
}
