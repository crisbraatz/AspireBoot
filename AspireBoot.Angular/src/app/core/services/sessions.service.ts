import { HttpClient, HttpHeaders } from "@angular/common/http";
import { inject, Injectable, signal } from "@angular/core";
import { Router } from "@angular/router";
import { catchError, finalize, map, Observable, of, shareReplay, throwError } from "rxjs";
import { CreateSessionRequest } from "../models/sessions/create-session-request.model";
import { RefreshSessionResponse } from "../models/sessions/refresh-session-response.model";
import { environment } from "../../../environments/environment";

@Injectable({ providedIn: 'root' })
export class SessionsService {
  private http = inject(HttpClient);
  private router = inject(Router);

  private readonly tokenState = signal<string | null>(localStorage.getItem('token'));
  private refreshRequest: Observable<string> | null = null;

  clearTokens(): void {
    localStorage.removeItem('token');
    this.tokenState.set(null);
    sessionStorage.clear();
  }

  getAuthHeaders(): HttpHeaders {
    return new HttpHeaders({ Authorization: `Bearer ${this.getToken()}` });
  }

  getToken(): string | null {
    return this.tokenState();
  }

  isTokenExpired(token: string): boolean {
    const payload = this.decodeToken(token);

    if (payload && typeof payload === 'object' && 'exp' in payload) {
      const exp = (payload as { exp: number }).exp;

      return !exp || exp < Math.floor(Date.now() / 1000);
    }

    return true;
  }

  redirectToSignIn(): void {
    this.router.navigate(['/sign-in']);
  }

  refreshToken(): Observable<RefreshSessionResponse> {
    return this.http.post<RefreshSessionResponse>(
      `${environment.baseUrl}${environment.endpoints.sessions.refresh}`,
      {},
      { withCredentials: true }
    );
  }

  refreshTokenIfNeeded(): Observable<string | null> {
    const token = this.getToken();
    if (token && !this.willTokenExpireSoon(token))
      return of(token);

    if (this.refreshRequest)
      return this.refreshRequest;

    this.refreshRequest = this.refreshToken().pipe(
      map((response) => {
        const newToken = response.token;
        if (!newToken)
          throw new Error('Token not found');

        this.storeToken(newToken);
        return newToken;
      }),
      catchError((error) => {
        this.clearTokens();

        return throwError(() => error);
      }),
      finalize(() => this.refreshRequest = null),
      shareReplay(1)
    );

    return this.refreshRequest;
  }

  signIn(data: CreateSessionRequest): Observable<RefreshSessionResponse> {
    this.clearTokens();

    return this.http.post<RefreshSessionResponse>(
      `${environment.baseUrl}${environment.endpoints.sessions.create}`,
      data,
      { withCredentials: true }
    );
  }

  signOut(): Observable<void> {
    const headers = this.getAuthHeaders();

    return this.http.delete<void>(
      `${environment.baseUrl}${environment.endpoints.sessions.delete}`,
      { headers, withCredentials: true }
    );
  }

  storeToken(token: string): void {
    localStorage.setItem('token', token);
    this.tokenState.set(token);
  }

  willTokenExpireSoon(token: string): boolean {
    const payload = this.decodeToken(token);

    if (payload && typeof payload === 'object' && 'exp' in payload) {
      const exp = (payload as { exp: number }).exp;

      return !exp || exp - Math.floor(Date.now() / 1000) < 60;
    }

    return true;
  }

  private decodeToken(token: string): unknown | null {
    try {
      return JSON.parse(atob(token.split('.')[1]));
    } catch {
      return null;
    }
  }

}
