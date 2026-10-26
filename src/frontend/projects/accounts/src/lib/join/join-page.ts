import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AccountsApi, ApiProblem, shortRef } from '@tutumuch/api';
import {
  AlertComponent,
  ButtonDirective,
  ChoiceComponent,
  ErrorSummaryComponent,
  FormFieldComponent,
  IconComponent,
  SummaryError,
} from '@tutumuch/components';

/** Request field names of POST /api/auth/register mapped to control ids, in summary order. */
const FIELDS = {
  displayName: 'display-name',
  email: 'email',
  password: 'password',
  ageConfirmed: 'age',
  termsAccepted: 'terms',
} as const;

type FieldId = (typeof FIELDS)[keyof typeof FIELDS];

/** Join TuTuMuch at /join (L2-001, mocks pages/register). Client checks mirror TuTuMuch API and are never trusted. */
@Component({
  selector: 'tl-join-page',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    AlertComponent,
    ButtonDirective,
    ChoiceComponent,
    ErrorSummaryComponent,
    FormFieldComponent,
    IconComponent,
  ],
  templateUrl: './join-page.html',
  styleUrl: './join-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class JoinPage {
  private readonly api = inject(AccountsApi);

  readonly form = inject(NonNullableFormBuilder).group({
    displayName: '',
    email: '',
    password: '',
    ageConfirmed: false,
    termsAccepted: false,
  });

  readonly state = signal<'form' | 'submitting' | 'success'>('form');
  readonly errors = signal<Partial<Record<FieldId, string>>>({});
  readonly failure = signal<string | null>(null);
  readonly resend = signal<'idle' | 'busy' | 'sent' | 'limited'>('idle');
  readonly email = signal('');

  readonly summary = computed<SummaryError[]>(() =>
    Object.values(FIELDS)
      .filter((id) => this.errors()[id])
      .map((id) => ({ fieldId: id, message: this.errors()[id]! })),
  );

  async submit(): Promise<void> {
    if (this.state() === 'submitting') return;
    const value = this.form.getRawValue();
    const errors = this.check(value);
    this.failure.set(null);
    this.errors.set(errors);
    if (Object.keys(errors).length) return;

    this.state.set('submitting');
    const result = await this.api.register({
      ...value,
      displayName: value.displayName.trim(),
      email: value.email.trim(),
    });
    if (result.ok) {
      this.email.set(value.email.trim());
      this.state.set('success');
      return;
    }
    this.state.set('form');
    this.showProblem(result.problem);
  }

  async resendEmail(): Promise<void> {
    this.resend.set('busy');
    const result = await this.api.requestVerificationEmail(this.email());
    this.resend.set(result.ok ? 'sent' : 'limited');
  }

  private showProblem(problem: ApiProblem): void {
    if (problem.status === 422) {
      const errors: Partial<Record<FieldId, string>> = {};
      for (const [field, id] of Object.entries(FIELDS)) {
        const error = problem.errors[field];
        if (error) errors[id] = error.message;
      }
      this.errors.set(errors);
      return;
    }
    this.failure.set(shortRef(problem.correlationId));
  }

  private check(value: ReturnType<typeof this.form.getRawValue>): Partial<Record<FieldId, string>> {
    const errors: Partial<Record<FieldId, string>> = {};
    if (!/^[A-Za-z0-9_-]{3,30}$/.test(value.displayName.trim())) {
      errors['display-name'] = 'Use 3 to 30 letters, numbers, _ or -';
    }
    if (!/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(value.email.trim())) {
      errors.email = 'Enter an email address like name@example.com';
    }
    if (value.password.length < 12) errors.password = 'Password must be at least 12 characters';
    else if (value.password.length > 128) errors.password = 'Password must be 128 characters or fewer';
    if (!value.ageConfirmed) errors.age = 'Confirm you’re 18 or older';
    if (!value.termsAccepted) errors.terms = 'Accept the terms to continue';
    return errors;
  }
}
