import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { SessionsApi } from '../sessions-api';
import { GetCurrentSessionResponse } from './get-current-session-response';
import { safeReturnUrl } from '../../auth/return-url';

@Injectable({ providedIn: 'root' })
export class CurrentSession {
  private readonly api = inject(SessionsApi);
  private readonly router = inject(Router);
  private generation = 0;
  private initialization: Promise<boolean> | null = null;
  private identity: GetCurrentSessionResponse | null = null;
  readonly authenticated = signal(false);
  readonly busy = signal(false);
  readonly error = signal<'unavailable' | 'expired' | 'logoutFailed' | null>(null);

  get revision(): number {
    return this.generation;
  }

  get isAdministrator(): boolean {
    return this.identity?.isAdministrator === true;
  }
  get email(): string | undefined {
    return this.identity?.email;
  }
  get identityUserId(): string | undefined {
    return this.identity?.identityUserId;
  }

  get userId(): string | undefined {
    return this.identity?.userId;
  }

  async getCurrentSession(): Promise<boolean> {
    const generation = this.generation;
    this.initialization ??= firstValueFrom(this.api.getCurrentSession())
      .then((identity) => {
        if (generation !== this.generation) return false;
        this.identity = identity;
        this.authenticated.set(true);
        return true;
      })
      .catch((error: unknown) => {
        if (generation !== this.generation) return false;
        this.identity = null;
        this.authenticated.set(false);
        this.initialization = null;
        if (error instanceof HttpErrorResponse && error.status === 401) return false;
        throw error;
      });
    return this.initialization;
  }

  async createSession(email: string, password: string, rememberMe: boolean): Promise<void> {
    await firstValueFrom(this.api.createSession({ email, password, rememberMe }));
    this.generation++;
    this.identity = null;
    this.authenticated.set(false);
    this.initialization = null;
    this.error.set(null);
    await this.getCurrentSession();
  }

  async account(): Promise<void> {
    await this.router.navigateByUrl('/account');
  }

  invalidate(): void {
    this.generation++;
    this.identity = null;
    this.initialization = null;
    this.authenticated.set(false);
  }

  expire(): void {
    const authenticated = this.authenticated();
    this.invalidate();
    if (!authenticated) return;
    this.error.set('expired');
    void this.router.navigateByUrl('/auth/login', { replaceUrl: true });
  }

  async login(returnUrl = '/users', register = false): Promise<void> {
    this.error.set(null);
    await this.router.navigate([register ? '/auth/register' : '/auth/login'], {
      queryParams: { returnUrl: safeReturnUrl(returnUrl) },
    });
  }

  accountDeleted(): void {
    this.generation++;
    this.identity = null;
    this.initialization = null;
    this.authenticated.set(false);
    this.error.set(null);
    void this.router.navigateByUrl('/', { replaceUrl: true });
  }

  async deleteCurrentSession(): Promise<void> {
    if (this.busy()) return;
    this.busy.set(true);
    try {
      await firstValueFrom(this.api.deleteCurrentSession());
      this.generation++;
      this.identity = null;
      this.initialization = null;
      this.authenticated.set(false);
      this.error.set(null);
      await this.router.navigateByUrl('/');
    } catch {
      this.error.set('logoutFailed');
    } finally {
      this.busy.set(false);
    }
  }
}
