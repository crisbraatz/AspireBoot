import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { of, throwError } from 'rxjs';
import { CurrentUser } from '../get-current-user/current-user';
import { UsersApi } from '../users-api';
import { ListUsersPage } from './list-users.page';

describe('ListUsersPage', () => {
  let api: { listUsers: ReturnType<typeof vi.fn> };
  beforeEach(() => {
    document.documentElement.lang = 'en-US';
    api = {
      listUsers: vi
        .fn()
        .mockImplementation(({ page, size }: { page: number; size: number }) =>
          of({ items: [{ id: 'person', name: 'Person Name' }], page, size, count: 25 }),
        ),
    };
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'users', component: ListUsersPage }]),
        { provide: UsersApi, useValue: api },
        { provide: CurrentUser, useValue: { user: signal({ id: 'owner' }) } },
      ],
    });
  });
  it('should bind pagination to the URL and navigate to the next server page', async () => {
    const harness = await RouterTestingHarness.create('/users?page=2&size=10');
    expect(api.listUsers).toHaveBeenCalledWith({ page: 2, size: 10 });
    expect(harness.routeNativeElement?.textContent).toContain('Person Name');
    expect(harness.routeNativeElement?.textContent).toContain('Page 2 of 3');
    (
      harness.routeNativeElement?.querySelector('[aria-label="Next page"]') as HTMLButtonElement
    ).click();
    await harness.fixture.whenStable();
    expect(api.listUsers).toHaveBeenLastCalledWith({ page: 3, size: 10 });
    expect(harness.routeNativeElement?.textContent).toContain('Page 3 of 3');
  });
  it('should show an empty page without fabricating people', async () => {
    api.listUsers.mockReturnValue(of({ items: [], page: 1, size: 10, count: 0 }));
    const harness = await RouterTestingHarness.create('/users');
    expect(harness.routeNativeElement?.textContent).toContain('No users to show yet.');
    expect(harness.routeNativeElement?.querySelectorAll('li').length).toBe(0);
  });

  it('should identify only the owner beneath the name without public-profile labels', async () => {
    api.listUsers.mockReturnValue(
      of({
        items: [
          { id: 'owner', name: 'Owner Name', isActive: true },
          { id: 'other', name: 'Other Name', isActive: false, isAdministrator: true },
        ],
        page: 1,
        size: 10,
        count: 2,
      }),
    );
    const harness = await RouterTestingHarness.create('/users');
    const rows = harness.routeNativeElement!.querySelectorAll('li');
    expect(rows[0].querySelector('p')?.textContent?.trim()).toBe('Owner Name');
    expect(rows[0].querySelectorAll('p')[1].textContent?.trim()).toBe('You');
    expect(rows[1].textContent).not.toContain('You');
    expect(rows[0].textContent).toContain('Active');
    expect(rows[1].textContent).toContain('Inactive');
    expect(rows[1].textContent).toContain('Administrator');
    expect(rows[0].textContent).not.toContain('Administrator');
    const badges = rows[1].querySelectorAll('.min-w-0 > span.border-line');
    expect(badges[0].textContent).toContain('Administrator');
    expect(badges[1].textContent).toContain('Inactive');
    expect(harness.routeNativeElement!.textContent).not.toContain('Public profile');
  });
  it('should retry a failed read without restarting authentication or changing pagination', async () => {
    api.listUsers.mockReturnValueOnce(throwError(() => new HttpErrorResponse({ status: 503 })));
    const harness = await RouterTestingHarness.create('/users?page=2&size=10');
    expect(harness.routeNativeElement?.textContent).toContain('temporarily unavailable');
    const button = Array.from(harness.routeNativeElement!.querySelectorAll('button')).find(
      (value) => value.textContent?.includes('Try again'),
    )!;
    button.click();
    await harness.fixture.whenStable();
    expect(api.listUsers).toHaveBeenLastCalledWith({ page: 2, size: 10 });
    expect(harness.routeNativeElement?.textContent).toContain('Person Name');
  });
});
