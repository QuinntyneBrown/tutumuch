import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  Injector,
  afterNextRender,
  inject,
  signal,
} from '@angular/core';
import { AccountsApi } from '@tutumuch/api';
import { AlertComponent, ButtonDirective, DIALOG_DATA, DialogComponent, DialogRef } from '@tutumuch/components';

/** Data the prompt needs: the member's email. */
export interface VerifyEmailPromptData {
  email: string;
}

/**
 * "Verify your email first" with a resend action (L2-002, mocks dialogs/verify-email-prompt).
 * Opened through DialogService from the unverified state of the sell page.
 */
@Component({
  selector: 'tl-verify-email-prompt',
  imports: [AlertComponent, ButtonDirective, DialogComponent],
  templateUrl: './verify-email-prompt.html',
  styleUrl: './verify-email-prompt.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VerifyEmailPromptComponent {
  private readonly api = inject(AccountsApi);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly injector = inject(Injector);
  readonly ref = inject(DialogRef);
  readonly data = inject<VerifyEmailPromptData>(DIALOG_DATA);

  readonly state = signal<'default' | 'busy' | 'sent' | 'limited'>('default');

  async resend(): Promise<void> {
    if (this.state() === 'busy') return;
    this.state.set('busy');
    const result = await this.api.requestVerificationEmail(this.data.email);
    this.state.set(result.ok ? 'sent' : 'limited');
    afterNextRender(() => this.host.nativeElement.querySelector<HTMLElement>('[data-autofocus]')?.focus(), {
      injector: this.injector,
    });
  }
}
