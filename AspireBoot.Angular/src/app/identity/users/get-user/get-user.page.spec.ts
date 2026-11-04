import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AppConfiguration } from '../../../shared/configuration/app-configuration';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { of, throwError } from 'rxjs';
import { CurrentSession } from '../../sessions/get-current-session/current-session';
import { CurrentUser } from '../get-current-user/current-user';
import { UsersApi } from '../users-api';
import { GetUserPage } from './get-user.page';

describe('GetUserPage', () => {
  const owner = { id: 'owner', name: 'Owner Name', email: 'owner@example.com' };
  let api: { getUser: ReturnType<typeof vi.fn> };
  beforeEach(() => {
    document.documentElement.lang = 'en-US';
    api = {
      getUser: vi.fn().mockReturnValue(of({ id: 'other', name: 'Other Name', isActive: true })),
    };
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: AppConfiguration,
          useValue: { value: { apiUrl: 'https://app.example.com/api' } },
        },
        provideRouter([
          { path: 'users/:id', component: GetUserPage },
          { path: 'profile', component: GetUserPage },
          { path: 'users', component: GetUserPage },
        ]),
        { provide: UsersApi, useFactory: () => Object.assign(new UsersApi(), api) },
        {
          provide: CurrentUser,
          useValue: { user: signal(owner), reload: () => Promise.resolve(owner) },
        },
        { provide: CurrentSession, useValue: { account: vi.fn() } },
      ],
    });
  });
  it('should show a dismissible saved notice only for the save navigation', async () => {
    const harness = await RouterTestingHarness.create();
    const router = TestBed.inject(Router);
    await router.navigateByUrl('/profile', { info: { saved: true } });
    harness.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http
      .expectOne('https://app.example.com/api/users/owner/sessions?page=1&size=10')
      .flush({ items: [], count: 0 });
    expect(harness.routeNativeElement?.textContent).toContain('Changes saved.');
    harness.routeNativeElement?.querySelector<HTMLButtonElement>('app-notice button')!.click();
    harness.detectChanges();
    expect(harness.routeNativeElement?.textContent).not.toContain('Changes saved.');
    await harness.navigateByUrl('/users/other');
    await router.navigateByUrl('/profile', { state: { saved: true } });
    harness.detectChanges();
    http
      .expectOne('https://app.example.com/api/users/owner/sessions?page=1&size=10')
      .flush({ items: [], count: 0 });
    expect(harness.routeNativeElement?.textContent).not.toContain('Changes saved.');
    http.verify();
  });

  it('should render only public fields and never an edit link for another user', async () => {
    const harness = await RouterTestingHarness.create('/users/other');
    expect(harness.routeNativeElement?.textContent).toContain('Other Name');
    expect(harness.routeNativeElement?.textContent).not.toContain('owner@example.com');
    expect(harness.routeNativeElement?.querySelector('dl')).toBeNull();
    expect(harness.routeNativeElement?.querySelector('[href="/profile/edit"]')).toBeNull();
    expect(api.getUser).toHaveBeenCalledWith('other');
  });
  it('should show the administrator badge to an ordinary viewer without granting editing', async () => {
    api.getUser.mockReturnValue(
      of({ id: 'other', name: 'Admin Name', isActive: true, isAdministrator: true }),
    );
    const harness = await RouterTestingHarness.create('/users/other');
    expect(harness.routeNativeElement?.textContent).toContain('Administrator');
    expect(harness.routeNativeElement?.querySelector('[href="/profile/edit"]')).toBeNull();
    const badges = harness.routeNativeElement!.querySelectorAll(
      'article .min-w-0 > span.border-line',
    );
    expect(badges[0].textContent).toContain('Administrator');
    expect(badges[1].textContent).toContain('Active');
  });

  it('should render user-controlled names as text, never HTML', async () => {
    const malicious = '<img src=x onerror=alert(1)>';
    api.getUser.mockReturnValue(of({ id: 'other', name: malicious }));
    const harness = await RouterTestingHarness.create('/users/other');
    expect(harness.routeNativeElement?.querySelector('h2')?.textContent).toContain(malicious);
    expect(harness.routeNativeElement?.querySelector('img[onerror]')).toBeNull();
  });
  it('should show email granted by the API without granting editing rights', async () => {
    api.getUser.mockReturnValue(
      of({ id: 'other', name: 'Other Name', email: 'private@example.com' }),
    );
    const harness = await RouterTestingHarness.create('/users/other');
    expect(harness.routeNativeElement?.textContent).toContain('private@example.com');
    expect(harness.routeNativeElement?.querySelector('[href="/profile/edit"]')).toBeNull();
  });
  it('should not request another users sessions even for an administrator', async () => {
    TestBed.overrideProvider(CurrentSession, {
      useValue: { account: vi.fn(), isAdministrator: true },
    });
    const harness = await RouterTestingHarness.create('/users/other');
    TestBed.tick();
    expect(harness.routeNativeElement?.querySelector('app-list-user-sessions')).toBeNull();
    TestBed.inject(HttpTestingController).expectNone((request) =>
      request.url.endsWith('/sessions'),
    );
  });
  it('should allow the owner to navigate to editing and account security', async () => {
    const harness = await RouterTestingHarness.create('/profile');
    expect(harness.routeNativeElement?.textContent).toContain('Owner Name');
    expect(harness.routeNativeElement?.textContent).toContain('owner@example.com');
    expect(harness.routeNativeElement?.querySelector('[href="/profile/edit"]')).not.toBeNull();
    expect(harness.routeNativeElement?.textContent).toContain('Account & security');
    expect(harness.routeNativeElement?.textContent).not.toContain('Public profile');
    TestBed.tick();
    TestBed.inject(HttpTestingController)
      .expectOne('https://app.example.com/api/users/owner/sessions?page=1&size=10')
      .flush({ items: [], count: 0 });
  });
  it('should redirect an unavailable profile to the directory', async () => {
    api.getUser.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 404 })));
    const harness = await RouterTestingHarness.create();
    const navigation = vi.spyOn(TestBed.inject(Router), 'navigateByUrl');
    await harness.navigateByUrl('/users/inactive');
    expect(navigation).toHaveBeenCalledWith('/users', { replaceUrl: true });
  });
  it('should show the inactive badge for a profile returned to an administrator', async () => {
    TestBed.overrideProvider(CurrentSession, { useValue: { isAdministrator: true } });
    api.getUser.mockReturnValue(of({ id: 'other', name: 'Other Name', isActive: false }));
    const harness = await RouterTestingHarness.create('/users/other');
    expect(harness.routeNativeElement?.textContent).toContain('Inactive');
    expect(harness.routeNativeElement?.querySelector('[href="/profile/edit"]')).toBeNull();
  });
  it('should display a temporary profile failure and retry the same user', async () => {
    api.getUser.mockReturnValueOnce(throwError(() => new HttpErrorResponse({ status: 503 })));
    const harness = await RouterTestingHarness.create('/users/other');
    const root = harness.routeNativeElement!;
    expect(root.querySelector('[role="alert"]')).not.toBeNull();
    const retry = Array.from(root.querySelectorAll<HTMLButtonElement>('button')).find((button) =>
      button.textContent?.includes('Try again'),
    )!;

    retry.click();
    await harness.fixture.whenStable();

    expect(api.getUser).toHaveBeenLastCalledWith('other');
    expect(root.textContent).toContain('Other Name');
    expect(root.querySelector('[role="alert"]')).toBeNull();
  });
});
