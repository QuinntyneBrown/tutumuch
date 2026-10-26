import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

/** One wordmark letter: it bounces on its own baseline (dy in em, rot in degrees), as in the brand kit. */
interface WordmarkLetter {
  readonly ch: string;
  readonly dy: number;
  readonly rot: number;
  readonly scale: number;
  readonly fill: 'dots' | 'gingham';
}

const LETTERS: readonly WordmarkLetter[] = [
  { ch: 'T', dy: -0.02, rot: -7, scale: 1.08, fill: 'dots' },
  { ch: 'u', dy: 0.07, rot: 5, scale: 1, fill: 'dots' },
  { ch: 'T', dy: -0.05, rot: -3, scale: 1.08, fill: 'dots' },
  { ch: 'u', dy: 0.06, rot: 6, scale: 1, fill: 'dots' },
  { ch: 'M', dy: -0.03, rot: -5, scale: 1.08, fill: 'gingham' },
  { ch: 'u', dy: 0.07, rot: 4, scale: 1, fill: 'gingham' },
  { ch: 'c', dy: 0.02, rot: -4, scale: 1, fill: 'gingham' },
  { ch: 'h', dy: -0.04, rot: 6, scale: 1, fill: 'gingham' },
];

/**
 * The TuTuMuch wordmark as a link home: polka-dot "TuTu", gingham "Much", plum outline and a
 * raspberry drop shadow (brand kit: Gingham wordmark). Sizes sm, md, lg (design system: Top bar, Footer).
 */
@Component({
  selector: 'tl-logo',
  imports: [RouterLink],
  templateUrl: './logo.html',
  styleUrl: './logo.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LogoComponent {
  readonly size = input<'sm' | 'md' | 'lg'>('md');
  readonly letters = LETTERS;

  transform(letter: WordmarkLetter): string {
    return `translateY(${letter.dy}em) rotate(${letter.rot}deg) scale(${letter.scale})`;
  }
}
