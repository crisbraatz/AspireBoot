import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AppConfiguration } from '../../shared/configuration/app-configuration';
import { ListRequest } from '../../shared/http/list-request';
import { ListUserSessionsResponse } from './list-user-sessions/list-user-sessions-response';
import { DeleteUserRequest } from './delete-user/delete-user-request';
import { UpdateUserRequest } from './update-user/update-user-request';
import { GetUserResponse } from './get-user/get-user-response';
import { ListUsersResponse } from './list-users/list-users-response';

@Injectable({ providedIn: 'root' })
export class UsersApi {
  private readonly http = inject(HttpClient);
  private readonly configuration = inject(AppConfiguration);
  private get url(): string {
    return `${this.configuration.value.apiUrl.replace(/\/$/, '')}/users`;
  }

  getUser(id: string): Observable<GetUserResponse> {
    return this.http.get<GetUserResponse>(`${this.url}/${encodeURIComponent(id)}`);
  }
  listUsers(request: ListRequest): Observable<ListUsersResponse> {
    return this.http.get<ListUsersResponse>(this.url, {
      params: { page: request.page, size: request.size },
    });
  }
  updateUser(request: UpdateUserRequest): Observable<void> {
    const form = new FormData();
    if (request.newName !== undefined) form.set('newName', request.newName.trim());
    if (request.avatar) form.set('avatar', request.avatar, request.avatar.name);
    return this.http.patch<void>(`${this.url}/me`, form);
  }

  listUserSessions(id: string, request: ListRequest): Observable<ListUserSessionsResponse> {
    return this.http.get<ListUserSessionsResponse>(
      `${this.url}/${encodeURIComponent(id)}/sessions`,
      {
        params: { page: request.page, size: request.size },
      },
    );
  }

  deleteUser(request: DeleteUserRequest): Observable<void> {
    return this.http.delete<void>(`${this.url}/me`, { body: request });
  }
}
