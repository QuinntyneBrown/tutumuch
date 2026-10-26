import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';

/** The other party of an order under its summary: avatar, "Buyer" or "Seller" and the profile link (mocks pages/sale-detail, pages/order-detail). */
@Component({
  selector: 'tl-order-party',
  imports: [RouterLink],
  templateUrl: './order-party.html',
  styleUrl: './order-party.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrderPartyComponent {
  readonly role = input.required<'Buyer' | 'Seller'>();
  readonly displayName = input.required<string>();

  readonly initials = computed(() =>
    this.displayName()
      .replace(/[^a-z0-9]/gi, '')
      .slice(0, 2)
      .toUpperCase(),
  );
}
