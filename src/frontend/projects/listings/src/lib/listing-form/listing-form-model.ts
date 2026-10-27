import { DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder } from '@angular/forms';
import { map } from 'rxjs';
import { ApiProblem, Category, ListingDetails, MyListing, Photo, PhotosApi, SellingOptions } from '@tutumuch/api';
import { SummaryError, UploaderMove, UploaderTile } from '@tutumuch/components';

import {
  FIELD_IDS,
  FieldErrors,
  ListingFormValue,
  MAX_PHOTO_BYTES,
  SERVER_FIELDS,
  checkListing,
  estimateEarnings,
  formatFileSize,
  toDetails,
} from './listing-rules';

/** An uploader tile with the uploaded photo's ID and, for a refused file, why. */
interface PhotoTile extends UploaderTile {
  id?: string;
  message?: string;
}

/**
 * The state of the listing form shared by the sell page and the edit page: the controls, the
 * catalogue, the photo tiles and the field errors. Create it in an injection context. Photos
 * upload one file per request as soon as they are chosen; a save sends the uploaded IDs in display
 * order (upload-listing-photos, L2-017). There is no size input: TuTuMuch API sets it (L2-013).
 */
export class ListingFormModel {
  private readonly photoUploads = inject(PhotosApi);
  private nextPhotoKey = 0;

  readonly form = inject(NonNullableFormBuilder).group({
    category: '',
    subcategory: { value: '', disabled: true },
    title: '',
    brand: '',
    condition: '',
    flaws: '',
    description: '',
    price: '',
    shipping: '',
  });

  readonly categories = signal<Category[]>([]);
  readonly options = signal<SellingOptions | null>(null);
  readonly errors = signal<FieldErrors>({});
  readonly photos = signal<PhotoTile[]>([]);
  readonly maxPhotos = computed(() => this.options()?.maxPhotos ?? 8);
  /** The field error, then why the last refused file was refused: "Add at least one photo. Photos must be 10 MB or smaller." */
  readonly photoError = computed(() => {
    const rejected = this.photos().filter((p) => p.state === 'rejected');
    const parts = [this.errors().photos, rejected.at(-1)?.message].filter((m): m is string => !!m);
    const unique = [...new Set(parts)];
    return unique.length > 1 ? unique.map((m) => `${m}.`).join(' ') : (unique[0] ?? null);
  });

  readonly value = toSignal(this.form.valueChanges.pipe(map(() => this.raw())), { initialValue: this.raw() });
  readonly category = computed(() => this.categories().find((c) => c.name === this.value().category) ?? null);
  readonly isUsedFair = computed(() => this.value().condition === 'Used - Fair');
  readonly titleCount = computed(() => `${this.value().title.length} / 80`);
  readonly descriptionCount = computed(() => `${this.value().description.length.toLocaleString('en-US')} / 2,000`);
  readonly earnings = computed(() =>
    estimateEarnings(this.value().price, this.value().shipping, this.options()?.feePercent ?? 0),
  );
  readonly summary = computed<SummaryError[]>(() =>
    FIELD_IDS.filter((id) => this.errors()[id]).map((id) => ({ fieldId: id, message: this.errors()[id]! })),
  );

  constructor() {
    this.form.controls.category.valueChanges.pipe(takeUntilDestroyed(inject(DestroyRef))).subscribe((name) => {
      const subcategory = this.form.controls.subcategory;
      const category = this.categories().find((c) => c.name === name);
      if (!category) {
        subcategory.setValue('');
        subcategory.disable();
        return;
      }
      if (!category.subcategories.includes(subcategory.value)) subcategory.setValue('');
      subcategory.enable();
    });
  }

  raw(): ListingFormValue {
    return this.form.getRawValue() as ListingFormValue;
  }

