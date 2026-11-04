import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Observable, Subject, of, throwError } from 'rxjs';
import { CurrentSession } from '../../sessions/get-current-session/current-session';
import { CurrentUser } from './current-user';
import { GetUserResponse } from '../get-user/get-user-response';
import { UsersApi } from '../users-api';

describe('current user', () => {
  const user: GetUserResponse = {
    id: 'local-user',
    name: 'Local Name',
    isActive: true,
    isAdministrator: false,
    email: 'user@example.com',
  };
  let session: {
    authenticated: ReturnType<typeof signal<boolean>>;
    email: string;
    identityUserId: string;
    userId: string | undefined;
    revision: number;
  };
  let api: { getUser: ReturnType<typeof vi.fn> };
  let current: CurrentUser;
  beforeEach(() => {
    session = {
      authenticated: signal(true),
      email: user.email!,
      identityUserId: 'opaque-subject',
      revision: 0,
      userId: user.id,
    };
    api = { getUser: vi.fn<() => Observable<GetUserResponse>>().mockReturnValue(of(user)) };
    TestBed.configureTestingModule({
      providers: [
        { provide: CurrentSession, useValue: session },
        { provide: UsersApi, useValue: api },
      ],
    });
    current = TestBed.inject(CurrentUser);
  });

  it('should load the profile once by the local user ID supplied by the session', async () => {
    expect(await current.ensureLoaded()).toEqual(user);
    expect(await current.ensureLoaded()).toEqual(user);
    expect(current.user()).toEqual(user);
    expect(api.getUser).toHaveBeenCalledExactlyOnceWith('local-user');
  });

  it('should share concurrent profile requests', async () => {
    const response = new Subject<GetUserResponse>();
    api.getUser.mockReturnValue(response);
    const first = current.ensureLoaded();
    const second = current.ensureLoaded();
    response.next(user);
    response.complete();
    expect(await first).toEqual(user);
    expect(await second).toEqual(user);
    expect(api.getUser).toHaveBeenCalledOnce();
  });

  it('should reload the profile after editing without provisioning another one', async () => {
    await current.ensureLoaded();
    const updated = {
      ...user,
      name: 'Updated Name',
      avatarUrl: 'https://storage.example.com/avatar',
    };
    api.getUser.mockReturnValue(of(updated));
    expect(await current.reload()).toEqual(updated);
    expect(current.user()).toEqual(updated);
    expect(api.getUser).toHaveBeenCalledTimes(2);
  });

  it('should keep the backend email rather than overwrite it from the session snapshot', async () => {
    session.email = 'stale@example.com';
    expect(await current.ensureLoaded()).toEqual(user);
  });

  it('should let a profile reload supersede an older pending request', async () => {
    const response = new Subject<GetUserResponse>();
    const updated = { ...user, name: 'Latest Name' };
    api.getUser.mockReturnValueOnce(response).mockReturnValueOnce(of(updated));
    const pending = current.ensureLoaded();
    expect(await current.reload()).toEqual(updated);

    response.next(user);
    response.complete();

    await expect(pending).rejects.toThrow('Profile request superseded.');
    expect(current.user()).toEqual(updated);
  });

  it('should share an in-progress reload instead of returning the cached profile', async () => {
    await current.ensureLoaded();
    const response = new Subject<GetUserResponse>();
    const updated = { ...user, name: 'Latest Name' };
    api.getUser.mockReturnValue(response);
    const reload = current.reload();

    const concurrent = current.ensureLoaded();
    response.next(updated);
    response.complete();

    expect(await reload).toEqual(updated);
    expect(await concurrent).toEqual(updated);
    expect(api.getUser).toHaveBeenCalledTimes(2);
  });

  it('should reload cached data after signing into the same account again', async () => {
    await current.ensureLoaded();
    const updated = { ...user, name: 'Latest Name' };
    api.getUser.mockReturnValue(of(updated));

    session.revision++;
    const result = await current.ensureLoaded();

    expect(result).toEqual(updated);
    expect(api.getUser).toHaveBeenCalledTimes(2);
  });

  it('should not substitute the identity subject for a missing profile ID', async () => {
    session.userId = undefined;
    await expect(current.ensureLoaded()).rejects.toThrow('Current profile unavailable.');
    expect(api.getUser).not.toHaveBeenCalled();
  });

  it.each([403, 404, 429, 503])(
    'should report a %s failure without starting onboarding',
    async (status) => {
      const error = new HttpErrorResponse({ status, error: { code: 'identity.users.not-found' } });
      api.getUser.mockReturnValue(throwError(() => error));
      await expect(current.ensureLoaded()).rejects.toBe(error);
      expect(current.user()).toBeNull();
      // A failed request must not poison subsequent retries.
      api.getUser.mockReturnValue(of(user));
      expect(await current.ensureLoaded()).toEqual(user);
    },
  );

  it('should not restore personal data after sign-out', async () => {
    const response = new Subject<GetUserResponse>();
    api.getUser.mockReturnValue(response);
    const pending = current.ensureLoaded();
    session.authenticated.set(false);
    TestBed.tick();
    response.next(user);
    response.complete();
    await expect(pending).rejects.toThrow('Session changed.');
    expect(current.user()).toBeNull();
  });

  it('should discard an old response after switching accounts and fetch the new profile', async () => {
    const response = new Subject<GetUserResponse>();
    api.getUser.mockReturnValueOnce(response);
    const old = current.ensureLoaded();
    session.identityUserId = 'another-subject';
    session.userId = 'another-profile';
    const another = { id: 'another-profile', name: 'Another User' };
    api.getUser.mockReturnValue(of(another));
    expect(await current.ensureLoaded()).toEqual(another);
    response.next(user);
    response.complete();
    await expect(old).rejects.toThrow('Session changed.');
    expect(current.user()).toEqual(another);
    expect(api.getUser).toHaveBeenLastCalledWith('another-profile');
  });

  it('should discard a pending response when the same account starts a new session', async () => {
    const response = new Subject<GetUserResponse>();
    api.getUser.mockReturnValue(response);
    const pending = current.ensureLoaded();

    session.authenticated.set(false);
    session.revision++;
    session.authenticated.set(true);
    response.next(user);
    response.complete();

    await expect(pending).rejects.toThrow('Session changed.');
    expect(current.user()).toBeNull();
  });
});
