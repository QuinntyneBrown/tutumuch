import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

import { ButtonDirective } from '../button/button';
import { IconComponent } from '../icon/icon';
import { LogoComponent } from '../logo/logo';

/** The member the top bar shows; null for a visitor. */
export interface TopBarMember {
  displayName: string;
}

/**
 * App-shell top bar in its guest and member states (design system: Top bar). The application
 * passes the member from AuthState and the current keyword, and navigates on (searched); the menu
 * button opens the navigation sheet once responsive-accessible-shell builds it. Under 992px the
 * search button reveals the search form. The admin variant (admin shell) shows only the logo, the
 * "Admin" label and the signed-in member's initials.
 */
@Component({
  selector: 'tm-top-bar',
  imports: [RouterLink, RouterLinkActive, ButtonDirective, IconComponent, LogoComponent],
  templateUrl: './top-bar.html',
  styleUrl: './top-bar.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TopBarComponent {
  /** Longest keyword sent; a longer one is cut before searching (L2-023). */
  static readonly maxSearchLength = 100;

  readonly member = input<TopBarMember | null>(null);
  readonly variant = input<'app' | 'admin'>('app');
  /** The keyword of the result set shown, so the box keeps it. */
  readonly query = input('');
  /** A trimmed keyword of at most 100 characters; empty clears the search. */
  readonly searched = output<string>();

  readonly searchOpen = signal(false);

  readonly initials = computed(() => this.member()?.displayName.slice(0, 2).toUpperCase() ?? '');

  readonly categories = [
    { label: 'Shoes', path: '/shoes' },
    { label: 'Tops', path: '/tops' },
    { label: 'Outerwear', path: '/outerwear' },
    { label: 'Bottoms', path: '/bottoms' },
  ] as const;

  toggleSearch(): void {
    this.searchOpen.update((open) => !open);
    if (this.searchOpen()) setTimeout(() => document.getElementById('q')?.focus());
  }

  submit(event: Event, term: string): void {
    event.preventDefault();
    this.searched.emit(term.trim().slice(0, TopBarComponent.maxSearchLength));
  }
}
