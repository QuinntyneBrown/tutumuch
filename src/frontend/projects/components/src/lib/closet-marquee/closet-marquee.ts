import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';

import { ButtonDirective } from '../button/button';
import { IconComponent } from '../icon/icon';
import { ListingCardView } from '../listing-card/listing-card';

/**
 * Real listings gliding past under the marketing hero (design system: Closet marquee, ADR
 * general/0010): a heading with a live dot, a "See everything" link and two rows that scroll in
 * opposite directions. Each tile shows the photo, the title and the price, and links to the
 * listing. Each row repeats its tiles once for a seamless loop; the repeat is hidden from
 * assistive technology and cannot be focused. Hover or focus pauses a row; reduced motion turns
 * each row into a plain horizontal scroller.
 */
@Component({
  selector: 'tl-closet-marquee',
  imports: [NgTemplateOutlet, RouterLink, ButtonDirective, IconComponent],
  templateUrl: './closet-marquee.html',
  styleUrl: './closet-marquee.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ClosetMarqueeComponent {
  readonly heading = input.required<string>();
  readonly headingId = input.required<string>();
  readonly items = input.required<ListingCardView[]>();
  readonly seeAllHref = input('/');

  /** The items split into two rows; the second scrolls the other way. */
  readonly rows = computed(() => {
    const items = this.items();
    const half = Math.ceil(items.length / 2);
    return [items.slice(0, half), items.slice(half)].filter((row) => row.length);
  });
}
