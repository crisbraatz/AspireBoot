import { signal } from '@angular/core';
import { CurrentUser } from '../../users/get-current-user/current-user';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AppConfiguration } from '../../../shared/configuration/app-configuration';
import { CurrentSession } from '../../sessions/get-current-session/current-session';
import { AccountSecurityPage } from './account-security.page';

describe('AccountSecurityPage', () => {
  beforeEach(() => {
    document.documentElement.lang = 'en-US';
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: AppConfiguration,
          useValue: { value: { apiUrl: 'https://app.example.com/api' } },
        },
        { provide: CurrentUser, useValue: { user: signal({ id: 'owner' }) } },
        {
          provide: CurrentSession,
          useValue: { deleteCurrentSession: vi.fn(), expire: vi.fn(), accountDeleted: vi.fn() },
        },
      ],
    });
  });
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('should display session metadata without credentials', async () => {
    const fixture = TestBed.createComponent(AccountSecurityPage);
    fixture.detectChanges();
    TestBed.inject(HttpTestingController)
      .expectOne('https://app.example.com/api/users/owner/sessions?page=1&size=10')
      .flush({
        items: [
          {
            id: 'session-id',
            createdAtUtc: '2026-10-14T12:00:00Z',
            expiresAtUtc: '2026-10-14T20:00:00Z',
            isCurrent: true,
            browser: 'Safari 18',
            operatingSystem: 'macOS',
            device: 'Desktop',
            remoteIpAddress: '203.0.113.8',
          },
        ],
        count: 1,
      });
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain('This session');
    expect(fixture.nativeElement.textContent).toContain('session-id');
    expect(fixture.nativeElement.textContent).toContain('Safari 18');
    expect(fixture.nativeElement.textContent).toContain('macOS');
    expect(fixture.nativeElement.textContent).toContain('203.0.113.8');
    expect(fixture.nativeElement.querySelector('input[type="password"]')).not.toBeNull();
  });

  it('should require the current password before requesting an email change', async () => {
    const fixture = TestBed.createComponent(AccountSecurityPage);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http
      .expectOne('https://app.example.com/api/users/owner/sessions?page=1&size=10')
      .flush({ items: [], count: 0 });
    await fixture.whenStable();
    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    http.expectNone('https://app.example.com/api/auth/email');
    for (const [name, value] of [
      ['newEmail', 'new@example.com'],
      ['password', 'Test-Password123!'],
    ]) {
      const input = form.querySelector<HTMLInputElement>('[formcontrolname="' + name + '"]')!;
      input.value = value;
      input.dispatchEvent(new Event('input'));
    }
    form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    const request = http.expectOne('https://app.example.com/api/auth/email');
    expect(request.request.method).toBe('PATCH');
    expect(request.request.body).toEqual({
      newEmail: 'new@example.com',
      password: 'Test-Password123!',
    });
    request.flush(null);
    await fixture.whenStable();
    expect(form.querySelector<HTMLInputElement>('input[type="password"]')!.value).toBe('');
  });

  it('should send the current backend password contract and expire the session after success', async () => {
    const fixture = TestBed.createComponent(AccountSecurityPage);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http
      .expectOne('https://app.example.com/api/users/owner/sessions?page=1&size=10')
      .flush({ items: [], count: 0 });
    await fixture.whenStable();
    const form = fixture.nativeElement.querySelectorAll('form')[1] as HTMLFormElement;
    for (const [name, value] of [
      ['currentPassword', 'Test-Password123!'],
      ['newPassword', 'Changed-Password123!'],
    ]) {
      const input = form.querySelector<HTMLInputElement>('[formcontrolname="' + name + '"]')!;
      input.value = value;
      input.dispatchEvent(new Event('input'));
    }
    form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    const request = http.expectOne('https://app.example.com/api/auth/password');
    expect(request.request.method).toBe('PATCH');
    expect(request.request.body).toEqual({
      currentPassword: 'Test-Password123!',
      newPassword: 'Changed-Password123!',
    });
    request.flush(null);
    await fixture.whenStable();
    expect(TestBed.inject(CurrentSession).expire).toHaveBeenCalledOnce();
  });

  it('should paginate active sessions and reset the page when resizing', async () => {
    const fixture = TestBed.createComponent(AccountSecurityPage);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http
      .expectOne('https://app.example.com/api/users/owner/sessions?page=1&size=10')
      .flush({ items: [], count: 25 });
    await fixture.whenStable();
    const root = fixture.nativeElement as HTMLElement;
    expect(root.textContent).toContain('25 sessions');
    expect(root.textContent).toContain('Page 1 of 3');
    root.querySelector<HTMLButtonElement>('[aria-label="Next page"]')!.click();
    http
      .expectOne('https://app.example.com/api/users/owner/sessions?page=2&size=10')
      .flush({ items: [], count: 25 });
    await fixture.whenStable();
    const size = root.querySelector('select')!;
    size.value = '20';
    size.dispatchEvent(new Event('change'));
    http
      .expectOne('https://app.example.com/api/users/owner/sessions?page=1&size=20')
      .flush({ items: [], count: 25 });
    await fixture.whenStable();
    expect(root.textContent).toContain('Page 1 of 2');
    expect(size.value).toBe('20');
  });
  it('should confirm ending other sessions in the page dialog and preserve the current session', async () => {
    const fixture = TestBed.createComponent(AccountSecurityPage);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    const sessionsUrl = 'https://app.example.com/api/users/owner/sessions?page=1&size=10';
    http.expectOne(sessionsUrl).flush({ items: [], count: 2 });
    await fixture.whenStable();
    const root = fixture.nativeElement as HTMLElement;
    const dialog = root.querySelector<HTMLDialogElement>(
      '[aria-labelledby="end-others-dialog-title"]',
    )!;
    // The test DOM does not implement native dialog methods.
    dialog.showModal = () => dialog.setAttribute('open', '');
    dialog.close = () => dialog.removeAttribute('open');
    const open = Array.from(root.querySelectorAll<HTMLButtonElement>('button')).find(
      (button) =>
        !button.closest('dialog') && button.textContent?.includes('End all other sessions'),
    )!;
    open.click();
    expect(dialog.open).toBe(true);
    expect(dialog.textContent).toContain('This session will remain active.');
    expect(dialog.querySelector('.btn-primary app-icon')).not.toBeNull();
    http.expectNone('https://app.example.com/api/sessions/others');
    dialog.querySelector<HTMLButtonElement>('.btn-secondary')!.click();
    expect(dialog.open).toBe(false);
    http.expectNone('https://app.example.com/api/sessions/others');
    open.click();
    dialog.querySelector<HTMLButtonElement>('.icon-button')!.click();
    expect(dialog.open).toBe(false);
    http.expectNone('https://app.example.com/api/sessions/others');
    open.click();
    dialog.querySelector<HTMLButtonElement>('.btn-primary')!.click();
    expect(dialog.open).toBe(false);
    const request = http.expectOne('https://app.example.com/api/sessions/others');
    expect(request.request.method).toBe('DELETE');
    request.flush(null);
    await fixture.whenStable();
    http.expectOne(sessionsUrl).flush({ items: [], count: 1 });
    await fixture.whenStable();
    expect(TestBed.inject(CurrentSession).expire).not.toHaveBeenCalled();
    expect(TestBed.inject(CurrentSession).deleteCurrentSession).not.toHaveBeenCalled();
    expect(open.disabled).toBe(true);
  });

  it('should require confirmation and password and delete only the current profile', async () => {
    const fixture = TestBed.createComponent(AccountSecurityPage);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http
      .expectOne('https://app.example.com/api/users/owner/sessions?page=1&size=10')
      .flush({ items: [], count: 0 });
    await fixture.whenStable();
    const form = fixture.nativeElement.querySelectorAll('form')[2] as HTMLFormElement;
    const dialog = fixture.nativeElement.querySelector('dialog') as HTMLDialogElement;
    // The test DOM does not implement native dialog methods.
    dialog.showModal = () => dialog.setAttribute('open', '');
    dialog.close = () => dialog.removeAttribute('open');
    const input = form.querySelector<HTMLInputElement>('input')!;
    input.value = 'Test-Password123!';
    input.dispatchEvent(new Event('input'));
    form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    http.expectNone('https://app.example.com/api/users/me');
    expect(dialog.open).toBe(true);
    dialog.querySelector<HTMLButtonElement>('.btn-secondary')!.click();
    expect(dialog.open).toBe(false);
    http.expectNone('https://app.example.com/api/users/me');
    form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    dialog.querySelector<HTMLButtonElement>('.btn-primary')!.click();
    const request = http.expectOne('https://app.example.com/api/users/me');
    expect(request.request.method).toBe('DELETE');
    expect(request.request.body).toEqual({ password: 'Test-Password123!' });
    request.flush(null);
    await fixture.whenStable();
    expect(TestBed.inject(CurrentSession).accountDeleted).toHaveBeenCalledOnce();
    expect(input.value).toBe('');
    expect(dialog.open).toBe(false);
  });

  it.each([
    {
      index: 0,
      endpoint: 'email',
      fields: { newEmail: 'new@example.com', password: 'Test-Password123!' },
    },
    {
      index: 1,
      endpoint: 'password',
      fields: { currentPassword: 'Test-Password123!', newPassword: 'Changed-Password123!' },
    },
  ])(
    'should preserve the session and clear secrets when $endpoint is rejected',
    async ({ index, endpoint, fields }) => {
      const fixture = TestBed.createComponent(AccountSecurityPage);
      fixture.detectChanges();
      const http = TestBed.inject(HttpTestingController);
      http
        .expectOne('https://app.example.com/api/users/owner/sessions?page=1&size=10')
        .flush({ items: [], count: 0 });
      await fixture.whenStable();
      const root = fixture.nativeElement as HTMLElement;
      const form = root.querySelectorAll('form')[index];
      for (const [name, value] of Object.entries(fields)) {
        const input = form.querySelector<HTMLInputElement>('[formcontrolname="' + name + '"]')!;
        input.value = value;
        input.dispatchEvent(new Event('input'));
      }

      form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
      http
        .expectOne('https://app.example.com/api/auth/' + endpoint)
        .flush(
          { code: 'identity.auth.invalid-credentials' },
          { status: 401, statusText: 'Unauthorized' },
        );
      await fixture.whenStable();

      expect(root.querySelector('[role="alert"]')).not.toBeNull();
      expect(TestBed.inject(CurrentSession).expire).not.toHaveBeenCalled();
      for (const input of form.querySelectorAll<HTMLInputElement>('input[type="password"]'))
        expect(input.value).toBe('');
    },
  );

  it('should keep session metadata unavailable after a failed read and allow retry', async () => {
    const fixture = TestBed.createComponent(AccountSecurityPage);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    const url = 'https://app.example.com/api/users/owner/sessions?page=1&size=10';

    http.expectOne(url).flush({}, { status: 503, statusText: 'Unavailable' });
    await fixture.whenStable();
    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[role="alert"]')).not.toBeNull();
    const retry = Array.from(root.querySelectorAll<HTMLButtonElement>('button')).find((button) =>
      button.textContent?.includes('Try again'),
    )!;
    retry.click();
    http.expectOne(url).flush({ items: [], count: 0 });
    await fixture.whenStable();

    expect(root.querySelector('[role="alert"]')).toBeNull();
    expect(TestBed.inject(CurrentSession).expire).not.toHaveBeenCalled();
  });

  it('should reject the current password as the new password without ending the session', async () => {
    const fixture = TestBed.createComponent(AccountSecurityPage);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http
      .expectOne('https://app.example.com/api/users/owner/sessions?page=1&size=10')
      .flush({ items: [], count: 0 });
    await fixture.whenStable();
    const form = fixture.nativeElement.querySelectorAll('form')[1] as HTMLFormElement;
    for (const input of form.querySelectorAll('input')) {
      input.value = 'Test-Password123!';
      input.dispatchEvent(new Event('input'));
      input.dispatchEvent(new Event('blur'));
    }
    form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    fixture.detectChanges();
    http.expectNone('https://app.example.com/api/auth/password');
    expect(form.textContent).toContain('Choose a password different from your current password.');
    expect(TestBed.inject(CurrentSession).expire).not.toHaveBeenCalled();
  });

  it('should not substitute the identity subject for a missing local profile ID', async () => {
    TestBed.overrideProvider(CurrentUser, { useValue: { user: signal(null) } });
    const fixture = TestBed.createComponent(AccountSecurityPage);
    fixture.detectChanges();
    await fixture.whenStable();
    const http = TestBed.inject(HttpTestingController);
    http.expectNone((request) => request.url.includes('/sessions'));
    expect(fixture.nativeElement.textContent).toContain('temporarily unavailable');
  });

  it('should validate security fields separately on blur and disable invalid submissions', async () => {
    const fixture = TestBed.createComponent(AccountSecurityPage);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http
      .expectOne('https://app.example.com/api/users/owner/sessions?page=1&size=10')
      .flush({ items: [], count: 0 });
    await fixture.whenStable();
    const root = fixture.nativeElement as HTMLElement;
    const forms = root.querySelectorAll('form');
    expect(forms[0].querySelector('button')!.disabled).toBe(true);
    expect(forms[1].querySelector('button')!.disabled).toBe(true);
    for (const id of ['email-password', 'old-password', 'new-password']) {
      const input = root.querySelector<HTMLInputElement>('#' + id)!;
      input.value = 'weak';
      input.dispatchEvent(new Event('input'));
      input.dispatchEvent(new Event('blur'));
    }
    const email = root.querySelector<HTMLInputElement>('#new-email')!;
    email.value = 'invalid';
    email.dispatchEvent(new Event('input'));
    email.dispatchEvent(new Event('blur'));
    fixture.detectChanges();
    expect(root.querySelector('#new-email-error')?.textContent).toContain(
      'Enter a valid email address.',
    );
    for (const id of ['email-password-error', 'old-password-error', 'new-password-error'])
      expect(root.querySelector('#' + id)?.textContent).toContain('12–128 characters');
    email.value = 'person@example.com';
    email.dispatchEvent(new Event('input'));
    for (const id of ['email-password', 'old-password', 'new-password']) {
      const input = root.querySelector<HTMLInputElement>('#' + id)!;
      input.value = id === 'new-password' ? 'Changed-Password123!' : 'Test-Password123!';
      input.dispatchEvent(new Event('input'));
    }
    fixture.detectChanges();
    expect(forms[0].querySelector('button')!.disabled).toBe(false);
    expect(forms[1].querySelector('button')!.disabled).toBe(false);
    expect(root.querySelector('#new-email-error')?.textContent?.trim()).toBe('');
    expect(root.querySelector('#new-password-error')?.textContent?.trim()).toBe('');
    expect(root.querySelector('[href="/profile"]')).not.toBeNull();
  });
});
