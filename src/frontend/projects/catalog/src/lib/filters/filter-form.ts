import { ChangeDetectionStrategy, Component, computed, inject, input, output, signal } from '@angular/core';
import { CatalogBrand, LaunchState } from '@tutumuch/api';
import { ButtonDirective, ChoiceComponent, InputGroupComponent } from '@tutumuch/components';

import { CATEGORIES, CONDITIONS, CatalogQuery, categoryOf, priceError } from '../catalog/catalog-query';

/** Brands shown before "All {n} brands" expands the list. */
const TOP_BRANDS = 6;

/**
 * The filter fields shared by the sidebar and the filter sheet (mocks dialogs/filters). The three
 * most useful come first and open (page guidelines 8): Category with the chosen category's
 * subcategories, Size (the one accepted size, L2-013) and Condition; Brand and Price sit behind
 * "More filters", which opens by itself while either is set. It is controlled: every change
 * emits the edited query and the host decides when to apply it. In launch Phase 1 there is no
 * Category group, so no subcategories either (L2-093).
 */
@Component({
  selector: 'tl-filter-form',
  imports: [ButtonDirective, ChoiceComponent, InputGroupComponent],
  templateUrl: './filter-form.html',
  styleUrl: './filter-form.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FilterForm {
  readonly query = input.required<CatalogQuery>();
  readonly brands = input<readonly CatalogBrand[]>([]);
  /** Prefix of every id, so the sidebar and the sheet can be in the page together. */
  readonly idPrefix = input.required<string>();
  readonly changed = output<CatalogQuery>();

  readonly launch = inject(LaunchState);
  readonly categories = CATEGORIES;
  readonly conditions = CONDITIONS;
  readonly allBrands = signal(false);

  readonly category = computed(() => categoryOf(this.query()));
  readonly error = computed(() => priceError(this.query()));
  /** The top brands, plus any chosen brand outside them, or every brand once expanded. */
  readonly shownBrands = computed(() => {
    const names = this.brands().map((b) => b.name);
    if (this.allBrands()) return names;
    const top = names.slice(0, TOP_BRANDS);
    return [...top, ...this.query().brands.filter((b) => !top.includes(b))];
  });
  readonly moreBrands = computed(() => !this.allBrands() && this.brands().length > TOP_BRANDS);
  /** Whether the visitor opened "More filters" themself, so clearing a brand does not fold it away. */
  readonly userOpened = signal(false);
  /** "More filters" is open while the visitor opened it, a brand or price is set, or the price is invalid. */
  readonly moreOpen = computed(() => {
    const query = this.query();
    return this.userOpened() || query.brands.length > 0 || !!query.min || !!query.max || !!this.error();
  });

  setCategory(slug: string | null): void {
    this.changed.emit({ ...this.query(), category: slug, subcategories: [] });
  }

  toggle(field: 'subcategories' | 'brands' | 'conditions', value: string, checked: boolean): void {
    const values = this.query()[field].filter((v) => v !== value);
    this.changed.emit({ ...this.query(), [field]: checked ? [...values, value] : values });
  }

  setPrice(field: 'min' | 'max', value: string): void {
    this.changed.emit({ ...this.query(), [field]: value.trim().replace(/^\$/, '') });
  }
}
