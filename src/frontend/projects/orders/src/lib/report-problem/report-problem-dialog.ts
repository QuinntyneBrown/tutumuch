import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  Injector,
  afterNextRender,
  computed,
  inject,
  signal,
} from '@angular/core';
import { OrderProblem, PROBLEM_REASONS, ProblemsApi } from '@tutumuch/api';
import {
  AlertComponent,
  ButtonDirective,
  DIALOG_DATA,
  DialogComponent,
  DialogRef,
  ErrorSummaryComponent,
  FormFieldComponent,
  PhotoUploaderComponent,
  SegmentedComponent,
  SummaryError,
  UploaderTile,
} from '@tutumuch/components';

/** What Report a problem needs: the order and the end of its problem window, if it is Delivered. */
export interface ReportProblemData {
  orderId: string;
  /** "Tue 27 Oct", or null for a Shipped order, which has no deadline yet. */
  deadline: string | null;
}

const MAX_PHOTOS = 4;

/**
 * "Report a problem" (L2-036, mocks dialogs/report-problem): what's wrong, a description and up
 * to 4 photos, sent with "Send to TuTuMuch". Missing answers show the error summary and inline errors
 * without calling the API; while sending, Cancel is disabled; a failure keeps everything and
 * offers "Try again". Closes with the opened problem.
 */
@Component({
  selector: 'tl-report-problem-dialog',
  imports: [
    AlertComponent,
    ButtonDirective,
    DialogComponent,
    ErrorSummaryComponent,
    FormFieldComponent,
    PhotoUploaderComponent,
    SegmentedComponent,
  ],
  templateUrl: './report-problem-dialog.html',
  styleUrl: './report-problem-dialog.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReportProblemDialog {
  private readonly api = inject(ProblemsApi);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly injector = inject(Injector);
  readonly ref = inject<DialogRef<OrderProblem>>(DialogRef);
  readonly data = inject<ReportProblemData>(DIALOG_DATA);

  readonly labels = PROBLEM_REASONS.map((r) => r.label);
  readonly max = MAX_PHOTOS;
  readonly reason = signal<string | null>(null);
  readonly description = signal('');
  readonly photos = signal<{ key: string; file: File; src: string }[]>([]);
  readonly state = signal<'default' | 'busy' | 'failed'>('default');
  readonly reasonError = signal<string | null>(null);
  readonly descriptionError = signal<string | null>(null);
  readonly photosError = signal<string | null>(null);

  readonly tiles = computed<UploaderTile[]>(() =>
    this.photos().map((p) => ({ key: p.key, state: 'ready', src: p.src, name: p.file.name })),
  );
  readonly summary = computed<SummaryError[]>(() =>
    [
      this.reasonError() ? { fieldId: 'problem-reason-0', message: this.reasonError()! } : null,
      this.descriptionError() ? { fieldId: 'problem-description', message: this.descriptionError()! } : null,
    ].filter((e): e is SummaryError => e !== null),
  );

  private nextKey = 0;

  constructor() {
    inject(DestroyRef).onDestroy(() => this.photos().forEach((p) => URL.revokeObjectURL(p.src)));
  }

  choose(label: string): void {
    this.reason.set(label);
    this.reasonError.set(null);
  }

  add(files: File[]): void {
    const room = MAX_PHOTOS - this.photos().length;
    const added = files
      .slice(0, room)
      .map((file) => ({ key: `photo-${this.nextKey++}`, file, src: URL.createObjectURL(file) }));
    this.photos.update((photos) => [...photos, ...added]);
    this.photosError.set(null);
  }

  remove(key: string): void {
    const photo = this.photos().find((p) => p.key === key);
    if (photo) URL.revokeObjectURL(photo.src);
    this.photos.update((photos) => photos.filter((p) => p.key !== key));
  }

  async submit(): Promise<void> {
    if (this.state() === 'busy') return;
    const label = this.reason();
    const description = this.description().trim();
    this.reasonError.set(label ? null : 'Choose what’s wrong');
    this.descriptionError.set(description ? null : 'Tell us what happened');
    if (!label || !description) return;
    const reason = PROBLEM_REASONS.find((r) => r.label === label)!.value;
    this.state.set('busy');
    const result = await this.api.reportProblem(
      this.data.orderId,
      reason,
      description,
      this.photos().map((p) => p.file),
    );
    if (result.ok) {
      this.ref.close(result.value);
      return;
    }
    const errors = result.problem.errors;
    if (result.problem.status === 422 && (errors['reason'] || errors['description'] || errors['photos'])) {
      this.state.set('default');
      this.reasonError.set(errors['reason']?.message ?? null);
      this.descriptionError.set(errors['description']?.message ?? null);
      this.photosError.set(errors['photos']?.message ?? null);
      return;
    }
    if ([413, 415].includes(result.problem.status) || result.problem.code.startsWith('PHOTO_')) {
      this.state.set('default');
      this.photosError.set(result.problem.title);
      this.focus('problem-photos');
      return;
    }
    this.state.set('failed');
  }

  private focus(id: string): void {
    afterNextRender(() => this.host.nativeElement.querySelector<HTMLElement>(`#${id}`)?.focus(), {
      injector: this.injector,
    });
  }
}
