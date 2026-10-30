import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  NgZone,
  afterNextRender,
  inject,
  input,
  signal,
  viewChild,
} from '@angular/core';

import { IconComponent, IconName } from '../icon/icon';

/** One numbered step; `icon` marks its station in the journey. */
export interface Step {
  title: string;
  text: string;
  icon?: IconName;
}

/**
 * Numbered steps on a red top rule, titles as h3 (design system: Steps). With `journey` the steps
 * sit on a rail that fills as the list scrolls through view, and each step's icon lights up when
 * the fill reaches it (ADR general/0010). Without script or under reduced motion every step is lit.
 */
@Component({
  selector: 'tl-steps',
  imports: [IconComponent],
  templateUrl: './steps.html',
  styleUrl: './steps.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StepsComponent {
  readonly steps = input.required<Step[]>();
  readonly journey = input(false);

  /** True once the scroll-driven fill runs; until then every station shows lit. */
  readonly live = signal(false);
  /** How many stations the fill has reached. */
  readonly lit = signal(0);

  private readonly journeyRef = viewChild<ElementRef<HTMLElement>>('journey');

  constructor() {
    const zone = inject(NgZone);
    const destroyRef = inject(DestroyRef);
    afterNextRender(() => {
      const el = this.journeyRef()?.nativeElement;
      if (!el || matchMedia('(prefers-reduced-motion: reduce)').matches) return;
      const rail = el.querySelector<HTMLElement>('.journey__rail')!;
      const stations = el.querySelectorAll<HTMLElement>('.journey__station');
      let queued = false;

      // Stacked (phone) layout: end the rail at the last station's centre, not below its text.
      const fitRail = () => {
        const box = el.getBoundingClientRect();
        const first = stations[0].getBoundingClientRect();
        const last = stations[stations.length - 1].getBoundingClientRect();
        rail.style.bottom = last.top > first.top + 1 ? `${box.bottom - (last.top + last.height / 2)}px` : '';
      };
      const update = () => {
        queued = false;
        const rect = el.getBoundingClientRect();
        const vh = window.innerHeight;
        // 0 when the journey's top reaches 85% of the viewport, 1 when its bottom reaches 55%.
        const p = Math.max(0, Math.min(1, (vh * 0.85 - rect.top) / (rect.height + vh * 0.3)));
        el.style.setProperty('--journey', p.toFixed(3));
        const n = stations.length;
        const reached = Array.from(stations).filter((_, i) => p >= i / Math.max(1, n - 1) - 0.001).length;
        if (reached !== this.lit()) zone.run(() => this.lit.set(reached));
      };
      const queue = () => {
        if (!queued) {
          queued = true;
          requestAnimationFrame(update);
        }
      };
      const onResize = () => {
        fitRail();
        queue();
      };
      this.live.set(true);
      zone.runOutsideAngular(() => {
        window.addEventListener('scroll', queue, { passive: true });
        window.addEventListener('resize', onResize);
      });
      fitRail();
      update();
      destroyRef.onDestroy(() => {
        window.removeEventListener('scroll', queue);
        window.removeEventListener('resize', onResize);
      });
    });
  }
}
