import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** One message of a thread: who wrote it and when, already formatted, the text and optional photos. */
export interface ThreadEntry {
  /** For example "You · Sat 24 Oct" or "marcus_12 · buyer · Sun 25 Oct". */
  caption: string;
  text: string;
  photos?: readonly { src: string; alt: string }[];
}

/**
 * A short conversation in time order on sunken panels, such as a buyer's problem and the seller's
 * reply (design system: Thread). Photos show as square thumbnails under the text.
 */
@Component({
  selector: 'tl-thread',
  templateUrl: './thread.html',
  styleUrl: './thread.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ThreadComponent {
  readonly entries = input.required<readonly ThreadEntry[]>();
}
