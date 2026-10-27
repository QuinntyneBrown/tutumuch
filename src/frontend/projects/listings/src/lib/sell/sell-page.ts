import { ChangeDetectionStrategy, Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ApiProblem, AuthState, ListingsApi, MyListing, SellingApi, shortRef } from '@tutumuch/api';
import {
  AlertComponent,
  ButtonDirective,
  CenterStateComponent,
  DialogService,
  ErrorSummaryComponent,
  IconComponent,
  ItemRowComponent,
} from '@tutumuch/components';

import { ListingFormComponent } from '../listing-form/listing-form';
import { ListingFormModel } from '../listing-form/listing-form-model';
import { formatUsd } from '../listing-form/listing-rules';
import { SellingNotOpenPage } from '../selling-not-open/selling-not-open-page';
import { VerifyEmailPromptComponent } from '../verify-email-prompt/verify-email-prompt';

type View = 'loading' | 'form' | 'success' | 'unverified' | 'limited' | 'product-limit' | 'not-open' | 'error';

/** What the success panel shows after publishing. */
interface Published {
  reference: string;
  title: string;
  meta: string;
  payoutOnboardingRequired: boolean;
}

/**
 * Sell an item at /sell and continue a draft at /sell/{reference} (create-listing, mocks
 * pages/sell). There is no size input: the size shown comes from the category and TuTuMuch API sets
 * the stored size (L2-012, L2-013). Photos upload one file per request as soon as they are chosen;
 * a save sends the uploaded IDs in display order (upload-listing-photos, L2-017).
 */
@Component({
  selector: 'tl-sell-page',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    SellingNotOpenPage,
    AlertComponent,
    ButtonDirective,
    CenterStateComponent,
    ErrorSummaryComponent,
    IconComponent,
    ItemRowComponent,
    ListingFormComponent,
  ],
  templateUrl: './sell-page.html',
  styleUrl: './sell-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SellPage implements OnInit {
  private readonly auth = inject(AuthState);
  private readonly selling = inject(SellingApi);
  private readonly listings = inject(ListingsApi);
  private readonly dialogs = inject(DialogService);
  private readonly router = inject(Router);

  /** The draft to continue, bound from /sell/:reference. */
  readonly reference = input<string>();

  readonly model = new ListingFormModel();
  readonly view = signal<View>('loading');
  readonly busy = signal<'draft' | 'publish' | null>(null);
  readonly draftSaved = signal(false);
  readonly failure = signal<{ heading: string; text: string; ref: string } | null>(null);
  readonly loadRef = signal('');
  readonly published = signal<Published | null>(null);
  readonly email = computed(() => this.auth.member()?.email ?? '');

  private readonly savedReference = signal<string | null>(null);

  ngOnInit(): void {
    void this.open();
  }

  async open(): Promise<void> {
    this.view.set('loading');
    await this.auth.load();
    const member = this.auth.member();
    if (!member) {
      await this.router.navigate(['/sign-in'], { queryParams: { returnUrl: this.router.url }, replaceUrl: true });
      return;
    }
    if (!member.emailVerified) {
      this.view.set('unverified');
      return;
    }
    const catalogue = await this.selling.load();
    if (!catalogue.ok) {
      this.showLoadError(catalogue.problem);
      return;
    }
    this.model.categories.set(catalogue.value.categories);
    this.model.options.set(catalogue.value.options);
    const reference = this.reference();
    if (reference) {
      const draft = await this.listings.getMine(reference);
      if (!draft.ok) {
        this.showLoadError(draft.problem);
        return;
      }
      this.savedReference.set(draft.value.reference);
      this.model.fill(draft.value);
    }
    this.view.set('form');
  }

  async saveDraft(): Promise<void> {
    if (this.busy()) return;
    if (!this.accept('draft')) return;
    this.busy.set('draft');
    const saved = await this.save();
    this.busy.set(null);
    if (saved) this.draftSaved.set(true);
  }

  async publish(): Promise<void> {
    if (this.busy()) return;
    if (!this.accept('publish')) return;
    this.busy.set('publish');
    const saved = await this.save();
    if (!saved) {
      this.busy.set(null);
      return;
    }
    const result = await this.listings.publish(saved.reference);
    this.busy.set(null);
    if (!result.ok) {
      this.showProblem(result.problem, 'Couldn’t publish your listing');
      return;
    }
    const size = saved.size.replace("'", '’');
    this.published.set({
      reference: saved.reference,
      title: saved.title ?? '',
      meta: [saved.brand, size, saved.price === null ? null : formatUsd(saved.price)].filter(Boolean).join(' · '),
      payoutOnboardingRequired: result.value.payoutOnboardingRequired,
    });
    this.view.set('success');
  }

  /** "Sell another": a blank form for a new listing. */
  reset(): void {
    this.model.reset();
    this.savedReference.set(null);
    this.published.set(null);
    this.draftSaved.set(false);
    this.failure.set(null);
    this.view.set('form');
  }

  openVerifyPrompt(): void {
    this.dialogs.open(VerifyEmailPromptComponent, { email: this.email() });
  }

  /** Creates the draft the first time, then updates it in place. */
  private async save(): Promise<MyListing | null> {
    const details = this.model.details();
    const reference = this.savedReference();
    const result = reference
      ? await this.listings.updateDraft(reference, details)
      : await this.listings.createDraft(details);
    if (!result.ok) {
      this.showProblem(result.problem, 'Couldn’t save your draft');
      return null;
    }
    this.savedReference.set(result.value.reference);
    return result.value;
  }

  /** Shows client errors; true when there are none. */
  private accept(mode: 'draft' | 'publish'): boolean {
    this.draftSaved.set(false);
    this.failure.set(null);
    return this.model.accept(mode);
  }

  private showProblem(problem: ApiProblem, heading: string): void {
    if (problem.status === 422) {
      this.model.showFieldErrors(problem);
    } else if (problem.status === 429) {
      this.view.set('limited');
    } else if (problem.status === 409 && problem.code === 'PRODUCT_LIMIT_REACHED') {
      this.view.set('product-limit');
    } else if (problem.status === 403 && problem.code === 'SELLING_NOT_OPEN') {
      this.view.set('not-open');
    } else if (problem.status === 403 && problem.code === 'EMAIL_NOT_VERIFIED') {
      this.view.set('unverified');
    } else if (problem.status === 401) {
      void this.router.navigate(['/sign-in'], { queryParams: { returnUrl: this.router.url } });
    } else {
      const network = problem.status === 0;
      this.failure.set({
        heading,
        text: network ? 'Check your connection.' : 'Something went wrong on our side.',
        ref: network ? '' : shortRef(problem.correlationId),
      });
    }
  }

  private showLoadError(problem: ApiProblem): void {
    this.loadRef.set(shortRef(problem.correlationId));
    this.view.set('error');
  }
}
