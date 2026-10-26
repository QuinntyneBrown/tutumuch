import { ChangeDetectionStrategy, Component, OnDestroy, computed, inject } from '@angular/core';
import { Meta } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { AuthState } from '@tutumuch/api';
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

const DESCRIPTION =
  'New and gently loved clothes and gear for girls: dresses, dance and sports gear, cameras and more. Shop her next favourite thing, or pass on what she has outgrown.';

/**
 * Public marketing page at /welcome (L2-078 to L2-083). Static copy, no TuTuMuch API call of its own;
 * Start selling goes to /join for a visitor and /sell for a member. Copy follows ADR general/0005:
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
export class WelcomePage implements OnDestroy {
  private readonly auth = inject(AuthState);
  private readonly dialogs = inject(DialogService);
  private readonly meta = inject(Meta);

  readonly sellHref = computed(() => (this.auth.member() ? '/sell' : '/join'));

  readonly shopperPoints: FeaturePoint[] = [
    {
      title: 'Every listing is for girls',
      text: 'Clothes, dance and sports gear, cameras and more. Nothing to filter out.',
    },
    { title: 'Outgrown, not worn out', text: 'Most things still have plenty of twirl left in them.' },
    { title: 'Honest condition', text: 'Five grades, and flaws named up front.' },
  ];
  readonly sellerPoints: FeaturePoint[] = [
    { title: 'List in minutes', text: 'Photos, a price and the condition. That’s it.' },
    { title: 'Families are already looking', text: 'Sell to parents who need exactly what she has outgrown.' },
    { title: '10% fee, only when it sells', text: 'Taken when the order completes. Listing is free.' },
  ];
  readonly steps: Step[] = [
    { title: 'List it', text: 'Add photos, a price and the condition.' },
    { title: 'Sell it', text: 'A buyer pays. You ship within 5 business days.' },
    { title: 'Get paid', text: 'The order completes and your payout is on its way.' },
  ];

  constructor() {
    this.meta.updateTag({ name: 'description', content: DESCRIPTION });
  }

  openVideo(): void {
    this.dialogs.open(PitchVideoDialog);
  }

  ngOnDestroy(): void {
    this.meta.removeTag('name="description"');
  }
}
