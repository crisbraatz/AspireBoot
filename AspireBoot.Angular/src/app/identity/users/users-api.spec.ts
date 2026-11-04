import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AppConfiguration } from '../../shared/configuration/app-configuration';
import { GetUserResponse } from './get-user/get-user-response';
import { UsersApi } from './users-api';

describe('UsersApi', () => {
  let http: HttpTestingController;
  let api: UsersApi;
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: AppConfiguration,
          useValue: { value: { apiUrl: 'https://api.example.com/api' } },
        },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    api = TestBed.inject(UsersApi);
  });
  afterEach(() => http.verify());

  it('should read a profile using its local user ID', () => {
    const user = { id: 'local-id', name: 'User Name', email: 'user@example.com' };
    let result: GetUserResponse | undefined;
    api.getUser('local-id').subscribe((value) => {
      result = value;
    });
    const request = http.expectOne('https://api.example.com/api/users/local-id');
    expect(request.request.method).toBe('GET');
    expect(request.request.body).toBeNull();
    request.flush(user);
    expect(result).toEqual(user);
  });
  it('should bind page and size using the backend contract', () => {
    api.listUsers({ page: 2, size: 20 }).subscribe();
    const request = http.expectOne('https://api.example.com/api/users?page=2&size=20');
    expect(request.request.method).toBe('GET');
    request.flush({ items: [], page: 2, size: 20, count: 0 });
  });
  it('should send profile edits as multipart without choosing another user', () => {
    const file = new File(['image'], 'avatar.png', { type: 'image/png' });
    api.updateUser({ newName: ' User Name ', avatar: file }).subscribe();
    const request = http.expectOne('https://api.example.com/api/users/me');
    expect(request.request.method).toBe('PATCH');
    const body = request.request.body as FormData;
    expect(body.get('newName')).toBe('User Name');
    expect(body.get('avatar')).toEqual(file);
    expect(body.has('id')).toBe(false);
    expect(request.request.headers.has('Content-Type')).toBe(false);
    request.flush(null);
  });

  it('should list user sessions with the flat API response and pagination', () => {
    const body = {
      items: [
        {
          id: 'session',
          createdAtUtc: '2026-10-28T00:00:00Z',
          expiresAtUtc: '2026-10-28T08:00:00Z',
          browser: 'Firefox',
          operatingSystem: 'Linux',
          device: 'Desktop',
          isCurrent: true,
          remoteIpAddress: '203.0.113.7',
        },
      ],
      page: 2,
      size: 20,
      count: 21,
    };
    let response: unknown;
    api
      .listUserSessions('local-id', { page: 2, size: 20 })
      .subscribe((value) => (response = value));
    const request = http.expectOne(
      'https://api.example.com/api/users/local-id/sessions?page=2&size=20',
    );
    expect(request.request.method).toBe('GET');
    request.flush(body);
    expect(response).toEqual(body);
  });

  it('should delete only the current user using the password request', () => {
    api.deleteUser({ password: 'Test-Password123!' }).subscribe();
    const request = http.expectOne('https://api.example.com/api/users/me');
    expect(request.request.method).toBe('DELETE');
    expect(request.request.body).toEqual({ password: 'Test-Password123!' });
    request.flush(null);
  });
});
