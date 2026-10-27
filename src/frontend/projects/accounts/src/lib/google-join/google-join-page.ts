import { ChangeDetectionStrategy, Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AccountsApi, ApiProblem, AuthState, shortRef } from '@tutumuch/api';
import {
  AlertComponent,
  ButtonDirective,
  ChoiceComponent,
  ErrorSummaryComponent,
  FormFieldComponent,
  SummaryError,
} from '@tutumuch/components';

/** Request field names of POST /api/auth/google/finish mapped to control ids, in summary order. */
const FIELDS = {
  displayName: 'display-name',
  ageConfirmed: 'age',
  termsAccepted: 'terms',
} as const;

type FieldId = (typeof FIELDS)[keyof typeof FIELDS];

type State = 'loading' | 'form' | 'submitting' | 'expired';

/**
 * Finish joining with Google at /join/google (L2-088, mocks pages/join-google): the email Google
 * verified, read-only, plus the display name, age and terms of L2-001. The pending join lives
 * server-side for 10 minutes; this page never sees a Google token. Client checks mirror TuTuMuch
 * API and are never trusted.
 */
@Component({
  selector: 'tl-google-join-page',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    AlertComponent,
    ButtonDirective,
    ChoiceComponent,
    ErrorSummaryComponent,
    FormFieldComponent,
  ],
  templateUrl: './google-join-page.html',
  styleUrl: './google-join-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GoogleJoinPage implements OnInit {
  private readonly api = inject(AccountsApi);
  private readonly auth = inject(AuthState);
  private readonly router = inject(Router);

  /** Where to go once joined, bound from the query string; only same-site paths are followed. */
  readonly returnUrl = input<string>('');

  readonly form = inject(NonNullableFormBuilder).group({
    displayName: '',
    ageConfirmed: false,
    termsAccepted: false,
  });

  readonly state = signal<State>('loading');
  readonly email = signal('');
  readonly errors = signal<Partial<Record<FieldId, string>>>({});
  readonly failure = signal<string | null>(null);

  readonly summary = computed<SummaryError[]>(() =>
    Object.values(FIELDS)
      .filter((id) => this.errors()[id])
      .map((id) => ({ fieldId: id, message: this.errors()[id]! })),
  );

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state.set('loading');
    const result = await this.api.googleJoinPending();
    if (result.ok) {
      this.email.set(result.value.email);
      this.state.set('form');
      return;
    }
    this.state.set('expired');
  }

  async submit(): Promise<void> {
    if (this.state() === 'submitting') return;
    const value = this.form.getRawValue();
    const errors = this.check(value);
    this.failure.set(null);
    this.errors.set(errors);
    if (Object.keys(errors).length) return;

    this.state.set('submitting');
    const result = await this.auth.finishGoogleJoin(value.displayName.trim(), value.ageConfirmed, value.termsAccepted);
    if (result.ok) {
      await this.router.navigateByUrl(this.safeReturnUrl());
      return;
    }
    if (result.problem.status === 410) {
      this.state.set('expired');
      return;
    }
    this.state.set('form');
    this.showProblem(result.problem);
  }

  private showProblem(problem: ApiProblem): void {
    if (problem.status === 422) {
      const errors: Partial<Record<FieldId, string>> = {};
      for (const [field, id] of Object.entries(FIELDS)) {
        const error = problem.errors[field];
        if (error) errors[id] = error.message;
      }
      if (Object.keys(errors).length) {
        this.errors.set(errors);
        return;
      }
    }
    this.failure.set(shortRef(problem.correlationId));
  }

  private check(value: ReturnType<typeof this.form.getRawValue>): Partial<Record<FieldId, string>> {
    const errors: Partial<Record<FieldId, string>> = {};
    if (!/^[A-Za-z0-9_-]{3,30}$/.test(value.displayName.trim())) {
      errors['display-name'] = 'Use 3 to 30 letters, numbers, _ or -';
    }
    if (!value.ageConfirmed) errors.age = 'Confirm you’re 18 or older';
    if (!value.termsAccepted) errors.terms = 'Accept the terms to continue';
    return errors;
  }

  private safeReturnUrl(): string {
    // The router binds undefined, not the default, when the query parameter is absent.
    const url = this.returnUrl() ?? '';
    return url.startsWith('/') && !url.startsWith('//') ? url : '/';
  }
}
