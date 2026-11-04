import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Observable } from 'rxjs';
import { AppConfiguration } from '../../shared/configuration/app-configuration';
import { AuthApi } from './auth-api';

describe('AuthApi', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: AppConfiguration,
          useValue: { value: { apiUrl: 'https://api.example.com/api' } },
        },
      ],
    });
  });
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('should preserve every auth route, HTTP method and request field', () => {
    const api = TestBed.inject(AuthApi);
    const http = TestBed.inject(HttpTestingController);
    const email = 'user@example.com';
    const password = 'Test-Password123!';
    const cases: { path: string; method: string; body: object; send: () => Observable<unknown> }[] =
      [
        {
          path: 'register',
          method: 'POST',
          body: { email, password, name: 'User Name' },
          send: () => api.register({ email, password, name: 'User Name' }),
        },
        {
          path: 'email',
          method: 'PATCH',
          body: { newEmail: email, password },
          send: () => api.changeEmail({ newEmail: email, password }),
        },
        {
          path: 'password',
          method: 'PATCH',
          body: { currentPassword: password, newPassword: 'Changed-Password123!' },
          send: () =>
            api.changePassword({ currentPassword: password, newPassword: 'Changed-Password123!' }),
        },
        {
          path: 'confirm-email',
          method: 'POST',
          body: { identityUserId: 'account', token: 'token' },
          send: () => api.confirmEmail({ identityUserId: 'account', token: 'token' }),
        },
        {
          path: 'confirm-email-change',
          method: 'POST',
          body: { email, identityUserId: 'account', token: 'token' },
          send: () => api.confirmEmailChange({ email, identityUserId: 'account', token: 'token' }),
        },
        {
          path: 'forgot-password',
          method: 'POST',
          body: { email },
          send: () => api.requestRecovery({ email }),
        },
        {
          path: 'resend-email-confirmation',
          method: 'POST',
          body: { email },
          send: () => api.resendEmailConfirmation({ email }),
        },
        {
          path: 'reset-password',
          method: 'POST',
          body: { email, password, token: 'token' },
          send: () => api.resetPassword({ email, password, token: 'token' }),
        },
      ];
    for (const scenario of cases) {
      scenario.send().subscribe();
      const request = http.expectOne('https://api.example.com/api/auth/' + scenario.path);
      expect(request.request.method).toBe(scenario.method);
      expect(request.request.body).toEqual(scenario.body);
      request.flush(scenario.path === 'register' ? { requiresEmailConfirmation: true } : null);
    }
  });
});
