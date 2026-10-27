import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

/**
 * Terms of service at /terms (L2-001, L2-036, L2-031; mocks pages/terms): a short numbered list
 * with links to the marketplace rules and privacy. Static copy, no TuTuMuch API call.
 */
@Component({
  selector: 'tl-terms-page',
  imports: [RouterLink],
  templateUrl: './terms-page.html',
  styleUrl: './terms-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TermsPage {
  readonly terms = [
    'You must be 18 or older to hold an account.',
    'List only girls’ items you own.',
    'Describe condition and flaws honestly; photos must be of the actual item.',
    'Payment goes through our payment provider. Sellers are paid when the order completes.',
    'Buyers can report a problem until 3 days after delivery.',
    'We may remove listings that break the marketplace rules.',
    'TuTuMuch operates in the United States only.',
  ];
}
