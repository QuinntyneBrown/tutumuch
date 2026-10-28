import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { CardPhoto, SentShowcaseRequest, ShowcaseApi, ShowcaseRequest, shortRef } from '@tutumuch/api';
import {
  AlertComponent,
  ButtonDirective,
  DIALOG_DATA,
  DialogComponent,
  DialogRef,
  ErrorSummaryComponent,
  FormFieldComponent,
  ItemRowComponent,
  SummaryError,
} from '@tutumuch/components';

/** What Request this item needs: the listing and the member's request for it, if any. */
export interface RequestShowcaseItemData {
  reference: string;
  title: string;
  meta: string;
  photo: CardPhoto | null;
  existing: ShowcaseRequest | null;
}

/** How the dialog closed: the request was sent, or the session had ended. */
export type RequestShowcaseItemResult = SentShowcaseRequest | 'signed-out';

/** The longest note (L2-109). */
export const MAX_NOTE = 500;

/** "Oct 24": the date a request was first sent. */
export function requestedOn(iso: string): string {
  return new Date(iso).toLocaleDateString('en-US', { month: 'short', day: 'numeric', timeZone: 'UTC' });
}

/**
 * "Request this item" (L2-109, mocks dialogs/request-showcase-item): the listing and one optional
 * note of up to 500 characters, checked here as TuTuMuch API checks it. A saved request fills the
 * note and is updated. A failure keeps the note with its reference; 429 says the daily limit is
 * reached. Closes with the sent request, or 'signed-out' on 401.
 */
@Component({
  selector: 'tl-request-showcase-item-dialog',
  imports: [
    AlertComponent,
    ButtonDirective,
    DialogComponent,
    ErrorSummaryComponent,
    FormFieldComponent,
    ItemRowComponent,
  ],
  templateUrl: './request-showcase-item-dialog.html',
  styleUrl: './request-showcase-item-dialog.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RequestShowcaseItemDialog {
  private readonly api = inject(ShowcaseApi);
  readonly ref = inject<DialogRef<RequestShowcaseItemResult>>(DialogRef);
  readonly data = inject<RequestShowcaseItemData>(DIALOG_DATA);

  readonly note = signal(this.data.existing?.note ?? '');
  readonly state = signal<'default' | 'busy' | 'failed' | 'limited'>('default');
  readonly error = signal<string | null>(null);
  readonly failureRef = signal('');
  readonly requestedOn = this.data.existing ? requestedOn(this.data.existing.requestedAt) : null;

  readonly counter = computed(() => `${this.note().length} / ${MAX_NOTE}`);
  readonly summary = computed<SummaryError[]>(() => {
    const error = this.error();
    return error ? [{ fieldId: 'rq-note', message: error }] : [];
  });

  input(event: Event): void {
    this.note.set((event.target as HTMLTextAreaElement).value);
  }

  async submit(): Promise<void> {
    if (this.state() === 'busy') return;
    const note = this.note().trim();
    if (note.length > MAX_NOTE) {
      this.error.set('Keep the note to 500 characters');
      return;
    }
    this.error.set(null);
    this.state.set('busy');
    const result = await this.api.sendRequest(this.data.reference, note || null);
    if (result.ok) {
      this.ref.close(result.value);
      return;
    }
    const { status, errors, correlationId } = result.problem;
    if (status === 401) {
      this.ref.close('signed-out');
    } else if (status === 422 && errors['note']) {
      this.state.set('default');
      this.error.set(errors['note'].message);
    } else if (status === 429) {
      this.state.set('limited');
    } else {
      this.failureRef.set(shortRef(correlationId));
      this.state.set('failed');
    }
  }
}
