import { ChangeDetectionStrategy, Component, ElementRef, input, signal, viewChild, viewChildren } from '@angular/core';

import { ListingCardPhoto } from '../listing-card/listing-card';

/**
 * The photos of one listing (design system: Gallery): a scroll-snap track that changes photo on
 * swipe, dots below 992 px and a row of thumbnails. The first photo loads eagerly with high
 * priority. Arrow keys move between thumbnails (L2-025, L2-063).
 */
@Component({
  selector: 'tm-gallery',
  templateUrl: './gallery.html',
  styleUrl: './gallery.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GalleryComponent {
  readonly photos = input.required<ListingCardPhoto[]>();

  readonly selected = signal(0);
  /** The main view is half the page from 992 px and the full width below it. */
  readonly sizes = '(min-width: 992px) 50vw, 100vw';

  private readonly track = viewChild.required<ElementRef<HTMLElement>>('track');
  private readonly thumbs = viewChildren<ElementRef<HTMLButtonElement>>('thumb');

  show(index: number): void {
    this.selected.set(index);
    const track = this.track().nativeElement;
    track.scrollTo({ left: index * track.clientWidth });
  }

  /** Keeps the selection in step with a swipe of the track. */
  onScroll(): void {
    const track = this.track().nativeElement;
    if (track.clientWidth > 0) this.selected.set(Math.round(track.scrollLeft / track.clientWidth));
  }

  onKeydown(event: KeyboardEvent, index: number): void {
    const step = event.key === 'ArrowRight' ? 1 : event.key === 'ArrowLeft' ? -1 : 0;
    const next = index + step;
    if (step === 0 || next < 0 || next >= this.photos().length) return;
    event.preventDefault();
    this.show(next);
    this.thumbs()[next].nativeElement.focus();
  }
}
