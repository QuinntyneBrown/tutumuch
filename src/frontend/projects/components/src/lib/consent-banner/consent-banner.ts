import { ChangeDetectionStrategy, Component, output } from '@angular/core';

import { ButtonDirective } from '../button/button';

let nextId = 0;

/**
 * Non-blocking cookie consent panel fixed to the bottom of the page: heading "Cookies", one line
 * of text, "Accept analytics", "Necessary only" and a "Cookie settings" link-styled button
 * (design system: Consent banner). It does not trap focus; the owner hides it once a choice is saved.
 */
@Component({
  selector: 'tm-consent-banner',
  imports: [ButtonDirective],
  templateUrl: './consent-banner.html',
  styleUrl: './consent-banner.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConsentBannerComponent {
  readonly accepted = output();
  readonly declined = output();
  readonly settings = output();

  readonly id = `tm-consent-${nextId++}`;
}
