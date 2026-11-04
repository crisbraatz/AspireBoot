import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AppConfiguration } from '../../shared/configuration/app-configuration';
import { authenticationInterceptor, isApiRequest } from './authentication.interceptor';
import { Antiforgery } from './get-antiforgery/antiforgery';
import { CurrentSession } from '../sessions/get-current-session/current-session';

describe('authentication interceptor', () => {
  let http: HttpTestingController;
  let client: HttpClient;
  let session: { getToken: ReturnType<typeof vi.fn>; expire: ReturnType<typeof vi.fn> };
  beforeEach(() => {
    session = { getToken: vi.fn().mockResolvedValue('csrf-token'), expire: vi.fn() };
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authenticationInterceptor])),
        provideHttpClientTesting(),
        {
          provide: AppConfiguration,
          useValue: { value: { apiUrl: 'https://api.example.com/api' } },
        },
        { provide: CurrentSession, useValue: session },
        { provide: Antiforgery, useValue: session },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    client = TestBed.inject(HttpClient);
  });
  afterEach(() => http.verify());

  it('should send cookies without exposing credentials to an API read', async () => {
    client.get('https://api.example.com/api/users').subscribe();
    await Promise.resolve();
    const request = http.expectOne('https://api.example.com/api/users');
    expect(request.request.headers.has('Authorization')).toBe(false);
    expect(request.request.withCredentials).toBe(true);
    expect(session.getToken).not.toHaveBeenCalled();
    request.flush({ items: [] });
  });
  it.each([
    'https://rustfs.example.com/avatar?X-Amz-Signature=signature',
    'https://external.example.com/api',
    'https://api.example.com.attacker.test/api/users',
    'https://api.example.com/api-evil',
  ])('should not attach tokens outside the exact API boundary', (url) => {
    client.get(url).subscribe();
    const request = http.expectOne(url);
    expect(request.request.headers.has('Authorization')).toBe(false);
    expect(session.getToken).not.toHaveBeenCalled();
    request.flush({});
  });
  it('should expire the session without replaying a failed write', async () => {
    let failed = false;
    client.patch('https://api.example.com/api/users/me', {}).subscribe({
      error: () => {
        failed = true;
      },
    });
    await Promise.resolve();
    const request = http.expectOne('https://api.example.com/api/users/me');
    request.flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(failed).toBe(true);
    expect(session.expire).toHaveBeenCalledOnce();
    http.expectNone('https://api.example.com/api/users/me');
  });
  it('should block writes if CSRF token acquisition fails', async () => {
    session.getToken.mockRejectedValue(new Error('CSRF token unavailable.'));
    let failed = false;
    client.patch('https://api.example.com/api/users/me', {}).subscribe({
      error: () => {
        failed = true;
      },
    });
    await Promise.resolve();
    expect(failed).toBe(true);
    http.expectNone('https://api.example.com/api/users/me');
  });

  it.each(['auth/email', 'auth/password', 'sessions'])(
    'should not expire a valid session for rejected credentials on %s',
    async (path) => {
      let rejected = false;
      client.post('https://api.example.com/api/' + path, {}).subscribe({
        error: () => {
          rejected = true;
        },
      });
      await Promise.resolve();
      const request = http.expectOne('https://api.example.com/api/' + path);
      request.flush(
        { code: 'identity.auth.invalid-credentials' },
        { status: 401, statusText: 'Unauthorized' },
      );
      expect(rejected).toBe(true);
      expect(session.expire).not.toHaveBeenCalled();
      http.expectNone('https://api.example.com/api/' + path);
    },
  );
  it('should match API path boundaries rather than textual URL prefixes', () => {
    expect(
      isApiRequest('/api/users', 'https://api.example.com/api', 'https://api.example.com'),
    ).toBe(true);
    expect(isApiRequest('/users', 'https://api.example.com/api', 'https://api.example.com')).toBe(
      false,
    );
  });
});
