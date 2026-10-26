import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { CatalogBrand } from '@tutumuch/api';
import { ButtonDirective, ChoiceComponent, InputGroupComponent } from '@tutumuch/components';

import { CATEGORIES, CONDITIONS, CatalogQuery, categoryOf, priceError } from '../catalog/catalog-query';

/** Brands shown before "All {n} brands" expands the list. */
const TOP_BRANDS = 6;

/**
 * The filter fields shared by the sidebar and the filter sheet (mocks dialogs/filters): Category,
 * the chosen category's subcategories, Brand, Condition and Price. It is controlled: every change
 * emits the edited query and the host decides when to apply it.
 */
@Component({
  selector: 'tm-filter-form',
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
