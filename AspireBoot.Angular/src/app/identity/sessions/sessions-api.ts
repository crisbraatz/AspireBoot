import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AppConfiguration } from '../../shared/configuration/app-configuration';
import { CreateSessionRequest } from './create-session/create-session-request';
import { GetCurrentSessionResponse } from './get-current-session/get-current-session-response';

@Injectable({ providedIn: 'root' })
export class SessionsApi {
  private readonly http = inject(HttpClient);
  private readonly configuration = inject(AppConfiguration);

  createSession(request: CreateSessionRequest): Observable<void> {
    return this.http.post<void>(this.configuration.value.apiUrl + '/sessions', request);
  }

  getCurrentSession(): Observable<GetCurrentSessionResponse> {
    return this.http.get<GetCurrentSessionResponse>(
      this.configuration.value.apiUrl + '/sessions/current',
    );
  }

  deleteCurrentSession(): Observable<void> {
    return this.http.delete<void>(this.configuration.value.apiUrl + '/sessions/current');
  }

  deleteSession(id: string): Observable<void> {
    return this.http.delete<void>(
      this.configuration.value.apiUrl + '/sessions/' + encodeURIComponent(id),
    );
  }

  deleteOtherSessions(): Observable<void> {
    return this.http.delete<void>(this.configuration.value.apiUrl + '/sessions/others');
  }
}
