import { ChangeDetectionStrategy, Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ApiProblem, AuthState, ListingsApi, MyListing, SellingApi, VersionedListing, shortRef } from '@tutumuch/api';
import {
  AlertComponent,
  ButtonDirective,
  CenterStateComponent,
  ErrorSummaryComponent,
  IconComponent,
  StatusComponent,
  StatusTone,
} from '@tutumuch/components';

import { ListingFormComponent } from '../listing-form/listing-form';
import { ListingFormModel } from '../listing-form/listing-form-model';

type View = 'loading' | 'error' | 'form' | 'locked' | 'sold' | 'withdrawn';

/**
 * Edit an Active listing at /items/{reference}/edit (edit-and-withdraw-listing, mocks
 * pages/edit-listing). The form reads the listing with its ETag and saves under it, so a stale
 * form is refused (412) and reloads instead of overwriting newer changes. A Reserved listing is
 * locked while a buyer is in checkout; a Sold or withdrawn listing says why it cannot change
 * (L2-020). A listing removed after a cancelled order can be edited before it is relisted.
 */
@Component({
  selector: 'tl-edit-listing-page',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    AlertComponent,
    ButtonDirective,
    CenterStateComponent,
    ErrorSummaryComponent,
    IconComponent,
    ListingFormComponent,
    StatusComponent,
  ],
  templateUrl: './edit-listing-page.html',
  styleUrl: './edit-listing-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EditListingPage implements OnInit {
  private readonly auth = inject(AuthState);
  private readonly selling = inject(SellingApi);
  private readonly listings = inject(ListingsApi);
  private readonly router = inject(Router);

  /** Bound from /items/:reference/edit. */
  readonly reference = input.required<string>();

  readonly model = new ListingFormModel();
  readonly view = signal<View>('loading');
  readonly listing = signal<MyListing | null>(null);
  readonly busy = signal(false);
  readonly saved = signal(false);
  readonly changed = signal(false);
  readonly failure = signal<{ text: string; ref: string } | null>(null);
  readonly loadRef = signal('');
  readonly title = computed(() => this.listing()?.title ?? this.listing()?.category ?? '');
  readonly tone = computed(() => (this.listing()?.status.toLowerCase() ?? 'info') as StatusTone);

  /** The ETag of the version the form shows, sent back in If-Match. */
  private etag = '';

  ngOnInit(): void {
    void this.open();
  }

  async open(): Promise<void> {
    this.view.set('loading');
    await this.auth.load();
    if (!this.auth.member()) {
      await this.router.navigate(['/sign-in'], { queryParams: { returnUrl: this.router.url }, replaceUrl: true });
      return;
    }
    const [catalogue, loaded] = await Promise.all([this.selling.load(), this.listings.getForEdit(this.reference())]);
    if (!catalogue.ok || !loaded.ok) {
      this.loadRef.set(
        shortRef(
          (catalogue.ok ? null : catalogue.problem.correlationId) ?? (loaded.ok ? null : loaded.problem.correlationId),
        ),
      );
      this.view.set('error');
      return;
    }
    this.model.categories.set(catalogue.value.categories);
    this.model.options.set(catalogue.value.options);
    this.show(loaded.value);
  }

  async save(): Promise<void> {
    if (this.busy()) return;
    this.saved.set(false);
    this.changed.set(false);
    this.failure.set(null);
    if (!this.model.accept('publish')) return;
    this.busy.set(true);
    const result = await this.listings.updateListing(this.reference(), this.model.details(), this.etag);
    this.busy.set(false);
    if (result.ok) {
      this.etag = result.value.etag;
      this.listing.set(result.value.listing);
      this.saved.set(true);
      return;
    }
    this.showProblem(result.problem);
  }

  /** After a stale save: the latest version replaces the form. */
  async reload(): Promise<void> {
    const loaded = await this.listings.getForEdit(this.reference());
    if (!loaded.ok) {
      this.loadRef.set(shortRef(loaded.problem.correlationId));
      this.view.set('error');
      return;
    }
    this.changed.set(false);
    this.show(loaded.value);
  }

  private show({ listing, etag }: VersionedListing): void {
    this.listing.set(listing);
    this.etag = etag;
    if (listing.status === 'Draft') {
      void this.router.navigate(['/sell', listing.reference], { replaceUrl: true });
    } else if (listing.status === 'Reserved') {
      this.view.set('locked');
    } else if (listing.status === 'Sold') {
      this.view.set('sold');
    } else if (listing.status === 'Removed' && listing.removalReason !== 'OrderCancelled') {
      this.view.set('withdrawn');
    } else {
      this.model.fill(listing);
      this.view.set('form');
    }
  }

  private showProblem(problem: ApiProblem): void {
    if (problem.status === 412) {
      this.changed.set(true);
    } else if (problem.code === 'LISTING_RESERVED') {
      this.view.set('locked');
    } else if (problem.code === 'LISTING_SOLD') {
      this.view.set('sold');
    } else if (problem.code === 'LISTING_NOT_EDITABLE') {
      this.view.set('withdrawn');
    } else if (problem.status === 422) {
      this.model.showFieldErrors(problem);
    } else if (problem.status === 401) {
      void this.router.navigate(['/sign-in'], { queryParams: { returnUrl: this.router.url } });
    } else {
      const network = problem.status === 0;
      this.failure.set({
        text: network ? 'Check your connection.' : 'Something went wrong on our side.',
        ref: network ? '' : shortRef(problem.correlationId),
      });
    }
  }
}
