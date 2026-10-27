import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { IconComponent } from '../icon/icon';

/**
 * A video's poster as one play button (design system: Video poster). It renders no video, so
 * nothing plays until `watch` is handled, typically by opening the video dialog.
 */
@Component({
  selector: 'tl-video-poster',
  imports: [IconComponent],
  templateUrl: './video-poster.html',
  styleUrl: './video-poster.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VideoPosterComponent {
  readonly poster = input.required<string>();
  readonly label = input.required<string>();
  /** Length shown in the corner, such as 0:15. */
  readonly duration = input('');
  readonly watch = output();
}
