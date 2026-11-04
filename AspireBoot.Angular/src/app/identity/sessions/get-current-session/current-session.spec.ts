import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { AppConfiguration } from '../../../shared/configuration/app-configuration';
import { CurrentSession } from './current-session';

describe('identity session', () => {
  let session: CurrentSession;
  let http: HttpTestingController;
  let navigate: ReturnType<typeof vi.fn>;
  beforeEach(() => {
    navigate = vi.fn().mockResolvedValue(true);
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: AppConfiguration,
          useValue: { value: { apiUrl: 'https://app.example.com/api' } },
        },
        { provide: Router, useValue: { navigateByUrl: navigate, navigate } },
      ],
    });
    session = TestBed.inject(CurrentSession);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('should restore the session once without exposing credentials', async () => {
    const first = session.getCurrentSession();
    const second = session.getCurrentSession();
    http
      .expectOne('https://app.example.com/api/sessions/current')
      .flush({ identityUserId: 'subject', userId: 'local-user', email: 'user@example.com' });
    expect(await first).toBe(true);
    expect(await second).toBe(true);
    expect(session.identityUserId).toBe('subject');
    expect(session.userId).toBe('local-user');
    expect(session.email).toBe('user@example.com');
    expect(session.authenticated()).toBe(true);
  });

  it('should create a session then restore the authoritative identity', async () => {
    const revision = session.revision;
    const login = session.createSession('user@example.com', 'ValidPassword123!', true);
    const request = http.expectOne('https://app.example.com/api/sessions');
    expect(request.request.body).toEqual({
      email: 'user@example.com',
      password: 'ValidPassword123!',
      rememberMe: true,
    });
    request.flush(null);
    await Promise.resolve();
    http.expectOne('https://app.example.com/api/sessions/current').flush({
      identityUserId: 'subject',
      userId: 'local-user',
      email: 'user@example.com',
      isAdministrator: true,
    });
    await login;

    expect(session.authenticated()).toBe(true);
    expect(session.isAdministrator).toBe(true);
    expect(session.revision).toBeGreaterThan(revision);
  });

  it('should open the account and default sign-in destinations', async () => {
    await session.account();
    await session.login();

    expect(navigate).toHaveBeenCalledWith('/account');
    expect(navigate).toHaveBeenCalledWith(['/auth/login'], {
      queryParams: { returnUrl: '/users' },
    });
  });

  it('should use local login and sanitize return URLs', async () => {
    await session.login('https://attacker.test', true);
    expect(navigate).toHaveBeenCalledWith(['/auth/register'], {
      queryParams: { returnUrl: '/users' },
    });
  });

  it('should clear session state only after confirmed logout', async () => {
    const initialized = session.getCurrentSession();
    http
      .expectOne('https://app.example.com/api/sessions/current')
      .flush({ identityUserId: 'subject', userId: 'local-user', email: 'user@example.com' });
    await initialized;
    const logout = session.deleteCurrentSession();
    const request = http.expectOne('https://app.example.com/api/sessions/current');
    expect(request.request.method).toBe('DELETE');
    request.flush(null);
    await logout;
    expect(session.authenticated()).toBe(false);
    expect(session.identityUserId).toBeUndefined();
    expect(session.userId).toBeUndefined();
    expect(navigate).toHaveBeenCalledWith('/');
  });

  it('should clear the deleted account and navigate home', async () => {
    const restored = session.getCurrentSession();
    http
      .expectOne('https://app.example.com/api/sessions/current')
      .flush({ identityUserId: 'subject', userId: 'local-user' });
    await restored;

    session.accountDeleted();

    expect(session.authenticated()).toBe(false);
    expect(session.userId).toBeUndefined();
    expect(session.error()).toBeNull();
    expect(navigate).toHaveBeenCalledWith('/', { replaceUrl: true });
  });

  it('should expire authenticated state and redirect to sign-in', async () => {
    const restored = session.getCurrentSession();
    http
      .expectOne('https://app.example.com/api/sessions/current')
      .flush({ identityUserId: 'subject', userId: 'local-user' });
    await restored;

    session.expire();

    expect(session.authenticated()).toBe(false);
    expect(session.error()).toBe('expired');
    expect(navigate).toHaveBeenCalledWith('/auth/login', { replaceUrl: true });
  });

  it('should treat a missing cookie as anonymous', async () => {
    const result = session.getCurrentSession();
    http
      .expectOne('https://app.example.com/api/sessions/current')
      .flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(await result).toBe(false);
    expect(session.authenticated()).toBe(false);
  });

  it('should allow retry after a session lookup outage', async () => {
    const unavailable = session.getCurrentSession();
    const assertion = expect(unavailable).rejects.toMatchObject({ status: 503 });
    http
      .expectOne('https://app.example.com/api/sessions/current')
      .flush({}, { status: 503, statusText: 'Unavailable' });
    await assertion;
    const retry = session.getCurrentSession();
    http
      .expectOne('https://app.example.com/api/sessions/current')
      .flush({ identityUserId: 'subject', userId: 'local-user' });

    expect(await retry).toBe(true);
  });

  it('should discard identity restored after the session was invalidated', async () => {
    const pending = session.getCurrentSession();
    session.expire();
    http
      .expectOne('https://app.example.com/api/sessions/current')
      .flush({ identityUserId: 'subject', userId: 'local-user' });

    expect(await pending).toBe(false);
    expect(session.authenticated()).toBe(false);
    expect(navigate).not.toHaveBeenCalled();
  });

  it('should discard an outdated failure without clearing a newer session', async () => {
    const pending = session.getCurrentSession();
    const old = http.expectOne('https://app.example.com/api/sessions/current');
    session.expire();
    const current = session.getCurrentSession();
    http
      .expectOne('https://app.example.com/api/sessions/current')
      .flush({ identityUserId: 'new', userId: 'new-user' });
    await current;
    old.flush({}, { status: 503, statusText: 'Unavailable' });

    expect(await pending).toBe(false);
    expect(session.userId).toBe('new-user');
    expect(session.authenticated()).toBe(true);
  });

  it('should ignore duplicate logout while the first request is pending', async () => {
    const logout = session.deleteCurrentSession();
    await session.deleteCurrentSession();
    http.expectOne('https://app.example.com/api/sessions/current').flush(null);
    await logout;

    expect(session.busy()).toBe(false);
    expect(navigate).toHaveBeenCalledTimes(1);
  });

  it('should not claim logout succeeded when the server failed', async () => {
    const initialized = session.getCurrentSession();
    http
      .expectOne('https://app.example.com/api/sessions/current')
      .flush({ identityUserId: 'subject', userId: 'local-user', email: 'user@example.com' });
    await initialized;
    const logout = session.deleteCurrentSession();
    http
      .expectOne('https://app.example.com/api/sessions/current')
      .flush({}, { status: 503, statusText: 'Unavailable' });
    await logout;
    expect(session.authenticated()).toBe(true);
    expect(session.error()).toBe('logoutFailed');
    expect(session.busy()).toBe(false);
  });
});
