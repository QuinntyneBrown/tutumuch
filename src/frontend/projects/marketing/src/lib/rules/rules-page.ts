import { ChangeDetectionStrategy, Component } from '@angular/core';
import { SizeTagComponent } from '@tutumuch/components';

interface Rule {
  title: string;
  detail: string;
}

/**
 * Marketplace rules at /rules (L2-011, L2-016, L2-031, L2-033; mocks pages/rules): the accepted
 * categories with their size, then the short list of rules. Static copy, no TuTuMuch API call.
 */
@Component({
  selector: 'tl-rules-page',
  imports: [SizeTagComponent],
  templateUrl: './rules-page.html',
  styleUrl: './rules-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RulesPage {
  readonly categories = [
    { name: 'Clothing', includes: 'Dresses, Tops, Bottoms, Outerwear, Pajamas, Swimwear' },
    { name: 'Shoes', includes: 'Sneakers, Boots, Sandals, Dress shoes' },
    { name: 'Dance', includes: 'Tutus, Leotards, Dance shoes, Dance bags' },
    { name: 'Sports', includes: 'Soccer, Volleyball, Gymnastics, Swimming, Bikes and scooters' },
    { name: 'Cameras and tech', includes: 'Cameras, Headphones, Tablet accessories' },
    { name: 'Toys and crafts', includes: 'Dolls, Arts and crafts, Games and puzzles, Books' },
  ];

  readonly rules: Rule[] = [
    {
      title: 'For girls only',
      detail: 'Clothing, shoes, dance, sports, cameras and tech, toys and crafts. Every item must be for girls.',
    },
    { title: '18 and over', detail: 'One account to buy and sell. No approval step.' },
    {
      title: 'Honest condition',
      detail:
        'New with tags, New without tags, Used - Excellent, Used - Good, Used - Fair. Used - Fair needs its flaws described.',
    },
    { title: 'Ship in 5 business days', detail: 'Add tracking or the order is cancelled and refunded.' },
    { title: '10% fee', detail: 'Taken from item plus shipping when the order completes.' },
    { title: 'US only', detail: 'Prices in US dollars, shipping within the United States.' },
    { title: 'No counterfeits', detail: 'Fakes are removed from the closet.' },
  ];
}
