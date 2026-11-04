import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AppConfiguration } from '../../shared/configuration/app-configuration';
import { RegisterRequest } from './register/register-request';
import { ChangeEmailRequest } from './change-email/change-email-request';
import { ChangePasswordRequest } from './change-password/change-password-request';
import { ConfirmEmailRequest } from './confirm-email/confirm-email-request';
import { ConfirmEmailChangeRequest } from './confirm-email-change/confirm-email-change-request';
import { RequestRecoveryRequest } from './request-recovery/request-recovery-request';
import { ResendEmailConfirmationRequest } from './resend-email-confirmation/resend-email-confirmation-request';
import { ResetPasswordRequest } from './reset-password/reset-password-request';
import { RegisterResponse } from './register/register-response';

@Injectable({ providedIn: 'root' })
export class AuthApi {
  private readonly http = inject(HttpClient);
  private readonly configuration = inject(AppConfiguration);

  register(request: RegisterRequest): Observable<RegisterResponse> {
    return this.http.post<RegisterResponse>(
      this.configuration.value.apiUrl + '/auth/register',
      request,
    );
  }

  changeEmail(request: ChangeEmailRequest): Observable<void> {
    return this.http.patch<void>(this.configuration.value.apiUrl + '/auth/email', request);
  }

  changePassword(request: ChangePasswordRequest): Observable<void> {
    return this.http.patch<void>(this.configuration.value.apiUrl + '/auth/password', request);
  }

  confirmEmail(request: ConfirmEmailRequest): Observable<void> {
    return this.http.post<void>(this.configuration.value.apiUrl + '/auth/confirm-email', request);
  }

  confirmEmailChange(request: ConfirmEmailChangeRequest): Observable<void> {
    return this.http.post<void>(
      this.configuration.value.apiUrl + '/auth/confirm-email-change',
      request,
    );
  }

  requestRecovery(request: RequestRecoveryRequest): Observable<void> {
    return this.http.post<void>(this.configuration.value.apiUrl + '/auth/forgot-password', request);
  }

  resendEmailConfirmation(request: ResendEmailConfirmationRequest): Observable<void> {
    return this.http.post<void>(
      this.configuration.value.apiUrl + '/auth/resend-email-confirmation',
      request,
    );
  }

  resetPassword(request: ResetPasswordRequest): Observable<void> {
    return this.http.post<void>(this.configuration.value.apiUrl + '/auth/reset-password', request);
  }
}
