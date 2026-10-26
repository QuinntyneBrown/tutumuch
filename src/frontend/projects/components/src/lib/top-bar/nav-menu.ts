import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';

import { DIALOG_DATA, DialogRef } from '../dialog/dialog-ref';
import { SheetComponent } from '../sheet/sheet';

/** One category link of the primary navigation. */
export interface NavMenuLink {
  label: string;
  path: string;
}

/**
 * The navigation menu sheet the top bar's menu button opens (dialogs/nav-menu): every category,
 * for widths where the top bar hides its inline links. Choosing a link closes the sheet.
 */
@Component({
  selector: 'tl-nav-menu',
  imports: [RouterLink, SheetComponent],
  template: `
    <tl-sheet heading="Menu" closeLabel="Close menu">
      <nav aria-label="Menu">
        <ul class="menu-sheet__links" role="list">
          @for (item of links; track item.path) {
            <li>
              <a [routerLink]="item.path" (click)="ref.close()">{{ item.label }}</a>
            </li>
          }
        </ul>
      </nav>
    </tl-sheet>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NavMenuComponent {
  readonly links = inject<readonly NavMenuLink[]>(DIALOG_DATA);
  readonly ref = inject(DialogRef);
}
