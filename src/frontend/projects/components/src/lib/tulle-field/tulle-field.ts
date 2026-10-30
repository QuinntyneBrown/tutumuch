import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  NgZone,
  afterNextRender,
  inject,
  viewChild,
} from '@angular/core';

interface Petal {
  x: number;
  y: number;
  size: number;
  depth: number;
  angle: number;
  spin: number;
  sway: number;
  vx: number;
  vy: number;
  colour: string;
  frills: number;
}

interface Spark {
  x: number;
  y: number;
  vx: number;
  vy: number;
  angle: number;
  size: number;
  life: number;
  max: number;
  colour: string;
}

const TAU = Math.PI * 2;
const TOKENS = ['--tulle-a', '--tulle-b', '--tulle-c', '--tulle-d'];

/**
 * The tulle field behind the marketing hero (design system: Tulle field, ADR general/0010): a
 * decorative canvas of translucent tulle petals that drift up and twirl around the pointer; a
 * press throws a burst of sparkles. Colours come from the `--color-tulle-*` tokens, so the theme
 * follows. Under reduced motion it paints one still frame; offscreen it sleeps. Place it in a
 * positioned parent (the hero's `[backdrop]` slot); it listens to pointer events on that parent.
 */
@Component({
  selector: 'tl-tulle-field',
  template: '<canvas #canvas class="tulle-field" aria-hidden="true"></canvas>',
  styleUrl: './tulle-field.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TulleFieldComponent {
  private readonly canvasRef = viewChild.required<ElementRef<HTMLCanvasElement>>('canvas');
  private readonly element: HTMLElement = inject(ElementRef).nativeElement;
  private readonly zone = inject(NgZone);
  private readonly destroyRef = inject(DestroyRef);

  constructor() {
    afterNextRender(() => this.zone.runOutsideAngular(() => this.start()));
  }

  private start(): void {
    const canvas = this.canvasRef().nativeElement;
    const ctx = canvas.getContext('2d');
    // The host is display: contents, so the box to fill and listen on is its parent.
    const host = this.element.parentElement;
    if (!ctx || !host) return;
    const reduce = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    let w = 0;
    let h = 0;
    let palette: string[] = [];
    let petals: Petal[] = [];
    let sparks: Spark[] = [];
    let visible = true;
    let frame = 0;
    const pointer = { x: 0, y: 0, active: false };

    const readPalette = () => {
      const style = getComputedStyle(canvas);
      palette = TOKENS.map((name) => style.getPropertyValue(name).trim()).filter(Boolean);
      if (!palette.length) palette = ['currentColor'];
    };
    const pick = () => palette[Math.floor(Math.random() * palette.length)];
    const petal = (anywhere: boolean): Petal => {
      const size = 10 + Math.random() * 26;
      return {
        x: Math.random() * w,
        y: anywhere ? Math.random() * h : h + size * 2,
        size,
        depth: 0.4 + Math.random() * 0.6,
        angle: Math.random() * TAU,
        spin: (Math.random() - 0.5) * 0.02,
        sway: Math.random() * TAU,
        vx: 0,
        vy: 0,
        colour: pick(),
        frills: 5 + Math.floor(Math.random() * 3),
      };
    };
    const resize = () => {
      const rect = host.getBoundingClientRect();
      const dpr = Math.min(window.devicePixelRatio || 1, 2);
      w = rect.width;
      h = rect.height;
      canvas.width = Math.round(w * dpr);
      canvas.height = Math.round(h * dpr);
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
      const count = Math.round(Math.min(70, Math.max(24, (w * h) / 16000)));
      petals = Array.from({ length: count }, () => petal(true));
    };

    // A tulle petal: a scalloped fan, like one layer of a tutu seen from above.
    const drawPetal = (p: Petal) => {
      const spread = Math.PI * 0.9;
      const step = spread / p.frills;
      ctx.save();
      ctx.translate(p.x, p.y);
      ctx.rotate(p.angle);
      ctx.globalAlpha = 0.18 + p.depth * 0.32;
      ctx.fillStyle = p.colour;
      ctx.strokeStyle = p.colour;
      ctx.beginPath();
      ctx.moveTo(0, 0);
      for (let i = 0; i <= p.frills; i++) {
        const a = -spread / 2 + step * i;
        if (i === 0) ctx.lineTo(Math.cos(a) * p.size, Math.sin(a) * p.size);
        else {
          const mid = a - step / 2;
          ctx.quadraticCurveTo(
            Math.cos(mid) * p.size * 1.22,
            Math.sin(mid) * p.size * 1.22,
            Math.cos(a) * p.size,
            Math.sin(a) * p.size,
          );
        }
      }
      ctx.closePath();
      ctx.fill();
      ctx.globalAlpha *= 0.7;
      ctx.lineWidth = 0.75;
      ctx.stroke();
      ctx.restore();
    };
    const drawSpark = (s: Spark) => {
      const r = s.size * (s.life / s.max);
      ctx.save();
      ctx.translate(s.x, s.y);
      ctx.rotate(s.angle);
      ctx.globalAlpha = Math.min(1, s.life / 20);
      ctx.fillStyle = s.colour;
      ctx.beginPath();
      for (let i = 0; i < 8; i++) {
        const rr = i % 2 ? r * 0.28 : r;
        ctx.lineTo(Math.cos((i * TAU) / 8) * rr, Math.sin((i * TAU) / 8) * rr);
      }
      ctx.closePath();
      ctx.fill();
      ctx.restore();
    };
    const burst = (x: number, y: number, n: number) => {
      for (let i = 0; i < n; i++) {
        const a = Math.random() * TAU;
        const v = 1.5 + Math.random() * 4;
        sparks.push({
          x,
          y,
          vx: Math.cos(a) * v,
          vy: Math.sin(a) * v - 1.5,
          angle: a,
          size: 4 + Math.random() * 7,
          life: 50 + Math.random() * 30,
          max: 80,
          colour: palette[i % palette.length],
        });
      }
    };

    const paint = (t: number) => {
      ctx.clearRect(0, 0, w, h);
      petals.forEach((p, i) => {
        // Twirl: a tangential pull around the pointer, strongest close in.
        if (pointer.active) {
          const dx = p.x - pointer.x;
          const dy = p.y - pointer.y;
          const d = Math.hypot(dx, dy) || 1;
          if (d < 220) {
            const f = (1 - d / 220) * 0.9 * p.depth;
            p.vx += (-dy / d) * f + (dx / d) * f * 0.25;
            p.vy += (dx / d) * f + (dy / d) * f * 0.25;
            p.spin += 0.002 * f;
          }
        }
        p.vx *= 0.94;
        p.vy *= 0.94;
        p.spin *= 0.995;
        p.sway += 0.012;
        p.x += p.vx + Math.sin(p.sway + t * 0.0004) * 0.35 * p.depth;
        p.y += p.vy - (0.25 + p.depth * 0.45);
        p.angle += p.spin + 0.003;
        if (p.y < -p.size * 2 || p.x < -80 || p.x > w + 80) petals[i] = petal(false);
        drawPetal(p);
      });
      sparks = sparks.filter((s) => {
        s.vx *= 0.96;
        s.vy = s.vy * 0.96 + 0.06;
        s.x += s.vx;
        s.y += s.vy;
        s.angle += 0.08;
        s.life -= 1;
        if (s.life > 0) drawSpark(s);
        return s.life > 0;
      });
    };
    const loop = (t: number) => {
      frame = 0;
      if (!visible) return;
      paint(t);
      frame = requestAnimationFrame(loop);
    };
    const run = () => {
      if (!reduce && !frame) frame = requestAnimationFrame(loop);
    };

    readPalette();
    resize();
    if (reduce) paint(0);
    else run();

    const onResize = () => {
      resize();
      if (reduce) paint(0);
    };
    const local = (e: PointerEvent) => {
      const rect = host.getBoundingClientRect();
      return { x: e.clientX - rect.left, y: e.clientY - rect.top };
    };
    const onMove = (e: PointerEvent) => Object.assign(pointer, local(e), { active: true });
    const onLeave = () => (pointer.active = false);
    const onDown = (e: PointerEvent) => {
      const at = local(e);
      burst(at.x, at.y, (e.target as Element).closest('a, button') ? 28 : 18);
    };
    window.addEventListener('resize', onResize);
    if (!reduce) {
      host.addEventListener('pointermove', onMove);
      host.addEventListener('pointerleave', onLeave);
      host.addEventListener('pointerdown', onDown);
    }
    // A theme change (the data-theme toggle or the system scheme) recolours the petals.
    const recolour = () => {
      readPalette();
      petals.forEach((p) => (p.colour = pick()));
      if (reduce) paint(0);
    };
    const themes = new MutationObserver(recolour);
    themes.observe(document.documentElement, { attributes: true, attributeFilter: ['data-theme'] });
    const scheme = window.matchMedia('(prefers-color-scheme: dark)');
    scheme.addEventListener('change', recolour);
    const seen = new IntersectionObserver(([entry]) => {
      visible = entry.isIntersecting;
      if (visible) run();
    });
    seen.observe(host);

    this.destroyRef.onDestroy(() => {
      cancelAnimationFrame(frame);
      window.removeEventListener('resize', onResize);
      host.removeEventListener('pointermove', onMove);
      host.removeEventListener('pointerleave', onLeave);
      host.removeEventListener('pointerdown', onDown);
      scheme.removeEventListener('change', recolour);
      themes.disconnect();
      seen.disconnect();
    });
  }
}
