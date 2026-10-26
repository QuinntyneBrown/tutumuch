import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  Injector,
  afterNextRender,
  computed,
  inject,
  input,
  output,
  signal,
  viewChildren,
} from '@angular/core';

import { IconComponent } from '../icon/icon';

/**
 * One tile of the uploader. A photo is uploading until it has a src; a rejected file shows its
 * name and size and does not count towards the limit.
 */
export interface UploaderTile {
  /** Stable identity for tracking and the events. */
  key: string;
  state: 'uploading' | 'ready' | 'rejected';
  /** Thumbnail of a ready photo. */
  src?: string;
  /** File name, shown while uploading and on a rejected tile. */
  name?: string;
  /** Formatted file size of a rejected tile, for example "11.4 MB". */
  size?: string;
}

export interface UploaderMove {
  from: number;
  to: number;
}

/**
 * Listing photos: a tile per photo with the cover badge on the first, rejected and uploading
 * tiles, and the "Add photos" tile with "{n} of {max}" (design system: Photo uploader). Photos
 * reorder by drag and drop, or with the left and right arrow keys on a focused tile, announced
 * in a polite live region. The host uploads files and owns the tiles. With `cover` false (evidence
 * photos of an order problem) there is no cover, no reordering and the add tile reads "Add photo".
 */
@Component({
  selector: 'tl-photo-uploader',
  imports: [IconComponent],
  templateUrl: './photo-uploader.html',
  styleUrl: './photo-uploader.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PhotoUploaderComponent {
  readonly tiles = input.required<readonly UploaderTile[]>();
  readonly max = input(8);
  readonly error = input<string | null>();
  /** Id of the file input; help is `${inputId}-help` and the error `${inputId}-error`. */
  readonly inputId = input('photos');
  /** The first photo is the listing cover and photos reorder; false for photos without an order. */
  readonly cover = input(true);

  readonly filesSelected = output<File[]>();
  readonly removed = output<string>();
  readonly moved = output<UploaderMove>();

  readonly announcement = signal('');

  /** Photos (uploading or ready) in display order; rejected files follow them. */
  readonly photos = computed(() => this.tiles().filter((t) => t.state !== 'rejected'));
  readonly rejected = computed(() => this.tiles().filter((t) => t.state === 'rejected'));
  readonly full = computed(() => this.photos().length >= this.max());
  readonly describedBy = computed(() =>
    this.error() ? `${this.inputId()}-help ${this.inputId()}-error` : `${this.inputId()}-help`,
  );

  private readonly photoTiles = viewChildren<ElementRef<HTMLElement>>('photoTile');
  private readonly injector = inject(Injector);
  private dragFrom: number | null = null;

  choose(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    input.value = '';
    if (files.length) this.filesSelected.emit(files);
  }

  onKeydown(event: KeyboardEvent, index: number): void {
    const step = event.key === 'ArrowLeft' ? -1 : event.key === 'ArrowRight' ? 1 : 0;
    if (!step || !this.cover() || event.target !== event.currentTarget) return;
    event.preventDefault();
    const to = index + step;
    if (to < 0 || to >= this.photos().length) return;
    this.move(index, to, true);
  }

  onDragStart(event: DragEvent, index: number): void {
    if (!this.cover()) return;
    this.dragFrom = index;
    event.dataTransfer?.setData('text/plain', String(index));
    if (event.dataTransfer) event.dataTransfer.effectAllowed = 'move';
  }

  onDragOver(event: DragEvent): void {
    if (this.dragFrom !== null) event.preventDefault();
  }

  onDrop(event: DragEvent, index: number): void {
    event.preventDefault();
    const from = this.dragFrom;
    this.dragFrom = null;
    if (from !== null && from !== index) this.move(from, index, false);
  }

  onDragEnd(): void {
    this.dragFrom = null;
  }

  private move(from: number, to: number, refocus: boolean): void {
    const count = this.photos().length;
    this.moved.emit({ from, to });
    this.announcement.set(
      to === 0
        ? `Photo moved to position 1 of ${count}. It is now the cover.`
        : `Photo moved to position ${to + 1} of ${count}.`,
    );
    if (refocus) {
      afterNextRender(() => this.photoTiles()[to]?.nativeElement.focus(), { injector: this.injector });
    }
  }
}
