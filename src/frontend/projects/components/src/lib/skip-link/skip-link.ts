import { ChangeDetectionStrategy, Component } from '@angular/core';

/** First focusable element of every page; jumps to main#main (L2-065, design system: Skip link). */
@Component({
  selector: 'tm-skip-link',
  templateUrl: './skip-link.html',
  styleUrl: './skip-link.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SkipLinkComponent {}
