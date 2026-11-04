import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { of, Subject } from 'rxjs';
import { routes } from './app.routes';
import { CurrentSession } from './identity/sessions/get-current-session/current-session';
import { CurrentUser } from './identity/users/get-current-user/current-user';
import { UsersApi } from './identity/users/users-api';
import { AuthApi } from './identity/auth/auth-api';
import { SessionsApi } from './identity/sessions/sessions-api';
import { ListUsersResponse } from './identity/users/list-users/list-users-response';

describe('application routes', () => {
  const user = {
    id: 'owner',
    name: 'Owner Name',
    email: 'owner@example.com',
    isActive: true,
    isAdministrator: false,
  };
  let authenticated: ReturnType<typeof signal<boolean>>;
  let listUsers: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    document.documentElement.lang = 'en-US';
    authenticated = signal(true);
    listUsers = vi.fn().mockReturnValue(of({ items: [user], page: 1, size: 10, count: 1 }));
    TestBed.configureTestingModule({
      providers: [
        provideRouter(routes),
        {
          provide: CurrentSession,
          useValue: {
            authenticated,
            error: signal(null),
            getCurrentSession: async () => authenticated(),
          },
        },
        {
          provide: CurrentUser,
          useValue: {
            user: signal(user),
            ensureLoaded: async () => user,
            reload: async () => user,
          },
        },
        {
          provide: UsersApi,
          useValue: {
            listUsers,
            getUser: () => of(user),
            listUserSessions: () => of({ items: [], page: 1, size: 10, count: 0 }),
          },
        },
        { provide: AuthApi, useValue: {} },
        { provide: SessionsApi, useValue: {} },
      ],
    });
  });

  it.each([
    ['/users', 'Users'],
    ['/users/owner', 'Owner Name'],
    ['/profile', 'Owner Name'],
    ['/profile/edit', 'Name'],
    ['/account', 'Account'],
    ['/account/confirm-email?identityUserId=owner&token=example', 'Confirm'],
    [
      '/account/confirm-email-change?identityUserId=owner&token=example&email=new%40example.com',
      'Confirm',
    ],
  ])('should load the authenticated feature at %s', async (url, text) => {
    const harness = await RouterTestingHarness.create(url);

    expect(TestBed.inject(Router).url).toBe(
      TestBed.inject(Router).serializeUrl(TestBed.inject(Router).parseUrl(url)),
    );
    expect(harness.routeNativeElement?.textContent).toContain(text);
  });

  it.each([
    'login',
    'register',
    'forgot-password',
    'reset-password',
    'confirm-email',
    'confirm-email-change',
    'resend-confirmation',
  ])('should load the public %s feature', async (mode) => {
    authenticated.set(false);

    const harness = await RouterTestingHarness.create('/auth/' + mode);

    expect(harness.routeNativeElement?.querySelector('form')).not.toBeNull();
    expect(TestBed.inject(Router).url).toBe('/auth/' + mode);
  });

  it('should display loading until the user directory request completes', async () => {
    const response = new Subject<ListUsersResponse>();
    listUsers.mockReturnValue(response);
    const harness = await RouterTestingHarness.create('/users');

    expect(harness.routeNativeElement?.querySelector('[role="status"]')?.textContent).toContain(
      'Loading',
    );
    response.next({ items: [user], page: 1, size: 10, count: 1 });
    response.complete();
    harness.detectChanges();

    expect(harness.routeNativeElement?.querySelector('app-loading-state')).toBeNull();
    expect(harness.routeNativeElement?.textContent).toContain('Owner Name');
  });

  it('should redirect anonymous visitors to sign-in with the protected destination', async () => {
    authenticated.set(false);

    const harness = await RouterTestingHarness.create('/profile/edit');

    expect(TestBed.inject(Router).url).toBe('/auth/login?returnUrl=%2Fprofile%2Fedit');
    expect(harness.routeNativeElement?.querySelector('form')).not.toBeNull();
    expect(listUsers).not.toHaveBeenCalled();
  });

  it.each([true, false])(
    'should offer a safe destination for unknown pages when authenticated=%s',
    async (value) => {
      authenticated.set(value);

      const harness = await RouterTestingHarness.create('/missing-page');

      expect(harness.routeNativeElement?.textContent).toContain('404');
      expect(harness.routeNativeElement?.querySelector('a')?.getAttribute('href')).toBe(
        value ? '/users' : '/',
      );
    },
  );
});
