import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AppConfiguration } from '../../../shared/configuration/app-configuration';
import { ListUserSessions } from './list-user-sessions';

describe('ListUserSessions', () => {
  beforeEach(() => {
    document.documentElement.lang = 'en-US';
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: AppConfiguration,
          useValue: { value: { apiUrl: 'https://app.example.com/api' } },
        },
      ],
    });
  });
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('should display session totals and use the shared pagination and page size', async () => {
    const fixture = TestBed.createComponent(ListUserSessions);
    fixture.componentRef.setInput('userId', 'owner');
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('https://app.example.com/api/users/owner/sessions?page=1&size=10').flush({
      items: [
        {
          id: 'session',
          createdAtUtc: '2026-10-15T12:00:00Z',
          expiresAtUtc: '2026-10-15T20:00:00Z',
          isCurrent: true,
          browser: 'Firefox 132',
          operatingSystem: 'Linux',
          device: 'Desktop',
          remoteIpAddress: '203.0.113.7',
        },
      ],
      count: 25,
    });
    await fixture.whenStable();
    const root = fixture.nativeElement as HTMLElement;
    expect(root.textContent).toContain('25 sessions');
    expect(root.textContent).toContain('This session');
    expect(root.textContent).toContain('Firefox 132');
    expect(root.textContent).toContain('Linux');
    expect(root.textContent).toContain('203.0.113.7');
    expect(root.textContent).toContain('Page 1 of 3');
    root.querySelector<HTMLButtonElement>('[aria-label="Next page"]')!.click();
    fixture.detectChanges();
    http
      .expectOne('https://app.example.com/api/users/owner/sessions?page=2&size=10')
      .flush({ items: [], count: 25 });
    await fixture.whenStable();
    expect(root.textContent).toContain('Page 2 of 3');
    const size = root.querySelector('select')!;
    size.value = '20';
    size.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    http
      .expectOne('https://app.example.com/api/users/owner/sessions?page=1&size=20')
      .flush({ items: [], count: 25 });
    await fixture.whenStable();
    expect(root.textContent).toContain('Page 1 of 2');
    expect(size.value).toBe('20');
  });

  it('should omit the current-session label for another session of the owner', async () => {
    const fixture = TestBed.createComponent(ListUserSessions);
    fixture.componentRef.setInput('userId', 'owner');
    fixture.detectChanges();
    TestBed.inject(HttpTestingController)
      .expectOne('https://app.example.com/api/users/owner/sessions?page=1&size=10')
      .flush({
        items: [
          {
            id: 'session',
            createdAtUtc: '2026-10-15T12:00:00Z',
            expiresAtUtc: '2026-10-15T20:00:00Z',
          },
        ],
        count: 1,
      });
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain('1 sessions');
    expect(fixture.nativeElement.textContent).not.toContain('This session');
    expect(fixture.nativeElement.querySelector('li button')).toBeNull();
  });

  it('should show an empty result rather than a fabricated session', async () => {
    const fixture = TestBed.createComponent(ListUserSessions);
    fixture.componentRef.setInput('userId', 'owner');
    fixture.detectChanges();
    TestBed.inject(HttpTestingController)
      .expectOne('https://app.example.com/api/users/owner/sessions?page=1&size=10')
      .flush({ items: [], count: 0 });
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain('0 sessions');
    expect(fixture.nativeElement.textContent).toContain('No active sessions to show.');
    expect(fixture.nativeElement.querySelector('[aria-label="Next page"]').disabled).toBe(true);
  });

  it('should report unavailable sessions without showing stale items', async () => {
    const fixture = TestBed.createComponent(ListUserSessions);
    fixture.componentRef.setInput('userId', 'owner');
    fixture.detectChanges();
    TestBed.inject(HttpTestingController)
      .expectOne('https://app.example.com/api/users/owner/sessions?page=1&size=10')
      .flush(null, { status: 503, statusText: 'Unavailable' });
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain('temporarily unavailable');
    expect(fixture.nativeElement.textContent).not.toContain('No active sessions to show.');
    expect(fixture.nativeElement.querySelectorAll('li').length).toBe(0);
  });
});
