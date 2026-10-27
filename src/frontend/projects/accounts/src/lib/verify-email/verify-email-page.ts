import { ChangeDetectionStrategy, Component, OnInit, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AccountsApi, AuthState, LaunchState, shortRef } from '@tutumuch/api';
import { AlertComponent, ButtonDirective, IconComponent, MarkComponent } from '@tutumuch/components';

type State = 'loading' | 'verified' | 'expired' | 'used' | 'error';

/** Opens a verification link at /verify?token=… (L2-002, mocks pages/verify-email). */
@Component({
  selector: 'tl-verify-email-page',
  imports: [RouterLink, AlertComponent, ButtonDirective, IconComponent, MarkComponent],
  templateUrl: './verify-email-page.html',
  styleUrl: './verify-email-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VerifyEmailPage implements OnInit {
  private readonly api = inject(AccountsApi);
  private readonly auth = inject(AuthState);
  readonly launch = inject(LaunchState);
  private readonly router = inject(Router);

  /** The token query parameter, bound by the router. */
  readonly token = input<string>('');

  readonly state = signal<State>('loading');
  readonly ref = signal('');
  readonly resend = signal<'idle' | 'busy' | 'sent' | 'limited'>('idle');

  ngOnInit(): void {
    void this.verify();
  }

  async verify(): Promise<void> {
    this.state.set('loading');
    const result = await this.api.verifyEmail(this.token());
    if (result.ok) {
      this.state.set('verified');
      if (this.auth.member()) await this.auth.reload();
      return;
    }
    const { code, correlationId } = result.problem;
    if (code === 'VERIFICATION_LINK_EXPIRED') this.state.set('expired');
    else if (code === 'VERIFICATION_LINK_USED') this.state.set('used');
    else {
      this.ref.set(shortRef(correlationId));
      this.state.set('error');
    }
  }

  /** A signed-in member gets a new link here; a visitor signs in first and resends from the sell page. */
  async sendNewLink(): Promise<void> {
    const member = this.auth.member();
    if (!member) {
      await this.router.navigate(['/sign-in'], { queryParams: { returnUrl: '/sell' } });
      return;
    }
    this.resend.set('busy');
    const result = await this.api.requestVerificationEmail(member.email);
    this.resend.set(result.ok ? 'sent' : 'limited');
  }
}