  /** Uploads the chosen files into the free slots; files over 10 MB are refused before sending. */
  addPhotos(files: File[]): void {
    const free = this.maxPhotos() - this.photos().filter((p) => p.state !== 'rejected').length;
    for (const file of files.slice(0, Math.max(free, 0))) {
      const key = `photo-${this.nextPhotoKey++}`;
      if (file.size > MAX_PHOTO_BYTES) {
        this.photos.update((tiles) => [...tiles, this.rejectedTile(key, file, 'Photos must be 10 MB or smaller')]);
        continue;
      }
      this.photos.update((tiles) => [...tiles, { key, state: 'uploading', name: file.name }]);
      void this.photoUploads.upload(file).then((result) => {
        const tile: PhotoTile = result.ok
          ? { key, state: 'ready', id: result.value.id, src: result.value.url }
          : this.rejectedTile(key, file, this.uploadMessage(result.problem, file));
        this.photos.update((tiles) => tiles.map((t) => (t.key === key ? tile : t)));
      });
    }
  }

  removePhoto(key: string): void {
    this.photos.update((tiles) => tiles.filter((t) => t.key !== key));
  }

  /** Moves a photo among the photos; refused files stay after them. */
  movePhoto({ from, to }: UploaderMove): void {
    this.photos.update((tiles) => {
      const photos = tiles.filter((t) => t.state !== 'rejected');
      const [moved] = photos.splice(from, 1);
      photos.splice(to, 0, moved);
      return [...photos, ...tiles.filter((t) => t.state === 'rejected')];
    });
  }

  photoIds(): string[] {
    return this.photos().flatMap((p) => (p.state === 'ready' && p.id ? [p.id] : []));
  }

  /** The request body: the present fields and the photos in display order. */
  details(): ListingDetails {
    return { ...toDetails(this.raw()), photoIds: this.photoIds() };
  }

  /** Runs the client checks, plus a wait while photos are still uploading, and shows the errors; true when there are none. */
  accept(mode: 'draft' | 'publish'): boolean {
    const errors = checkListing(this.raw(), mode, this.photoIds().length);
    if (this.photos().some((p) => p.state === 'uploading')) {
      errors.photos = 'Wait for your photos to finish uploading';
    }
    this.errors.set(errors);
    return Object.keys(errors).length === 0;
  }

  /** Puts the field errors of a 422 on their controls. */
  showFieldErrors(problem: ApiProblem): void {
    const errors: FieldErrors = {};
    for (const [field, error] of Object.entries(problem.errors)) {
      const id = SERVER_FIELDS[field];
      if (id) errors[id] ??= error.message;
    }
    this.errors.set(errors);
  }

  /** Fills the form from a saved listing. */
  fill(listing: MyListing): void {
    this.photos.set(listing.photos.map((photo: Photo) => this.readyTile(photo)));
    this.form.patchValue({
      category: listing.category,
      subcategory: listing.subcategory ?? '',
      title: listing.title ?? '',
      brand: listing.brand ?? '',
      condition: listing.condition ?? '',
      flaws: listing.flaws ?? '',
      description: listing.description ?? '',
      price: listing.price === null ? '' : listing.price.toFixed(2),
      shipping: listing.shippingCost === null ? '' : listing.shippingCost.toFixed(2),
    });
    this.errors.set({});
  }

  /** A blank form for a new listing. */
  reset(): void {
    this.form.reset();
    this.photos.set([]);
    this.errors.set({});
  }

  private readyTile(photo: Photo): PhotoTile {
    return { key: `photo-${this.nextPhotoKey++}`, state: 'ready', id: photo.id, src: photo.url };
  }

  private rejectedTile(key: string, file: File, message: string): PhotoTile {
    return { key, state: 'rejected', name: file.name, size: formatFileSize(file.size), message };
  }

  /** 413, 415 and 422 carry the message to show; anything else is a failed upload. */
  private uploadMessage(problem: ApiProblem, file: File): string {
    if ([413, 415, 422].includes(problem.status)) return problem.title;
    return `Couldn’t upload ${file.name}. Try again.`;
  }
}
