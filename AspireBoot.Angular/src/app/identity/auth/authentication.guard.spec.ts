import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  provideRouter,
  Router,
  RouterStateSnapshot,
} from '@angular/router';
import { CurrentUser } from '../users/get-current-user/current-user';
import { authenticationGuard, guestGuard, profileGuard } from './authentication.guard';
import { CurrentSession } from '../sessions/get-current-session/current-session';

describe('authentication guards', () => {
  let session: {
    authenticated: ReturnType<typeof signal<boolean>>;
    getCurrentSession: ReturnType<typeof vi.fn>;
    login: ReturnType<typeof vi.fn>;
    error: ReturnType<typeof signal<string | null>>;
  };
  let current: { ensureLoaded: ReturnType<typeof vi.fn> };
  beforeEach(() => {
    session = {
      authenticated: signal(false),
      getCurrentSession: vi.fn().mockResolvedValue(true),
      login: vi.fn().mockResolvedValue(undefined),
      error: signal(null),
    };
    current = { ensureLoaded: vi.fn().mockResolvedValue({ id: 'local-user' }) };
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: CurrentSession, useValue: session },
        { provide: CurrentUser, useValue: current },
      ],
    });
  });
  it('should allow an authenticated initialized profile', async () => {
    expect(
      await TestBed.runInInjectionContext(() =>
        authenticationGuard({} as ActivatedRouteSnapshot, { url: '/users' } as RouterStateSnapshot),
      ),
    ).toBe(true);
    expect(
      await TestBed.runInInjectionContext(() =>
        profileGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
      ),
    ).toBe(true);
    expect(session.login).not.toHaveBeenCalled();
  });
  it('should send an anonymous user to the local login page and preserve the intended route', async () => {
    session.getCurrentSession.mockResolvedValue(false);
    const result = await TestBed.runInInjectionContext(() =>
      authenticationGuard(
        {} as ActivatedRouteSnapshot,
        { url: '/users?page=2' } as RouterStateSnapshot,
      ),
    );
    expect(session.login).not.toHaveBeenCalled();
    expect(TestBed.inject(Router).serializeUrl(result as ReturnType<Router['parseUrl']>)).toBe(
      '/auth/login?returnUrl=%2Fusers%3Fpage%3D2',
    );
  });
  it('should block a missing profile as an account inconsistency', async () => {
    current.ensureLoaded.mockResolvedValue(null);
    const result = await TestBed.runInInjectionContext(() =>
      profileGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
    );
    expect(result).toBe(false);
    expect(session.error()).toBe('unavailable');
  });
  it('should surface a profile loading failure', async () => {
    current.ensureLoaded.mockRejectedValue(new Error('Unavailable.'));
    const result = await TestBed.runInInjectionContext(() =>
      profileGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
    );
    expect(result).toBe(false);
    expect(session.error()).toBe('unavailable');
  });

  it('should redirect authenticated home visits to users without showing the hero', () => {
    session.authenticated.set(true);
    const result = TestBed.runInInjectionContext(() =>
      guestGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
    );
    expect(TestBed.inject(Router).serializeUrl(result as ReturnType<Router['parseUrl']>)).toBe(
      '/users',
    );
  });

  it('should allow anonymous visitors to see the home page', () => {
    expect(
      TestBed.runInInjectionContext(() =>
        guestGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
      ),
    ).toBe(true);
  });

  it.each(['login', 'register', 'forgot-password', 'reset-password', 'resend-confirmation'])(
    'should redirect authenticated visitors away from %s',
    (mode) => {
      session.authenticated.set(true);
      const result = TestBed.runInInjectionContext(() =>
        guestGuard(
          { data: { mode } } as unknown as ActivatedRouteSnapshot,
          {} as RouterStateSnapshot,
        ),
      );
      expect(TestBed.inject(Router).serializeUrl(result as ReturnType<Router['parseUrl']>)).toBe(
        '/users',
      );
    },
  );

  it.each(['confirm-email', 'confirm-email-change'])(
    'should preserve %s links outside the guest area for authenticated visitors',
    (mode) => {
      session.authenticated.set(true);
      const result = TestBed.runInInjectionContext(() =>
        guestGuard(
          {
            data: { mode },
            queryParams: { identityUserId: 'account', token: 'confirmation' },
          } as unknown as ActivatedRouteSnapshot,
          {} as RouterStateSnapshot,
        ),
      );
      expect(TestBed.inject(Router).serializeUrl(result as ReturnType<Router['parseUrl']>)).toBe(
        '/account/' + mode + '?identityUserId=account&token=confirmation',
      );
    },
  );
});
