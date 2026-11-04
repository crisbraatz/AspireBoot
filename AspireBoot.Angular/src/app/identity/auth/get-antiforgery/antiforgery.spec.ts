import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AppConfiguration } from '../../../shared/configuration/app-configuration';
import { Antiforgery } from './antiforgery';

describe('Antiforgery', () => {
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

  it('should request the token from auth with the current session cookies', async () => {
    const token = TestBed.inject(Antiforgery).getToken();
    const request = TestBed.inject(HttpTestingController).expectOne(
      'https://api.example.com/api/auth/csrf',
    );
    expect(request.request.method).toBe('GET');
    expect(request.request.withCredentials).toBe(true);
    request.flush({ token: 'current-token' });
    await expect(token).resolves.toBe('current-token');
  });

  it('should acquire a fresh token after the principal changes', async () => {
    const csrf = TestBed.inject(Antiforgery);
    const http = TestBed.inject(HttpTestingController);
    const anonymous = csrf.getToken();
    http.expectOne('https://api.example.com/api/auth/csrf').flush({ token: 'anonymous' });
    await expect(anonymous).resolves.toBe('anonymous');
    const authenticated = csrf.getToken();
    http.expectOne('https://api.example.com/api/auth/csrf').flush({ token: 'authenticated' });
    await expect(authenticated).resolves.toBe('authenticated');
  });

  it('should propagate token acquisition failures', async () => {
    const token = TestBed.inject(Antiforgery).getToken();
    const failure = expect(token).rejects.toMatchObject({ status: 503 });
    TestBed.inject(HttpTestingController)
      .expectOne('https://api.example.com/api/auth/csrf')
      .flush(null, { status: 503, statusText: 'Unavailable' });
    await failure;
  });
});
