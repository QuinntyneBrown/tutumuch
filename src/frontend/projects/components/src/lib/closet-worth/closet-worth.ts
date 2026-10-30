import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  input,
  linkedSignal,
  signal,
  untracked,
} from '@angular/core';

/** One kind of outgrown item with a typical price in whole dollars. */
export interface WorthItem {
  label: string;
  price: number;
  /** A decorative illustration, such as /items/tutu.svg. */
  image: string;
}

/**
 * "What's her closet worth?" (design system: Closet worth, ADR general/0010): toggle buttons for
 * outgrown items (`aria-pressed`) and a typical total that counts up in a polite live region,
 * beside `[actions]`. The headline is projected as `[title]` (h2); `[note]` is a short line under
 * the actions. Reduced motion changes the total at once.
 */
@Component({
  selector: 'tl-closet-worth',
  templateUrl: './closet-worth.html',
  styleUrl: './closet-worth.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ClosetWorthComponent {
  readonly headingId = input.required<string>();
  readonly eyebrow = input('');
  readonly lede = input('');
  readonly items = input.required<WorthItem[]>();
  /** Labels of the items pressed at first. */
  readonly initial = input<string[]>([]);

  readonly pressed = linkedSignal(() => new Set(this.initial()));
  readonly total = computed(() =>
    this.items()
      .filter((item) => this.pressed().has(item.label))
      .reduce((sum, item) => sum + item.price, 0),
  );
  readonly count = computed(() => this.items().filter((item) => this.pressed().has(item.label)).length);
  /** The total as shown, counting up or down to `total` over 450ms. */
  readonly shown = signal(0);
  /** Restarts the bump animation on each change. */
  readonly bump = signal(0);

  private frame = 0;

  constructor() {
    const reduce = typeof matchMedia === 'function' && matchMedia('(prefers-reduced-motion: reduce)').matches;
    let first = true;
    effect(() => {
      const target = this.total();
      untracked(() => {
        cancelAnimationFrame(this.frame);
        if (first || reduce) {
          first = false;
          this.shown.set(target);
          return;
        }
        this.bump.update((n) => n + 1);
        const from = this.shown();
        const start = performance.now();
        const tick = (now: number) => {
          const t = Math.min(1, (now - start) / 450);
          this.shown.set(Math.round(from + (target - from) * (1 - Math.pow(1 - t, 3))));
          if (t < 1) this.frame = requestAnimationFrame(tick);
        };
        this.frame = requestAnimationFrame(tick);
      });
    });
    inject(DestroyRef).onDestroy(() => cancelAnimationFrame(this.frame));
  }

  toggle(label: string): void {
    this.pressed.update((set) => {
      const next = new Set(set);
      if (!next.delete(label)) next.add(label);
      return next;
    });
  }
}
