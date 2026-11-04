import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { AppConfiguration } from '../../../shared/configuration/app-configuration';
import { CurrentSession } from '../../sessions/get-current-session/current-session';
import { AccountAccessPage } from './account-access.page';

describe('AccountAccessPage', () => {
  const session = {
    createSession: vi.fn(),
    authenticated: () => false,
    identityUserId: 'account',
    invalidate: vi.fn(),
  };
  beforeEach(() => {
    document.documentElement.lang = 'en-US';
    session.invalidate.mockReset();
    session.createSession.mockReset().mockResolvedValue(undefined);
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter(
          [
            'login',
            'register',
            'forgot-password',
            'resend-confirmation',
            'reset-password',
            'confirm-email',
            'confirm-email-change',
          ].map((mode) => ({
            path: 'auth/' + mode,
            component: AccountAccessPage,
            data: { mode },
          })),
        ),
        {
          provide: AppConfiguration,
          useValue: { value: { apiUrl: 'https://app.example.com/api' } },
        },
        { provide: CurrentSession, useValue: session },
      ],
    });
  });
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it.each([false, true])(
    'should sign in with rememberMe=%s and retain a safe return route',
    async (rememberMe) => {
      const harness = await RouterTestingHarness.create('/auth/login?returnUrl=%2Fprofile');
      const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
      for (const [name, value] of [
        ['email', 'person@example.com'],
        ['password', 'Test-Password123!'],
      ]) {
        const input = harness.routeNativeElement!.querySelector<HTMLInputElement>(
          '[formcontrolname="' + name + '"]',
        )!;
        input.value = value;
        input.dispatchEvent(new Event('input'));
      }
      const remember =
        harness.routeNativeElement!.querySelector<HTMLInputElement>('input[type="checkbox"]')!;
      remember.checked = rememberMe;
      remember.dispatchEvent(new Event('change'));
      harness
        .routeNativeElement!.querySelector('form')!
        .dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
      await harness.fixture.whenStable();
      expect(session.createSession).toHaveBeenCalledWith(
        'person@example.com',
        'Test-Password123!',
        rememberMe,
      );
      expect(navigate).toHaveBeenCalledWith('/profile', { replaceUrl: true });
      expect(
        harness.routeNativeElement!.querySelector<HTMLInputElement>('[formcontrolname="password"]')!
          .value,
      ).toBe('');
    },
  );

  it('should show a neutral registration result without automatically testing credentials', async () => {
    const harness = await RouterTestingHarness.create('/auth/register');
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    for (const [name, value] of [
      ['email', 'person@example.com'],
      ['password', 'Test-Password123!'],
      ['name', 'Person Name'],
    ]) {
      const input = harness.routeNativeElement!.querySelector<HTMLInputElement>(
        '[formcontrolname="' + name + '"]',
      )!;
      input.value = value;
      input.dispatchEvent(new Event('input'));
    }
    harness
      .routeNativeElement!.querySelector('form')!
      .dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    const request = TestBed.inject(HttpTestingController).expectOne(
      'https://app.example.com/api/auth/register',
    );
    expect(request.request.body).toEqual({
      email: 'person@example.com',
      password: 'Test-Password123!',
      name: 'Person Name',
    });
    request.flush({ requiresEmailConfirmation: false });
    await harness.fixture.whenStable();
    expect(session.createSession).not.toHaveBeenCalled();
    expect(navigate).not.toHaveBeenCalled();
    expect(harness.routeNativeElement!.textContent).toContain('Request processed.');
    expect(harness.routeNativeElement!.querySelector('app-notice button')).toBeNull();
    expect(
      harness.routeNativeElement!.querySelector('[href="/auth/login?returnUrl=%2Fprofile%2Fedit"]'),
    ).not.toBeNull();
  });

  it('should keep profile editing as the next login destination when registration requires confirmation', async () => {
    const harness = await RouterTestingHarness.create('/auth/register');
    for (const [name, value] of [
      ['email', 'person@example.com'],
      ['password', 'Test-Password123!'],
      ['name', 'Person Name'],
    ]) {
      const input = harness.routeNativeElement!.querySelector<HTMLInputElement>(
        '[formcontrolname="' + name + '"]',
      )!;
      input.value = value;
      input.dispatchEvent(new Event('input'));
    }
    harness
      .routeNativeElement!.querySelector('form')!
      .dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    TestBed.inject(HttpTestingController)
      .expectOne('https://app.example.com/api/auth/register')
      .flush({ requiresEmailConfirmation: true });
    await harness.fixture.whenStable();
    expect(session.createSession).not.toHaveBeenCalled();
    expect(
      harness.routeNativeElement!.querySelector('[href="/auth/login?returnUrl=%2Fprofile%2Fedit"]'),
    ).not.toBeNull();
    expect(harness.routeNativeElement!.textContent).toContain('If this email can be registered');
  });

  it('should offer profile editing after the email is confirmed', async () => {
    const harness = await RouterTestingHarness.create(
      '/auth/confirm-email?identityUserId=account&token=confirmation',
    );
    harness
      .routeNativeElement!.querySelector('form')!
      .dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    const request = TestBed.inject(HttpTestingController).expectOne(
      'https://app.example.com/api/auth/confirm-email',
    );
    expect(request.request.body.identityUserId).toBe('account');
    expect(request.request.body.token).toBe('confirmation');
    request.flush(null);
    await harness.fixture.whenStable();
    expect(
      harness.routeNativeElement!.querySelector('[href="/auth/login?returnUrl=%2Fprofile%2Fedit"]'),
    ).not.toBeNull();
    expect(session.createSession).not.toHaveBeenCalled();
  });

  it('should reset the password using the action-link token and clear the secret', async () => {
    const harness = await RouterTestingHarness.create(
      '/auth/reset-password?email=person%40example.com&token=reset-token',
    );
    const root = harness.routeNativeElement!;
    const password = root.querySelector<HTMLInputElement>('[formcontrolname="password"]')!;
    password.value = 'Test-Password123!';
    password.dispatchEvent(new Event('input'));

    root
      .querySelector('form')!
      .dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    const request = TestBed.inject(HttpTestingController).expectOne(
      'https://app.example.com/api/auth/reset-password',
    );
    expect(request.request.body).toEqual({
      email: 'person@example.com',
      password: 'Test-Password123!',
      token: 'reset-token',
    });
    request.flush(null);
    await harness.fixture.whenStable();

    expect(root.querySelector('[role="status"]')).not.toBeNull();
    expect(password.value).toBe('');
    expect(window.location.search).not.toContain('token');
  });

  it.each(['account', 'other-account'])(
    'should confirm email change and invalidate only the matching session for %s',
    async (identityUserId) => {
      const harness = await RouterTestingHarness.create(
        '/auth/confirm-email-change?email=new%40example.com&identityUserId=' +
          identityUserId +
          '&token=confirmation',
      );
      const root = harness.routeNativeElement!;

      root
        .querySelector('form')!
        .dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
      const request = TestBed.inject(HttpTestingController).expectOne(
        'https://app.example.com/api/auth/confirm-email-change',
      );
      expect(request.request.body).toEqual({
        email: 'new@example.com',
        identityUserId,
        token: 'confirmation',
      });
      request.flush(null);
      await harness.fixture.whenStable();

      expect(root.querySelector('[role="status"]')).not.toBeNull();
      expect(session.invalidate).toHaveBeenCalledTimes(identityUserId === 'account' ? 1 : 0);
    },
  );

  it('should show a failed sign-in without retaining the password or navigating', async () => {
    session.createSession.mockRejectedValue({ status: 401 });
    const harness = await RouterTestingHarness.create('/auth/login');
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    const root = harness.routeNativeElement!;
    for (const [name, value] of [
      ['email', 'person@example.com'],
      ['password', 'Test-Password123!'],
    ]) {
      const input = root.querySelector<HTMLInputElement>('[formcontrolname="' + name + '"]')!;
      input.value = value;
      input.dispatchEvent(new Event('input'));
    }

    root
      .querySelector('form')!
      .dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    await harness.fixture.whenStable();

    expect(root.querySelector('[role="alert"]')).not.toBeNull();
    expect(root.querySelector<HTMLInputElement>('[formcontrolname="password"]')!.value).toBe('');
    expect(navigate).not.toHaveBeenCalled();
  });

  it('should show invalid confirmation links as errors instead of successful confirmation', async () => {
    const harness = await RouterTestingHarness.create(
      '/auth/confirm-email?identityUserId=account&token=expired',
    );
    const root = harness.routeNativeElement!;

    root
      .querySelector('form')!
      .dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    TestBed.inject(HttpTestingController)
      .expectOne('https://app.example.com/api/auth/confirm-email')
      .flush(
        { errors: [{ code: 'identity.auth.InvalidToken', field: 'token' }] },
        { status: 400, statusText: 'Bad Request' },
      );
    await harness.fixture.whenStable();

    expect(root.querySelector('[role="alert"]')).not.toBeNull();
    expect(root.querySelector('app-notice[role="status"]')).toBeNull();
    expect(session.invalidate).not.toHaveBeenCalled();
  });

  it('should not submit incomplete registration', async () => {
    const harness = await RouterTestingHarness.create('/auth/register');
    harness
      .routeNativeElement!.querySelector('form')!
      .dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    TestBed.inject(HttpTestingController).expectNone('https://app.example.com/api/auth/register');
    expect(session.createSession).not.toHaveBeenCalled();
  });

  it.each(['login', 'register', 'reset-password'])(
    'should validate each %s field on blur and enable only valid forms',
    async (mode) => {
      const harness = await RouterTestingHarness.create('/auth/' + mode);
      const root = harness.routeNativeElement!;
      const button = root.querySelector<HTMLButtonElement>('button[type="submit"]')!;
      expect(button.disabled).toBe(true);
      expect(root.querySelector('#account-email-error')?.textContent?.trim()).toBe('');
      const email = root.querySelector<HTMLInputElement>('#account-email')!;
      email.value = 'not-an-email';
      email.dispatchEvent(new Event('input'));
      email.dispatchEvent(new Event('blur'));
      harness.fixture.detectChanges();
      expect(email.getAttribute('aria-invalid')).toBe('true');
      expect(root.querySelector('#account-email-error')?.textContent).toContain(
        'Enter a valid email address.',
      );
      email.value = 'person@example.com';
      email.dispatchEvent(new Event('input'));
      const password = root.querySelector<HTMLInputElement>('#account-password')!;
      password.value = 'weak';
      password.dispatchEvent(new Event('input'));
      password.dispatchEvent(new Event('blur'));
      harness.fixture.detectChanges();
      expect(root.querySelector('#account-password-error')?.textContent).toContain(
        '12–128 characters',
      );
      expect(button.disabled).toBe(true);
      password.value = 'Test-Password123!';
      password.dispatchEvent(new Event('input'));
      if (mode === 'register') {
        const name = root.querySelector<HTMLInputElement>('#account-name')!;
        name.value = ' ';
        name.dispatchEvent(new Event('input'));
        name.dispatchEvent(new Event('blur'));
        harness.fixture.detectChanges();
        expect(root.querySelector('#account-name-error')?.textContent).toContain(
          'Enter your name.',
        );
        expect(button.disabled).toBe(true);
        name.value = 'Person Name';
        name.dispatchEvent(new Event('input'));
      }
      harness.fixture.detectChanges();
      expect(email.classList.contains('ng-valid')).toBe(true);
      expect(password.classList.contains('ng-valid')).toBe(true);
      expect(root.querySelector('#account-email-error')?.textContent?.trim()).toBe('');
      expect(root.querySelector('#account-password-error')?.textContent?.trim()).toBe('');
      expect(button.disabled).toBe(false);
      expect(session.createSession).not.toHaveBeenCalled();
    },
  );

  it.each(['forgot-password', 'resend-confirmation'])(
    'should enable %s only for a valid email',
    async (mode) => {
      const harness = await RouterTestingHarness.create('/auth/' + mode);
      const root = harness.routeNativeElement!;
      const button = root.querySelector<HTMLButtonElement>('button[type="submit"]')!;
      expect(button.disabled).toBe(true);
      expect(root.querySelector('input[type="password"]')).toBeNull();
      const email = root.querySelector<HTMLInputElement>('#account-email')!;
      email.value = 'person@example.com';
      email.dispatchEvent(new Event('input'));
      email.dispatchEvent(new Event('blur'));
      harness.fixture.detectChanges();
      expect(button.disabled).toBe(false);
      root
        .querySelector('form')!
        .dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
      const request = TestBed.inject(HttpTestingController).expectOne(
        'https://app.example.com/api/auth/' +
          (mode === 'resend-confirmation' ? 'resend-email-confirmation' : mode),
      );
      expect(request.request.body).toEqual({ email: 'person@example.com' });
      request.flush(null);
      await harness.fixture.whenStable();
      expect(root.textContent).toContain(
        mode === 'forgot-password'
          ? 'instructions to reset your password'
          : 'a new confirmation link',
      );
      expect(root.querySelector('[role="status"]')).not.toBeNull();
      expect(root.querySelector('[aria-label="Dismiss notification"]')).toBeNull();
      expect(root.textContent).not.toContain('Support reference');
    },
  );

  it('should show only relevant account navigation beneath login', async () => {
    const harness = await RouterTestingHarness.create('/auth/login');
    const root = harness.routeNativeElement!;
    expect(root.querySelector('[href="/auth/login"]')).toBeNull();
    expect(root.querySelector('[href="/auth/register"]')).not.toBeNull();
    expect(root.querySelector('[href="/auth/forgot-password"]')).not.toBeNull();
    expect(root.querySelector('[href="/auth/resend-confirmation"]')).not.toBeNull();
    expect(root.querySelector('[href="/"]')).not.toBeNull();
  });
});
