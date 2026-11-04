import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Observable, firstValueFrom } from 'rxjs';
import { AuthApi } from '../auth-api';
import { RegisterResponse } from '../register/register-response';
import { Localization } from '../../../shared/localization/localization';
import { ErrorNotice } from '../../../shared/ui/error-notice';
import { MessageKey } from '../../../shared/localization/messages';
import { CurrentSession } from '../../sessions/get-current-session/current-session';
import { safeReturnUrl } from '../return-url';
import { accountFieldMessage, emailValidators, passwordValidators } from '../account-validation';
import { nameMessage, validateName } from '../../users/name-validation';
import { Icon } from '../../../shared/ui/icon';
import { Notice } from '../../../shared/ui/notice';

@Component({
  imports: [Icon, Notice, ReactiveFormsModule, RouterLink, ErrorNotice],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './account-access.page.html',
})
export class AccountAccessPage {
  private readonly api = inject(AuthApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  protected readonly session = inject(CurrentSession);
  private readonly destroy = inject(DestroyRef);
  private readonly parameters = this.route.snapshot.queryParamMap;
  protected readonly mode = this.route.snapshot.data['mode'] as string;
  protected readonly i18n = inject(Localization);
  protected readonly busy = signal(false);
  protected readonly done = signal(false);
  protected readonly error = signal<unknown>(null);
  protected readonly title = ('identity.' + this.mode) as MessageKey;
  protected completedMessage: MessageKey =
    this.mode === 'forgot-password'
      ? 'identity.recoveryCompleted'
      : this.mode === 'resend-confirmation'
        ? 'identity.confirmationSent'
        : this.mode === 'register'
          ? 'identity.registrationCompleted'
          : 'identity.requestCompleted';
  protected readonly form = inject(FormBuilder).nonNullable.group({
    email: [this.parameters.get('email') ?? '', emailValidators],
    name: ['', validateName],
    password: ['', passwordValidators],
    rememberMe: false,
  });
  protected readonly accountFieldMessage = accountFieldMessage;
  protected readonly nameMessage = nameMessage;

  constructor() {
    if (this.mode !== 'register') this.form.controls.name.disable();
    if (['confirm-email', 'confirm-email-change'].includes(this.mode))
      this.form.controls.email.disable();
    if (!['login', 'register', 'reset-password'].includes(this.mode))
      this.form.controls.password.disable();
    if (this.parameters.has('token'))
      window.history.replaceState(window.history.state, '', this.router.url.split('?')[0]);
  }

  protected async submit(): Promise<void> {
    this.form.markAllAsTouched();
    if (this.busy() || this.form.invalid) return;
    this.busy.set(true);
    this.error.set(null);
    const { email, name, password, rememberMe } = this.form.getRawValue();
    try {
      if (this.mode === 'login') {
        await this.session.createSession(email, password, rememberMe);
      } else {
        const token = this.parameters.get('token')!;
        const identityUserId = this.parameters.get('identityUserId')!;
        let request: Observable<RegisterResponse | void>;
        switch (this.mode) {
          case 'register':
            request = this.api.register({ email, password, name });
            break;
          case 'reset-password':
            request = this.api.resetPassword({ email, password, token });
            break;
          case 'confirm-email':
            request = this.api.confirmEmail({ identityUserId, token });
            break;
          case 'confirm-email-change':
            request = this.api.confirmEmailChange({ email, identityUserId, token });
            break;
          case 'forgot-password':
            request = this.api.requestRecovery({ email });
            break;
          case 'resend-confirmation':
            request = this.api.resendEmailConfirmation({ email });
            break;
          default:
            throw new Error('Unknown account access mode.');
        }
        const response = await firstValueFrom(request.pipe(takeUntilDestroyed(this.destroy)));
        if (this.destroy.destroyed) return;
        if (this.mode === 'register' && response?.requiresEmailConfirmation === false)
          this.completedMessage = 'identity.registrationCompletedWithoutConfirmation';
        if (this.mode === 'confirm-email-change' && this.session.identityUserId === identityUserId)
          this.session.invalidate();
        this.done.set(true);
        return;
      }
      if (!this.destroy.destroyed)
        await this.router.navigateByUrl(safeReturnUrl(this.parameters.get('returnUrl')), {
          replaceUrl: true,
        });
    } catch (error) {
      if (!this.destroy.destroyed) this.error.set(error);
    } finally {
      this.form.controls.password.reset();
      this.busy.set(false);
    }
  }
}
