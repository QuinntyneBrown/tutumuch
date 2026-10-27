import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

/** Third-party sign-in providers TuTuMuch can continue with (design system: Provider button). */
export type SignInProvider = 'google';

/**
 * "Continue with Google" (design system: Provider button): a link styled as the secondary lg block
 * button with the provider's mark. Pressing it leaves the page for the provider through the API's
 * start endpoint; no provider script runs in the page (L2-088). The mark is the one Google
 * publishes for sign-in buttons and is used as published, in both themes (ADR accounts/0001).
 */
@Component({
  selector: 'tl-provider-button',
  templateUrl: './provider-button.html',
  styleUrl: './provider-button.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProviderButtonComponent {
  readonly provider = input.required<SignInProvider>();
  /** The API start endpoint the browser navigates to, with its returnUrl. */
  readonly href = input.required<string>();
  /** Set while the browser leaves for the provider; the mark hides under the spinner. */
  readonly busy = input(false);
  /** Emitted on press, before the browser leaves. */
  readonly pressed = output();
}
