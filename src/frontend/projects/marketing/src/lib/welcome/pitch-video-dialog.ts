import { ChangeDetectionStrategy, Component } from '@angular/core';
import { DialogComponent, VideoPlayerComponent, VideoSource } from '@tutumuch/components';

/** The short pitch video (under 30 seconds) in a dialog; it plays because the visitor pressed the poster (L2-080). */
@Component({
  selector: 'tl-pitch-video-dialog',
  imports: [DialogComponent, VideoPlayerComponent],
  templateUrl: './pitch-video-dialog.html',
  styleUrl: './pitch-video-dialog.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PitchVideoDialog {
  readonly sources: VideoSource[] = [
    { src: '/media/tutumuch-pitch.mp4', type: 'video/mp4' },
    { src: '/media/tutumuch-pitch.webm', type: 'video/webm' },
  ];
}
