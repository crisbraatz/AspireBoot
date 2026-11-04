import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, from, switchMap, throwError } from 'rxjs';
import { AppConfiguration } from '../../shared/configuration/app-configuration';
import { Antiforgery } from './get-antiforgery/antiforgery';
import { CurrentSession } from '../sessions/get-current-session/current-session';
import { problemCode } from '../../shared/http/api-problem';

export function isApiRequest(requestUrl: string, apiUrl: string, origin: string): boolean {
  const request = new URL(requestUrl, origin);
  const api = new URL(apiUrl);
  const prefix = api.pathname.replace(/\/$/, '');
  return (
    request.origin === api.origin &&
    (request.pathname === prefix || request.pathname.startsWith(`${prefix}/`))
  );
}

export const authenticationInterceptor: HttpInterceptorFn = (request, next) => {
  const configuration = inject(AppConfiguration);
  if (!isApiRequest(request.url, configuration.value.apiUrl, window.location.origin))
    return next(request);
  const session = inject(CurrentSession);
  const antiforgery = inject(Antiforgery);
  const secured = request.clone({ withCredentials: true });
  const response = ['GET', 'HEAD', 'OPTIONS'].includes(request.method)
    ? next(secured)
    : from(antiforgery.getToken()).pipe(
        switchMap((token) => next(secured.clone({ setHeaders: { 'X-CSRF-TOKEN': token } }))),
      );
  return response.pipe(
    catchError((error: unknown) => {
      if (
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        !problemCode(error, 'identity.auth.invalid-credentials')
      )
        session.expire();
      return throwError(() => error);
    }),
  );
};
