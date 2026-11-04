import { signal } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { App } from './app';
import { routes } from './app.routes';
import { CurrentSession } from './identity/sessions/get-current-session/current-session';
import { CurrentUser } from './identity/users/get-current-user/current-user';
import { AppConfiguration } from './shared/configuration/app-configuration';

describe('App', () => {
  let session: {
    authenticated: ReturnType<typeof signal<boolean>>;
    busy: ReturnType<typeof signal<boolean>>;
    error: ReturnType<typeof signal<null>>;
    login: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    document.documentElement.lang = 'en-US';
    document.documentElement.dataset['theme'] = 'light';
    localStorage.removeItem('aspireboot.theme');
    localStorage.removeItem('aspireboot.locale');
    session = {
      authenticated: signal(false),
      busy: signal(false),
      error: signal(null),
      login: vi.fn(),
    };
    TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: AppConfiguration,
          useValue: { value: { apiUrl: 'https://app.example.com/api' } },
        },
        provideRouter(routes),
        { provide: CurrentSession, useValue: session },
        { provide: CurrentUser, useValue: { user: signal(null) } },
      ],
    });
  });

  it('should render the branded public home without contacting identity', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await TestBed.inject(Router).navigateByUrl('/');
    await fixture.whenStable();
    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('h1')?.textContent).toContain('Your home page.');
    expect(root.querySelector('header img')?.getAttribute('src')).toBe(
      '/assets/brand/logo-dark.svg',
    );
    expect(root.querySelector('footer')?.textContent).toContain('AspireBoot');
    expect(root.querySelector('input[type="password"]')).toBeNull();
    expect(root.querySelector('main button, main a')).toBeNull();
    expect(root.querySelector('footer')?.textContent?.trim()).toBe(
      `© ${new Date().getFullYear()} AspireBoot`,
    );
    const header = root.querySelector('header')!;
    expect(
      header.querySelector('nav')!.compareDocumentPosition(header.querySelector('#locale')!) &
        Node.DOCUMENT_POSITION_FOLLOWING,
    ).not.toBe(0);
    expect(session.login).not.toHaveBeenCalled();
  });

  it('should switch both theme and language and persist only preferences', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await TestBed.inject(Router).navigateByUrl('/');
    await fixture.whenStable();
    const root = fixture.nativeElement as HTMLElement;
    (root.querySelector('[aria-label="Switch to dark theme"]') as HTMLButtonElement).click();
    const locale = root.querySelector('#locale') as HTMLSelectElement;
    locale.value = 'pt-BR';
    locale.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    expect(document.documentElement.dataset['theme']).toBe('dark');
    expect(document.documentElement.lang).toBe('pt-BR');
    expect(root.querySelector('h1')?.textContent).toContain('Sua página inicial.');
    expect(root.querySelector('header img')?.getAttribute('src')).toBe(
      '/assets/brand/logo-light.svg',
    );
    expect(localStorage.getItem('aspireboot.theme')).toBe('dark');
    expect(localStorage.getItem('aspireboot.locale')).toBe('pt-BR');
    (root.querySelector('[aria-label="Usar tema claro"]') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(document.documentElement.dataset['theme']).toBe('light');
  });

  it('should open registration with profile editing as the return destination', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await TestBed.inject(Router).navigateByUrl('/');
    await fixture.whenStable();
    const button = Array.from(
      (fixture.nativeElement as HTMLElement).querySelectorAll('button'),
    ).find((element) => element.textContent?.includes('Create account'))!;
    button.click();
    expect(button.querySelector('app-icon')).not.toBeNull();
    expect(session.login).toHaveBeenCalledWith('/profile/edit', true);
  });

  it('should hide navbar account actions on account access pages', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await TestBed.inject(Router).navigateByUrl('/auth/register');
    await fixture.whenStable();
    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('header nav button')).toBeNull();
    expect(root.querySelector('#locale')).not.toBeNull();
    expect(root.querySelector('main [href="/auth/login"]')).not.toBeNull();
    expect(root.querySelector('main [href="/auth/register"]')).toBeNull();
    expect(root.querySelector('main [href="/auth/forgot-password"]')).toBeNull();
    expect(root.querySelector('main [href="/"]')).not.toBeNull();
  });

  it('should toggle the mobile navigation, close it with Escape and return focus', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await TestBed.inject(Router).navigateByUrl('/');
    await fixture.whenStable();
    const root = fixture.nativeElement as HTMLElement;
    const toggle = root.querySelector<HTMLButtonElement>('[aria-controls="main-navigation"]')!;
    const menu = root.querySelector('#main-navigation')!;
    expect(toggle.getAttribute('aria-expanded')).toBe('false');
    expect(menu.classList.contains('hidden')).toBe(true);
    expect(root.querySelector('header')?.classList.contains('fixed')).toBe(true);
    toggle.click();
    fixture.detectChanges();
    expect(toggle.getAttribute('aria-expanded')).toBe('true');
    expect(menu.classList.contains('hidden')).toBe(false);
    root
      .querySelector('header')!
      .dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    fixture.detectChanges();
    expect(toggle.getAttribute('aria-expanded')).toBe('false');
    expect(menu.classList.contains('hidden')).toBe(true);
  });

  it('should close the mobile menu after navigation', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const router = TestBed.inject(Router);
    await router.navigateByUrl('/');
    await fixture.whenStable();
    const root = fixture.nativeElement as HTMLElement;
    root.querySelector<HTMLButtonElement>('[aria-controls="main-navigation"]')!.click();
    fixture.detectChanges();
    await router.navigateByUrl('/auth/login');
    await fixture.whenStable();
    expect(
      root.querySelector('[aria-controls="main-navigation"]')?.getAttribute('aria-expanded'),
    ).toBe('false');
    expect(root.querySelector('#main-navigation')?.classList.contains('hidden')).toBe(true);
  });

  it('should show icon-labelled authenticated actions before appearance preferences', async () => {
    session.authenticated.set(true);
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();
    const root = fixture.nativeElement as HTMLElement;
    const nav = root.querySelector('header nav')!;
    expect(nav.textContent).toContain('Users');
    expect(nav.textContent).toContain('My profile');
    expect(nav.textContent).toContain('Sign out');
    expect(nav.querySelectorAll('app-icon').length).toBe(3);
    expect(nav.textContent).not.toContain('Complete your profile');
    expect(nav.querySelector('[href="/profile"]')).not.toBeNull();
    expect(root.querySelector('header a[aria-label="AspireBoot"]')?.getAttribute('href')).toBe(
      '/users',
    );
    expect(session.error()).toBeNull();
    expect(
      nav.compareDocumentPosition(root.querySelector('#locale')!) &
        Node.DOCUMENT_POSITION_FOLLOWING,
    ).not.toBe(0);
  });
});
