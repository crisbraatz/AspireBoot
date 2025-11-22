import { HttpClient, HttpHeaders } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { Router } from "@angular/router";
import { finalize, map, Observable, of, ReplaySubject, throwError } from "rxjs";
import { BaseResponse } from "../models/base-response.model";
import { RefreshTokenResponse } from "../models/tokens/refresh-token-response.model";
import { SignInRequest } from "../models/users/sign-in-user-request.model";
import { SignUpRequest } from "../models/users/sign-up-user-request.model";
import { environment } from "../../../environments/environment";

@Injectable({ providedIn: 'root' })
export class AuthService{
  private http = inject(HttpClient);
  private router = inject(Router);

  private isRefreshing = false;
  private refreshSubject = new ReplaySubject<string>(1);

  clearTokens(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
    sessionStorage.clear();
  }

  getAuthHeaders(): HttpHeaders {
    return new HttpHeaders({ Authorization: `Bearer ${this.getToken()}`});
  }

  getToken(): string | null {
    return localStorage.getItem('token');
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

  refreshToken(): Observable<BaseResponse<RefreshTokenResponse>> {
    const headers = this.getAuthHeaders();

    return this.http.post<BaseResponse<RefreshTokenResponse>>(
      `${environment.baseUrl}${environment.endpoints.auth.refreshToken}`,
      {},
      { headers, withCredentials: true }
    );
  }

  refreshTokenIfNeeded(): Observable<string | null> {
    const token = this.getToken();
    if (!token) {
      return throwError(() => new Error('Token not found'));
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
        const newToken = response.data?.token;
        if (!newToken) {
          throw new Error('Token not found');
        }

        this.storeToken(newToken);
        this.refreshSubject.next(newToken);
        this.refreshSubject.complete();
        this.refreshSubject = new ReplaySubject<string>(1);

        return newToken;
      }),
      finalize(() => {
        this.isRefreshing = false;
      })
    );
  }

  signIn(data: SignInRequest): Observable<BaseResponse<RefreshTokenResponse>> {
    this.clearTokens();

    return this.http.post<BaseResponse<RefreshTokenResponse>>(
      `${environment.baseUrl}${environment.endpoints.auth.signIn}`,
      data,
      { withCredentials: true }
    );
  }

  signOut(): Observable<BaseResponse> {
    const headers = this.getAuthHeaders();

    return this.http.post<BaseResponse>(
      `${environment.baseUrl}${environment.endpoints.auth.signOut}`,
      {},
      { headers, withCredentials: true }
    );
  }

  signUp(data: SignUpRequest): Observable<BaseResponse<RefreshTokenResponse>> {
    this.clearTokens();

    return this.http.post<BaseResponse<RefreshTokenResponse>>(
      `${environment.baseUrl}${environment.endpoints.auth.signUp}`,
      data, 
      { withCredentials: true }
    );
  }

  storeToken(token: string): void {
    localStorage.setItem('token', token);
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
