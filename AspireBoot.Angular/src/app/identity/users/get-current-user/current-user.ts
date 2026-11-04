import { Injectable, effect, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { CurrentSession } from '../../sessions/get-current-session/current-session';
import { GetUserResponse } from '../get-user/get-user-response';
import { UsersApi } from '../users-api';

@Injectable({ providedIn: 'root' })
export class CurrentUser {
  private readonly api = inject(UsersApi);
  private readonly session = inject(CurrentSession);
  private loadedUserId: string | null = null;
  private loadedRevision: number | null = null;
  private loadVersion = 0;
  private pending: { userId: string; revision: number; promise: Promise<GetUserResponse> } | null =
    null;
  readonly user = signal<GetUserResponse | null>(null);

  constructor() {
    effect(() => {
      if (!this.session.authenticated()) {
        this.loadedUserId = null;
        this.loadedRevision = null;
        this.loadVersion++;
        this.pending = null;
        this.user.set(null);
      }
    });
  }

  async ensureLoaded(): Promise<GetUserResponse> {
    const userId = this.requireUserId();
    const revision = this.session.revision;
    if (this.pending?.userId === userId && this.pending.revision === revision)
      return this.pending.promise;
    const user = this.user();
    if (this.loadedUserId === userId && this.loadedRevision === revision && user) return user;
    this.user.set(null);
    return this.startLoad(userId, revision);
  }

  async reload(): Promise<GetUserResponse> {
    return this.startLoad(this.requireUserId(), this.session.revision);
  }

  private startLoad(userId: string, revision: number): Promise<GetUserResponse> {
    this.loadedUserId = null;
    this.loadedRevision = null;
    const promise = this.load(userId, revision).finally(() => {
      if (this.pending?.promise === promise) this.pending = null;
    });
    this.pending = { userId, revision, promise };
    return promise;
  }

  private requireUserId(): string {
    if (!this.session.authenticated() || !this.session.userId)
      throw new Error('Current profile unavailable.');
    return this.session.userId;
  }

  private async load(userId: string, revision: number): Promise<GetUserResponse> {
    const identityUserId = this.session.identityUserId;
    const version = ++this.loadVersion;
    const user = await firstValueFrom(this.api.getUser(userId));
    if (
      !this.session.authenticated() ||
      this.session.identityUserId !== identityUserId ||
      this.session.userId !== userId ||
      this.session.revision !== revision
    )
      throw new Error('Session changed.');
    if (version !== this.loadVersion) throw new Error('Profile request superseded.');
    this.user.set(user);
    this.loadedUserId = userId;
    this.loadedRevision = revision;
    return user;
  }
}
