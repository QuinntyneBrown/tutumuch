import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  afterNextRender,
  inject,
  viewChild,
} from '@angular/core';
import { PAYMENT_FIELDS, TokenizeResult } from '@tutumuch/api';
import { IconComponent } from '@tutumuch/components';

/**
 * The "Card" field of checkout: mounts the payment provider's hosted fields, so card numbers and
 * security codes go to the provider only (L2-030), and returns the payment method token.
 */
@Component({
  selector: 'tm-hosted-card-fields',
  imports: [IconComponent],
  templateUrl: './hosted-card-fields.html',
  styleUrl: './hosted-card-fields.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HostedCardFieldsComponent {
  private readonly fields = inject(PAYMENT_FIELDS)();
  private readonly host = viewChild.required<ElementRef<HTMLElement>>('host');

  constructor() {
    afterNextRender(() => this.fields.mount(this.host().nativeElement));
    inject(DestroyRef).onDestroy(() => this.fields.unmount());
  }

  tokenize(): Promise<TokenizeResult> {
    return this.fields.tokenize();
  }
}
