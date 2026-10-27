import { ChangeDetectionStrategy, Component, computed, inject, input, output, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

import { ButtonDirective } from '../button/button';
import { IconComponent } from '../icon/icon';
import { LogoComponent } from '../logo/logo';
import { SheetService } from '../sheet/sheet-service';
import { NavMenuComponent } from './nav-menu';

/** The member the top bar shows; null for a visitor. */
export interface TopBarMember {
  displayName: string;
}

/**
 * App-shell top bar in its guest and member states (design system: Top bar). The application
 * passes the member from AuthState and the current keyword, and navigates on (searched). Under
 * 1280px the inline category links give way to the menu button, which opens the navigation menu
 * sheet listing every category. Under 992px the
 * search button reveals the search form. In launch Phase 1 the application sets
 * categoryLinksVisible false: no category links and no menu button (the sheet holds only
 * categories). sellVisible false hides Sell from anyone the launch phase does not let sell
 * (ADR platform/0014). The admin variant (admin shell) shows only the logo, the
 * "Admin" label and the signed-in member's initials.
 */
@Component({
  selector: 'tl-top-bar',
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
  /** The six category links and the menu button (L2-093). */
  readonly categoryLinksVisible = input(true);
  /** The Sell link or button (L2-094). */
  readonly sellVisible = input(true);
  /** The keyword of the result set shown, so the box keeps it. */
  readonly query = input('');
  /** A trimmed keyword of at most 100 characters; empty clears the search. */
  readonly searched = output<string>();

  private readonly sheets = inject(SheetService);

  readonly searchOpen = signal(false);
  readonly menuOpen = signal(false);

  readonly initials = computed(() => this.member()?.displayName.slice(0, 2).toUpperCase() ?? '');

  readonly categories = [
    { label: 'Clothing', path: '/clothing' },
    { label: 'Shoes', path: '/shoes' },
    { label: 'Dance', path: '/dance' },
    { label: 'Sports', path: '/sports' },
    { label: 'Cameras and tech', path: '/cameras-and-tech' },
    { label: 'Toys and crafts', path: '/toys-and-crafts' },
  ] as const;

  openMenu(): void {
    this.menuOpen.set(true);
    this.sheets.open(NavMenuComponent, this.categories).closed.then(() => this.menuOpen.set(false));
  }

  toggleSearch(): void {
    this.searchOpen.update((open) => !open);
    if (this.searchOpen()) setTimeout(() => document.getElementById('q')?.focus());
  }

  submit(event: Event, term: string): void {
    event.preventDefault();
    this.searched.emit(term.trim().slice(0, TopBarComponent.maxSearchLength));
  }
}
