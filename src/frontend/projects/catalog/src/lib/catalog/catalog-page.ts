import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CatalogApi, CatalogBrand } from '@tutumuch/api';
import {
  AlertComponent,
  ButtonDirective,
  ChipComponent,
  EmptyStateComponent,
  IconComponent,
  ListingGridComponent,
  LoadMoreComponent,
  SheetService,
  SizeTagComponent,
} from '@tutumuch/components';

import { CatalogStore } from './catalog-store';
import {
  CATEGORIES,
  CatalogQuery,
  CatalogSort,
  EMPTY_QUERY,
  SORTS,
  canonicalParams,
  chipsOf,
  hasFilters,
  parseQuery,
  toApiParams,
  toParams,
} from './catalog-query';

import { toCardView } from '../card-view';
import { FiltersSheet, FiltersSheetData } from '../filters/filters-sheet';
import { FiltersSidebar } from '../filters/filters-sidebar';

/**
 * The catalog at / and a category page at /shoes, /tops... (mocks pages/catalog). The URL is the
 * one source of a result set (L2-024): every change of its query parameters is parsed into a
 * CatalogQuery and loaded, and every filter, chip, sort or search change navigates. The header is
 * the hero, a category header, "Shop" or the search term. Filters are a sidebar from 992px and a
 * sheet below it (L2-062).
 */
@Component({
  selector: 'tm-catalog-page',
  imports: [
    DecimalPipe,
    RouterLink,
    AlertComponent,
    ButtonDirective,
    ChipComponent,
    EmptyStateComponent,
    FiltersSidebar,
    IconComponent,
    ListingGridComponent,
    LoadMoreComponent,
    SizeTagComponent,
  ],
  providers: [CatalogStore],
  templateUrl: './catalog-page.html',
  styleUrl: './catalog-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CatalogPage implements OnInit {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly sheets = inject(SheetService);
  private readonly api = inject(CatalogApi);
  readonly store = inject(CatalogStore);
  /**
   * From the route data of a category page; absent at /. Not named category, because input
   * binding would fill it from the ?category= query parameter as well.
   */
  readonly pageCategory = input<string>();

  readonly sorts = SORTS;
  readonly brands = signal<readonly CatalogBrand[]>([]);
  private readonly params = toSignal(this.route.queryParamMap, { requireSync: true });

  readonly query = computed<CatalogQuery>(() => {
    const query = parseQuery(this.params());
    const fixed = CATEGORIES.find((c) => c.label === this.pageCategory());
    return fixed ? { ...query, category: fixed.slug, subcategories: [] } : query;
  });
  readonly categoryHeader = computed(() => {
    const query = this.query();
    const category = CATEGORIES.find((c) => c.slug === query.category);
    const only = !query.q && !hasFilters({ ...query, category: null });
    return category && only ? category : null;
  });
  readonly filtered = computed(() => !!this.query().q || hasFilters(this.query()));
  readonly chips = computed(() => chipsOf(this.query()));
  readonly cards = computed(() => this.store.items().map(toCardView));

  /** Runs after the route-data input is bound, so a category page is known. */
  ngOnInit(): void {
    let loaded = '';
    this.route.queryParamMap.subscribe((params) => {
      const query = this.query();
      const canonical = this.pageCategory() ? null : canonicalParams(params, query);
      if (canonical) {
        void this.router.navigate([], { queryParams: canonical, replaceUrl: true });
        return;
      }
      const key = JSON.stringify(toApiParams(query));
      if (key === loaded) return;
      loaded = key;
      void this.store.load(toApiParams(query));
    });
    void this.api.brands().then((result) => {
      if (result.ok) this.brands.set(result.value);
    });
  }

  /** Navigates to the catalog with this result set; a category page leaves for / with its category kept. */
  show(query: CatalogQuery): void {
    void this.router.navigate(['/'], { queryParams: toParams(query) });
  }

  sortBy(sort: string): void {
    this.show({ ...this.query(), sort: sort as CatalogSort });
  }

  clearAll(): void {
    this.show(EMPTY_QUERY);
  }

  async openFilters(): Promise<void> {
    const data: FiltersSheetData = { query: this.query(), brands: this.brands() };
    const result = await this.sheets.open<FiltersSheet, FiltersSheetData, CatalogQuery>(FiltersSheet, data).closed;
    if (result) this.show(result);
  }
}
