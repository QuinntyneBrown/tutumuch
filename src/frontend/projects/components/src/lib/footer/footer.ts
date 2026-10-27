import { ChangeDetectionStrategy, Component, output } from '@angular/core';
import { RouterLink } from '@angular/router';

import { MarkComponent } from '../mark/mark';

/**
 * App-shell footer (design system: Footer, page guidelines 7): the mark (not a second logo link),
 * short links, a "Cookie settings" button that emits cookieSettings and "© TuTuMuch".
 */
@Component({
  selector: 'tl-footer',
  imports: [RouterLink, MarkComponent],
  templateUrl: './footer.html',
  styleUrl: './footer.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FooterComponent {
  readonly cookieSettings = output();
}
