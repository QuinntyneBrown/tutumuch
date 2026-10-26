import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  afterNextRender,
  input,
  signal,
  viewChild,
} from '@angular/core';

import { ButtonDirective } from '../button/button';
import { IconComponent } from '../icon/icon';
import { SpinnerComponent } from '../spinner/spinner';

/** One source of a video; browsers play the first type they support. */
export interface VideoSource {
  src: string;
  type: string;
}

type PlayerState = 'loading' | 'ready' | 'error';

/**
 * Captioned video with native controls plus Play/Pause and a Captions toggle, captions off until
 * the viewer turns them on (design system: Video player). Shows a spinner until it can play and a plain error with Try
 * again when every source fails. `autoplay` starts playback on render; use it only after the
 * visitor asked to watch.
 */
@Component({
  selector: 'tl-video-player',
  imports: [ButtonDirective, IconComponent, SpinnerComponent],
  templateUrl: './video-player.html',
  styleUrl: './video-player.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VideoPlayerComponent {
  readonly label = input.required<string>();
  readonly sources = input.required<VideoSource[]>();
  readonly captions = input.required<string>();
  readonly poster = input('');
  readonly autoplay = input(false);

  readonly state = signal<PlayerState>('loading');
  readonly playing = signal(false);
  readonly captionsOn = signal(false);
  /** Bumped by Try again to re-create the video element. */
  readonly attempt = signal(0);

  private readonly video = viewChild<ElementRef<HTMLVideoElement>>('video');

  constructor() {
    afterNextRender(() => this.start());
  }

  ready(): void {
    if (this.state() === 'loading') this.state.set('ready');
  }

  failed(): void {
    this.state.set('error');
    this.playing.set(false);
  }

  toggle(): void {
    const video = this.video()?.nativeElement;
    if (!video) return;
    if (video.paused) void video.play().catch(() => undefined);
    else video.pause();
  }

  toggleCaptions(): void {
    this.captionsOn.update((on) => !on);
    const track = this.video()?.nativeElement.textTracks[0];
    if (track) track.mode = this.captionsOn() ? 'showing' : 'hidden';
  }

  retry(): void {
    this.state.set('loading');
    this.attempt.update((n) => n + 1);
    setTimeout(() => this.start());
  }

  private start(): void {
    const video = this.video()?.nativeElement;
    if (!video) return;
    const track = video.textTracks[0];
    if (track) track.mode = this.captionsOn() ? 'showing' : 'hidden';
    if (this.autoplay()) void video.play().catch(() => undefined);
  }
}
