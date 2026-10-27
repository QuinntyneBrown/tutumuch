import { ChangeDetectionStrategy, Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ApiProblem, ItemRequestBody, ItemRequestsApi, shortRef } from '@tutumuch/api';
import {
  ButtonDirective,
  CenterStateComponent,
  ErrorSummaryComponent,
  FixedSizeComponent,
  FormFieldComponent,
  IconComponent,
  InputGroupComponent,
  SkeletonComponent,
  ToastService,
  type SummaryError,
} from '@tutumuch/components';

import { CATEGORIES, CONDITIONS, MAX_SEARCH_LENGTH } from '../catalog/catalog-query';
import { needsSignIn } from '../my-requests/request-view';

/** The field id on the form of each request field TuTuMuch API names in a 422. */
const FIELD_IDS: Record<string, string> = {
  category: 'cat',
  size: 'cat',
  keywords: 'keywords',
  maxPrice: 'max',
  minimumCondition: 'cond',
};

const PRICE_ERROR = 'Enter a price from $1.00 to $5,000.00';

type View = 'loading' | 'form' | 'error' | 'not-found' | 'limit';

/**
 * Request an item at /account/requests/new, or edit one at /account/requests/{id}/edit (L2-097,
 * L2-098, L2-104, mocks pages/request-form). There is no size input: once a category is chosen
 * the size shows as fixed (L2-012). ?category= and ?q= fill a new form from an empty search. A
 * visitor is sent through sign-in with this URL as returnUrl and comes back (L2-105).
 */
@Component({
  selector: 'tl-request-form-page',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    ButtonDirective,
    CenterStateComponent,
    ErrorSummaryComponent,
    FixedSizeComponent,
    FormFieldComponent,
    IconComponent,
    InputGroupComponent,
    SkeletonComponent,
  ],
  templateUrl: './request-form-page.html',
  styleUrl: './request-form-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RequestFormPage implements OnInit {
  private readonly api = inject(ItemRequestsApi);
  private readonly router = inject(Router);
  private readonly toasts = inject(ToastService);

  /** The request being edited, from the route; absent for a new request. */
  readonly id = input<string>();
  /** A category slug from an empty catalog page (L2-104). */
  readonly category = input<string>();
  /** A keyword from an empty search (L2-104). */
  readonly q = input<string>();

  readonly categories = CATEGORIES;
  readonly conditions = CONDITIONS;
  readonly maxKeywords = MAX_SEARCH_LENGTH;

  readonly form = inject(NonNullableFormBuilder).group({
    category: '',
    keywords: '',
    maxPrice: '',
    minimumCondition: '',
  });
  private readonly value = toSignal(this.form.valueChanges, { initialValue: this.form.getRawValue() });

  readonly view = signal<View>('form');
  readonly loadRef = signal('');
  readonly busy = signal(false);
  readonly errors = signal<Record<string, string>>({});
  readonly summary = signal<SummaryError[]>([]);

  readonly editing = computed(() => !!this.id());
  readonly chosen = computed(() => CATEGORIES.find((c) => c.label === this.value().category) ?? null);
  readonly keywordCount = computed(() => `${(this.value().keywords ?? '').length} / ${this.maxKeywords}`);

  ngOnInit(): void {
    if (this.id()) {
      void this.load();
      return;
    }
    const category = CATEGORIES.find((c) => c.slug === this.category());
    this.form.patchValue({
      category: category?.label ?? '',
      keywords: (this.q() ?? '').trim().slice(0, this.maxKeywords),
    });
  }

  async load(): Promise<void> {
    this.view.set('loading');
    const result = await this.api.get(this.id()!);
    if (!result.ok) {
      if (this.toSignIn(result.problem)) return;
      if (result.problem.status === 404) {
        this.view.set('not-found');
        return;
      }
      this.loadRef.set(shortRef(result.problem.correlationId));
      this.view.set('error');
      return;
    }
    const request = result.value;
    this.form.setValue({
      category: request.category,
      keywords: request.keywords,
      maxPrice: request.maxPrice === null ? '' : request.maxPrice.toFixed(2),
      minimumCondition: request.minimumCondition ?? '',
    });
    this.view.set('form');
  }

  async save(): Promise<void> {
    if (this.busy()) return;
    const body = this.body();
    if (!body) {
      this.showErrors({ max: PRICE_ERROR });
      return;
    }
    this.busy.set(true);
    const id = this.id();
    const result = id ? await this.api.update(id, body) : await this.api.create(body);
    this.busy.set(false);
    if (result.ok) {
      this.toasts.show({ tone: 'success', title: 'Request saved', text: 'We’ll email you when it’s listed.' });
      await this.router.navigate(['/account/requests']);
      return;
    }
    const problem = result.problem;
    if (this.toSignIn(problem)) return;
    if (problem.status === 422) {
      const errors: Record<string, string> = {};
      for (const [field, error] of Object.entries(problem.errors)) {
        const fieldId = FIELD_IDS[field] ?? field;
        errors[fieldId] ??= error.message;
      }
      this.showErrors(errors);
      return;
    }
    if (problem.code === 'REQUEST_LIMIT_REACHED') {
      this.view.set('limit');
      return;
    }
    if (problem.status === 404) {
      this.view.set('not-found');
      return;
    }
    this.toasts.show({
      tone: 'danger',
      title: 'Something went wrong',
      text: shortRef(problem.correlationId),
      action: { label: 'Try again', run: () => void this.save() },
    });
  }

  /** The body to send, or null when the highest price is not an amount. */
  private body(): ItemRequestBody | null {
    const { category, keywords, maxPrice, minimumCondition } = this.form.getRawValue();
    const price = maxPrice.trim().replace(/^\$/, '').replace(/,/g, '');
    if (price && !/^\d+(\.\d+)?$/.test(price)) return null;
    return {
      category,
      keywords,
      maxPrice: price ? Number(price) : null,
      minimumCondition: minimumCondition || null,
    };
  }

  private showErrors(errors: Record<string, string>): void {
    this.errors.set(errors);
    const order = ['cat', 'keywords', 'max', 'cond'];
    this.summary.set(
      Object.entries(errors)
        .sort(([a], [b]) => order.indexOf(a) - order.indexOf(b))
        .map(([fieldId, message]) => ({ fieldId, message })),
    );
  }

  private toSignIn(problem: ApiProblem): boolean {
    if (!needsSignIn(problem)) return false;
    void this.router.navigate(['/sign-in'], { queryParams: { returnUrl: this.router.url } });
    return true;
  }
}
