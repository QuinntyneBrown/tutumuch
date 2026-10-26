import { ChangeDetectionStrategy, Component, DestroyRef, effect, inject, input, output, signal } from '@angular/core';
import { CatalogBrand } from '@tutumuch/api';

import { CatalogQuery, priceError } from '../catalog/catalog-query';
import { FilterForm } from './filter-form';

/** Price typing applies this long after the last keystroke. */
const PRICE_DELAY_MS = 400;

/**
 * The permanent filter sidebar from 992px (L2-062): choices apply at once, a price 400 ms after
 * the last keystroke and only when the range is valid (L2-024).
 */
@Component({
  selector: 'tm-filters-sidebar',
  imports: [FilterForm],
  templateUrl: './filters-sidebar.html',
  styleUrl: './filters-sidebar.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FiltersSidebar {
  readonly query = input.required<CatalogQuery>();
  readonly brands = input<readonly CatalogBrand[]>([]);
  readonly apply = output<CatalogQuery>();

  /** The form's state; it differs from the URL only while a price is being typed or is invalid. */
  readonly draft = signal<CatalogQuery | null>(null);
  private timer: ReturnType<typeof setTimeout> | undefined;

  constructor() {
    effect(() => {
      this.draft.set(this.query());
    });
    inject(DestroyRef).onDestroy(() => clearTimeout(this.timer));
  }

  edit(next: CatalogQuery): void {
    const current = this.draft() ?? this.query();
    this.draft.set(next);
    clearTimeout(this.timer);
    if (priceError(next)) return;
    if (next.min !== current.min || next.max !== current.max) {
      // The URL may have moved on (a sort, a chip) while typing; only the price comes from the form.
      this.timer = setTimeout(() => this.apply.emit({ ...this.query(), min: next.min, max: next.max }), PRICE_DELAY_MS);
    } else {
      this.apply.emit(next);
    }
  }
}
