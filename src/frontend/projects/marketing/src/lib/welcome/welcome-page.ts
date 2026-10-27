import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthState, LaunchState } from '@tutumuch/api';
import {
  ButtonDirective,
  DialogService,
  FeatureBandComponent,
  FeaturePoint,
  MarketingHeroComponent,
  Step,
  StepsComponent,
  VideoPosterComponent,
} from '@tutumuch/components';

import { PitchVideoDialog } from './pitch-video-dialog';

/**
 * Public marketing page at /welcome (L2-078 to L2-083). The meta description and link-preview tags
 * are static in index.html (index.marketing.html for the marketing build) because crawlers do not run
 * scripts. Static copy, no TuTuMuch API call of its own;
 * Start selling goes to /join for a visitor and /sell for a member; in launch Phases 1 and 2 only an
 * administrator sees it and everyone else reads "Selling opens soon." (L2-094). Copy follows ADR general/0005:
 * every listing is for girls; never promise boys' or adult sizes.
 */
@Component({
  selector: 'tl-welcome-page',
  imports: [
    RouterLink,
    ButtonDirective,
    MarketingHeroComponent,
    FeatureBandComponent,
    StepsComponent,
    VideoPosterComponent,
  ],
  templateUrl: './welcome-page.html',
  styleUrl: './welcome-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WelcomePage {
  private readonly auth = inject(AuthState);
  private readonly dialogs = inject(DialogService);
  readonly launch = inject(LaunchState);

  readonly sellHref = computed(() => (this.auth.member() ? '/sell' : '/join'));

  readonly shopperPoints: FeaturePoint[] = [
    {
      title: 'Every listing is for girls',
      text: 'Clothes, shoes, dance and sports gear, cameras, toys and more. Nothing to filter out.',
    },
    { title: 'Outgrown, not worn out', text: 'Most things still have plenty of twirl left in them.' },
    { title: 'Honest condition', text: 'Five grades, and flaws named up front.' },
  ];
  readonly sellerPoints: FeaturePoint[] = [
    { title: 'List in minutes', text: 'Photos, a price and the condition. That’s it.' },
    { title: 'Families are already looking', text: 'Sell to parents who need exactly what she has outgrown.' },
  ];
  /** The seller band while selling is not open to the visitor (launch Phases 1 and 2). */
  readonly sellerPointsSoon: FeaturePoint[] = [
    { title: 'Pass it on soon', text: 'Selling opens to every member in the next phase.' },
    { title: 'Join today', text: 'Your account is ready to sell the day selling opens.' },
  ];
  readonly steps: Step[] = [
    { title: 'List it', text: 'Add photos, a price and the condition.' },
    { title: 'Sell it', text: 'A buyer pays. You ship within 5 business days.' },
    { title: 'Get paid', text: 'The order completes and your payout is on its way.' },
  ];

  openVideo(): void {
    this.dialogs.open(PitchVideoDialog);
  }
}
