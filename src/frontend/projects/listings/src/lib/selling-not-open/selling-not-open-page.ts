import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonDirective, CenterStateComponent } from '@tutumuch/components';

/**
 * Shown at the seller routes in launch Phases 1 and 2 to anyone but an administrator (mocks
 * pages/sell/selling-not-open, L2-094): selling opens soon, shopping is open.
 */
@Component({
  selector: 'tl-selling-not-open-page',
  imports: [RouterLink, ButtonDirective, CenterStateComponent],
  template: `
    <tl-center-state icon="tag" heading="Selling opens soon">
      <p class="text-muted">
        For now, every item is hand-picked by TuTuMuch. You can still shop everything in the closet.
      </p>
      <a actions tlButton="primary" routerLink="/">Shop the closet</a>
    </tl-center-state>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SellingNotOpenPage {}
