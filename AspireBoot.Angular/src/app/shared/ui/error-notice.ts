import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { apiProblem, fieldCodes } from '../http/api-problem';
import { Localization } from '../localization/localization';
import { MessageKey } from '../localization/messages';
import { Notice } from './notice';

@Component({
  selector: 'app-error-notice',
  imports: [Notice],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (error()) {
      <app-notice [notification]="error()" [dismissible]="dismissible()" announce="alert">
        <h2 class="font-semibold">{{ i18n.t('error.title') }}</h2>
        <p class="mt-2 text-sm text-muted">{{ i18n.t(message()) }}</p>
        @if (traceId()) {
          <p class="mt-3 break-all font-mono text-xs text-muted">
            {{ i18n.t('error.trace') }}: {{ traceId() }}
          </p>
        }
        @if (retryable()) {
          <button type="button" class="btn btn-secondary mt-4" (click)="retry.emit()">
            {{ i18n.t('common.retry') }}
          </button>
        }
      </app-notice>
    }
  `,
})
export class ErrorNotice {
  readonly error = input<unknown>(null);
  readonly retryable = input(false);
  readonly validationMessage = input<MessageKey>('error.validation');
  readonly credentialMessage = input<MessageKey>('identity.invalidCredentials');
  readonly retry = output<void>();
  protected readonly i18n = inject(Localization);
  protected readonly dismissible = computed(() => {
    const error = this.error();
    return error instanceof HttpErrorResponse && error.status > 0 && error.status < 500;
  });
  protected readonly traceId = computed(() => {
    const value = apiProblem(this.error()).traceId;
    return typeof value === 'string' && value.length <= 200 ? value : null;
  });
  protected readonly message = computed<MessageKey>(() => {
    const error = this.error();
    if (!(error instanceof HttpErrorResponse)) return 'error.unavailable';
    if (apiProblem(error).code === 'identity.auth.invalid-credentials')
      return this.credentialMessage();
    if (
      error.status === 400 &&
      fieldCodes(error, 'currentPassword').includes('identity.auth.PasswordMismatch')
    )
      return this.credentialMessage();
    if (fieldCodes(error, 'newPassword').includes('identity.auth.password-unchanged'))
      return 'identity.passwordUnchanged';
    switch (error.status) {
      case 0:
        return 'error.network';
      case 400:
      case 413:
      case 415:
        return this.validationMessage();
      case 401:
        return 'auth.expired';
      case 403:
        return 'error.forbidden';
      case 404:
        return 'error.notFound';
      case 429:
        return 'error.rateLimit';
      default:
        return 'error.unavailable';
    }
  });
}
