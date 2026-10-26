import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

/** TuTuMuch logo as a link home. Sizes sm, md, lg (design system: Top bar, Footer). */
@Component({
  selector: 'tl-logo',
  imports: [RouterLink],
  templateUrl: './logo.html',
  styleUrl: './logo.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LogoComponent {
  readonly size = input<'sm' | 'md' | 'lg'>('md');
}
