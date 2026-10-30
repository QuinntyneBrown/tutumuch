import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthState, CatalogApi, LaunchState, ListingCard } from '@tutumuch/api';
import {
  ButtonDirective,
  ClosetMarqueeComponent,
  ClosetWorthComponent,
  DialogService,
  FeatureBandComponent,
  FeaturePoint,
  ListingCardView,
  MarketingHeroComponent,
  Step,
  StepsComponent,
  TillyComponent,
  TulleFieldComponent,
  VideoPosterComponent,
  WorthItem,
} from '@tutumuch/components';

import { PitchVideoDialog } from './pitch-video-dialog';

/** The marquee is left out below this many listings (L2-112). */
const MARQUEE_MIN = 4;
const MARQUEE_MAX = 16;

/** "$85" for whole dollars, "$18.50" otherwise, as the catalog cards show prices. */
function formatPrice(price: string): string {
  const value = Number(price);
  const cents = Math.round(value * 100) % 100 !== 0;
  return `$${value.toLocaleString('en-US', { minimumFractionDigits: cents ? 2 : 0, maximumFractionDigits: cents ? 2 : 0 })}`;
}

/** A catalog card as the closet marquee shows it, linking to the listing detail. */
function toCardView(card: ListingCard): ListingCardView {
  return {
    href: `/items/${card.reference}`,
    title: card.title,
    brand: card.brand,
    sizeLabel: card.sizeLabel,
    condition: card.condition,
    price: formatPrice(card.price),
    reserved: card.status === 'Reserved',
    photo: card.coverPhoto,
  };
}

/**
 * Public marketing page at /welcome (L2-078 to L2-083). The meta description and link-preview tags
 * are static in index.html (index.marketing.html for the marketing build) because crawlers do not run
 * scripts. Copy follows ADR general/0005: every listing is for girls; never promise boys' or adult
 * sizes. Tilly twirls in the hero, the one Tilly and the one primary button of the page (L2-096).
 *
 * In launch Phase 1, for anyone but an administrator, it is the Phase 1 page (L2-112, ADR
 * general/0010): the tulle field, the closet marquee (the page's one TuTuMuch API call), the
 * closet-worth calculator, the shopper's journey and "Join early" for a visitor. Otherwise it is
 * static: Start selling goes to /join for a visitor and /sell for a member; in Phase 2 only an
 * administrator sees it and everyone else reads "Selling opens soon." (L2-094).
 */
@Component({
  selector: 'tl-welcome-page',
  imports: [
    RouterLink,
    ButtonDirective,
    ClosetMarqueeComponent,
    ClosetWorthComponent,
    MarketingHeroComponent,
    FeatureBandComponent,
    StepsComponent,
    TillyComponent,
    TulleFieldComponent,
    VideoPosterComponent,
  ],
  templateUrl: './welcome-page.html',
  styleUrl: './welcome-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WelcomePage {
  private readonly auth = inject(AuthState);
  private readonly catalog = inject(CatalogApi);
  private readonly dialogs = inject(DialogService);
  readonly launch = inject(LaunchState);

  readonly sellHref = computed(() => (this.auth.member() ? '/sell' : '/join'));
  readonly signedIn = computed(() => this.auth.member() !== null);
  /** Phase 1 for anyone but an administrator (L2-112). */
  readonly phaseOne = computed(() => this.launch.phase() === 1 && !this.launch.canSell());
  /** The newest listings for the marquee, or null while loading, on failure or when too few. */
  readonly marquee = signal<ListingCardView[] | null>(null);

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
  /** The seller band while selling is not open to the visitor (launch Phase 2). */
  readonly sellerPointsSoon: FeaturePoint[] = [
    { title: 'Pass it on soon', text: 'Selling opens to every member in the next phase.' },
    { title: 'Join today', text: 'Your account is ready to sell the day selling opens.' },
  ];
  readonly steps: Step[] = [
    { title: 'List it', text: 'Add photos, a price and the condition.' },
    { title: 'Sell it', text: 'A buyer pays. You ship within 5 business days.' },
    { title: 'Get paid', text: 'The order completes and your payout is on its way.' },
  ];

  // Phase 1 (L2-112): only TuTuMuch sells, so the points and the journey are the shopper's.
  readonly shopperPointsPhaseOne: FeaturePoint[] = [
    { title: 'Hand-picked by TuTuMuch', text: 'Every piece is chosen, checked and photographed by us.' },
    { title: 'Every listing is for girls', text: 'Clothes, dance and sports gear, cameras and more.' },
    { title: 'Honest condition', text: 'Five grades, and flaws named up front.' },
  ];
  readonly stepsPhaseOne: Step[] = [
    { title: 'Find it', text: 'Pick something she will love from the hand-picked closet.', icon: 'search' },
    { title: 'Pay at checkout', text: 'Card details go to Stripe, never to TuTuMuch.', icon: 'card' },
    { title: 'It ships to you', text: 'TuTuMuch ships within 5 business days.', icon: 'truck' },
  ];
  /** Typical prices for the calculator: static copy, not computed from listings (ADR general/0010). */
  readonly worthItems: WorthItem[] = [
    { label: 'Tutu', price: 32, image: '/items/tutu.svg' },
    { label: 'Party dress', price: 42, image: '/items/party-dress.svg' },
    { label: 'Sneakers', price: 22, image: '/items/sneakers.svg' },
    { label: 'Leotard', price: 20, image: '/items/leotard.svg' },
    { label: 'Winter coat', price: 34, image: '/items/puffer.svg' },
    { label: 'Scooter', price: 48, image: '/items/scooter.svg' },
    { label: 'Camera', price: 45, image: '/items/camera.svg' },
    { label: 'Doll', price: 16, image: '/items/doll.svg' },
    { label: 'Rain boots', price: 18, image: '/items/rain-boots.svg' },
    { label: 'Sweater', price: 32, image: '/items/sweater.svg' },
  ];
  readonly worthInitial = ['Tutu', 'Party dress', 'Sneakers'];

  constructor() {
    let requested = false;
    effect(() => {
      if (!this.phaseOne() || requested) return;
      requested = true;
      untracked(() => void this.loadMarquee());
    });
  }

  openVideo(): void {
    this.dialogs.open(PitchVideoDialog);
  }

  /** The newest listings, page 1 of the catalog; a failure or a short catalog leaves the marquee out. */
  private async loadMarquee(): Promise<void> {
    const result = await this.catalog.listings(1);
    if (!result.ok || result.value.items.length < MARQUEE_MIN) return;
    this.marquee.set(result.value.items.slice(0, MARQUEE_MAX).map(toCardView));
  }
}
