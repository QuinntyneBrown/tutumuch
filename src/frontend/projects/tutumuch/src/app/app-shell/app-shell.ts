import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRouteSnapshot, NavigationEnd, ResolveEnd, Router, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import { AuthState } from '@tutumuch/api';
import { FooterComponent, LogoComponent, TopBarComponent } from '@tutumuch/components';
import { PayoutBannerComponent } from '@tutumuch/orders';
import { CookieSettingsDialogService } from '@tutumuch/privacy';

/** Shell named by the deepest route's data.shell: 'app' (default), 'auth' or 'admin'. */
type Shell = 'app' | 'auth' | 'admin';

function shellOf(route: ActivatedRouteSnapshot): Shell {
  let deepest = route;
  while (deepest.firstChild) deepest = deepest.firstChild;
  const shell = deepest.data['shell'];
  return shell === 'auth' || shell === 'admin' ? shell : 'app';
}

/**
 * App shell: the payout banner, top bar with the member from AuthState, main#main with the router
 * outlet and the footer, whose "Cookie settings" opens the dialog; a top-bar search navigates to the catalog. Routes with data.shell 'auth' render in the auth shell: large logo above a centred card.
 * Routes with data.shell 'admin' render in the admin shell: the admin top bar and no footer.
 */
@Component({
  selector: 'tm-app-shell',
  imports: [RouterOutlet, TopBarComponent, FooterComponent, LogoComponent, PayoutBannerComponent],
  templateUrl: './app-shell.html',
  styleUrl: './app-shell.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppShellComponent {
  private readonly router = inject(Router);
  readonly auth = inject(AuthState);
  readonly cookieSettings = inject(CookieSettingsDialogService);

  readonly shell = toSignal(
    this.router.events.pipe(
      filter((event) => event instanceof ResolveEnd),
      map((event) => shellOf(event.state.root)),
    ),
    { initialValue: 'app' as Shell },
  );

  /** The keyword of the catalog URL, so the top-bar box keeps it. */
  readonly query = toSignal(
    this.router.events.pipe(
      filter((event) => event instanceof NavigationEnd),
      map(() => this.router.parseUrl(this.router.url).queryParamMap.get('q') ?? ''),
    ),
    { initialValue: '' },
  );

  /** A keyword search goes to the catalog; on the catalog it keeps the filters (L2-023). */
  search(term: string): void {
    const onCatalog = this.router.parseUrl(this.router.url).root.children['primary'] === undefined;
    void this.router.navigate(['/'], {
      queryParams: { q: term || null },
      queryParamsHandling: onCatalog ? 'merge' : '',
    });
  }

  constructor() {
    void this.auth.load();
  }
}
