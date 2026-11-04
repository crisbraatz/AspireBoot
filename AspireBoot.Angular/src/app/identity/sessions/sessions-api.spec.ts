import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AppConfiguration } from '../../shared/configuration/app-configuration';
import { SessionsApi } from './sessions-api';

describe('SessionsApi', () => {
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

  it('should create a session using the backend request', () => {
    const body = { email: 'user@example.com', password: 'Test-Password123!', rememberMe: true };
    TestBed.inject(SessionsApi).createSession(body).subscribe();
    const request = TestBed.inject(HttpTestingController).expectOne(
      'https://api.example.com/api/sessions',
    );
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(body);
    request.flush(null);
  });

  it('should preserve the current session response including local and identity user IDs', () => {
    const body = {
      identityUserId: 'identity-id',
      userId: 'local-id',
      email: 'user@example.com',
      isAdministrator: true,
    };
    let response: unknown;
    TestBed.inject(SessionsApi)
      .getCurrentSession()
      .subscribe((value) => (response = value));
    const request = TestBed.inject(HttpTestingController).expectOne(
      'https://api.example.com/api/sessions/current',
    );
    expect(request.request.method).toBe('GET');
    request.flush(body);
    expect(response).toEqual(body);
  });

  it('should distinguish current, other and individual session deletion', () => {
    const api = TestBed.inject(SessionsApi);
    const http = TestBed.inject(HttpTestingController);
    api.deleteCurrentSession().subscribe();
    api.deleteOtherSessions().subscribe();
    api.deleteSession('session-id').subscribe();
    for (const path of ['current', 'others', 'session-id']) {
      const request = http.expectOne('https://api.example.com/api/sessions/' + path);
      expect(request.request.method).toBe('DELETE');
      expect(request.request.body).toBeNull();
      request.flush(null);
    }
  });
});
