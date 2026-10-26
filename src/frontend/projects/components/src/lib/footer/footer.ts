import { ChangeDetectionStrategy, Component, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LogoComponent } from '../logo/logo';

/**
 * App-shell footer: small logo, legal links, a "Cookie settings" button that emits cookieSettings
 * and the copyright year (design system: Footer).
 */
@Component({
  selector: 'tm-footer',
  imports: [RouterLink, LogoComponent],
  templateUrl: './footer.html',
  styleUrl: './footer.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FooterComponent {
  readonly cookieSettings = output();
  readonly year = new Date().getFullYear();
}
