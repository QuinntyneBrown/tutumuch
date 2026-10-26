import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { CatalogApi, CatalogBrand } from '@tutumuch/api';
import {
  ButtonDirective,
  DIALOG_DATA,
  DialogRef,
  ErrorSummaryComponent,
  SheetComponent,
  SummaryError,
} from '@tutumuch/components';

import { CatalogQuery, EMPTY_QUERY, priceError, toApiParams } from '../catalog/catalog-query';
import { FilterForm } from './filter-form';

export interface FiltersSheetData {
  query: CatalogQuery;
  brands: readonly CatalogBrand[];
}

/** The live count refreshes this long after the last edit. */
const COUNT_DELAY_MS = 300;

/**
 * The filter sheet under 992px (mocks dialogs/filters, L2-062): full screen under 768px, a side
 * panel from 768px. It edits a draft; the apply button shows the live count, Clear resets the
 * draft without closing, and apply closes with the draft. The keyword and sort are kept.
 */
@Component({
  selector: 'tl-filters-sheet',
  imports: [ButtonDirective, ErrorSummaryComponent, FilterForm, SheetComponent],
  templateUrl: './filters-sheet.html',
  styleUrl: './filters-sheet.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FiltersSheet {
  private readonly api = inject(CatalogApi);
  private readonly ref = inject<DialogRef<CatalogQuery>>(DialogRef);
  readonly data = inject<FiltersSheetData>(DIALOG_DATA);

  readonly draft = signal(this.data.query);
  readonly count = signal<number | null>(null);
  readonly busy = signal(true);
  private timer: ReturnType<typeof setTimeout> | undefined;
  private sequence = 0;

  readonly error = computed(() => priceError(this.draft()));
  readonly summary = computed<SummaryError[]>(() => {
    const error = this.error();
    return error ? [{ fieldId: 'sheet-min', message: error }] : [];
  });
  readonly label = computed(() => {
    const count = this.count();
    if (this.error() || this.busy() || count === null) return 'Show items';
    return count === 1 ? 'Show 1 item' : `Show ${count.toLocaleString('en-US')} items`;
  });

  constructor() {
    void this.refresh(this.draft());
    inject(DestroyRef).onDestroy(() => clearTimeout(this.timer));
  }

  edit(next: CatalogQuery): void {
    this.draft.set(next);
    clearTimeout(this.timer);
    this.sequence++;
    if (priceError(next)) {
      this.busy.set(false);
      return;
    }
    this.busy.set(true);
    this.timer = setTimeout(() => void this.refresh(next), COUNT_DELAY_MS);
  }

  clear(): void {
    const { q, sort } = this.draft();
    this.edit({ ...EMPTY_QUERY, q, sort });
  }

  apply(): void {
    if (!this.error()) this.ref.close(this.draft());
  }

  private async refresh(query: CatalogQuery): Promise<void> {
    const sequence = ++this.sequence;
    const result = await this.api.count(toApiParams(query));
    if (sequence !== this.sequence) return;
    this.busy.set(false);
    this.count.set(result.ok ? result.value.totalCount : null);
  }
}
