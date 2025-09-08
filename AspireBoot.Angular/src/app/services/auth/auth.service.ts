import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { finalize, map, Observable, of, ReplaySubject, throwError } from 'rxjs';
import { BaseResponse } from '../../models/base.response.model';
import { RefreshTokenResponse } from '../../models/refresh-token.response.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private isRefreshing = false;
  private refreshSubject = new ReplaySubject<string>(1);

  constructor(private http: HttpClient) {

  }

  getAuthHeaders(): HttpHeaders {
    const token = this.getToken();
    return new HttpHeaders({ Authorization: `Bearer ${token}`});
  }

  getToken(): string | null {
    return localStorage.getItem('authToken');
  }

  isTokenExpired(token: string): boolean {
    return JSON.parse(atob(token.split('.')[1])).exp < Math.floor(Date.now() / 1000);
  }

  refreshToken(): Observable<RefreshTokenResponse> {
    const headers = this.getAuthHeaders();
    return this.http.post<RefreshTokenResponse>(`${environment.baseUrl}${environment.endpoints.auth.refreshToken}`, {}, { headers, withCredentials: true });
  }

  refreshTokenIfNeeded(): Observable<string> {
    const token = this.getToken();
    if (!token) {
      return throwError(() => new Error('Auth token not found'));
    }

    if (!this.willTokenExpireSoon(token)) {
      return of(token);
    }

    if (this.isRefreshing) {
      return this.refreshSubject.asObservable();
    }

    this.isRefreshing = true;
    return this.refreshToken().pipe(
      map((response) => {
        const newToken = response.data?.authToken!;
        if (!newToken) {
          throw new Error('Auth token not found');
        }
        this.setToken(newToken);
        this.refreshSubject.next(newToken);
        return newToken;
      }),
      finalize(() => {
        this.isRefreshing = false;
      })
    );
  }

  removeToken(): void {
    localStorage.removeItem('authToken');
    localStorage.removeItem('refreshToken');
    sessionStorage.clear();
    document.cookie = 'authToken=; Max-Age=0; path=/;';
    document.cookie = 'refreshToken=; Max-Age=0; path=/;';
  }

  setToken(token: string): void {
    localStorage.setItem('authToken', token);
  }

  signIn(data: { email: string; password: string }): Observable<RefreshTokenResponse> {
    return this.http.post<RefreshTokenResponse>(`${environment.baseUrl}${environment.endpoints.auth.signIn}`, data, { withCredentials: true });
  }

  signOut(): Observable<BaseResponse> {
    const headers = this.getAuthHeaders();
    return this.http.post<BaseResponse>(`${environment.baseUrl}${environment.endpoints.auth.signOut}`, {}, { headers, withCredentials: true });
  }

  signUp(data: { email: string; password: string }): Observable<RefreshTokenResponse> {
    return this.http.post<RefreshTokenResponse>(`${environment.baseUrl}${environment.endpoints.auth.signUp}`, data, { withCredentials: true });
  }

  test(): Observable<BaseResponse<string>> {
    const headers = this.getAuthHeaders();
    return this.http.post<BaseResponse<string>>(`${environment.baseUrl}${environment.endpoints.auth.test}`, {}, { headers, withCredentials: true });
  }

  willTokenExpireSoon(token: string): boolean {
    return JSON.parse(atob(token.split('.')[1])).exp - Math.floor(Date.now() / 1000) < 60;
  }

}
