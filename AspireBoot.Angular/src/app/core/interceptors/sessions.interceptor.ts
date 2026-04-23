import { HttpInterceptorFn } from "@angular/common/http";
import { inject } from "@angular/core";
import { catchError, switchMap, throwError } from "rxjs";
import { SessionsService } from "../services/sessions.service";

export const sessionInterceptor: HttpInterceptorFn = (req, next) => {
  const sessionsService = inject(SessionsService);

  const isAnonymousRequest = req.url.includes('/sessions') || (req.method === 'POST' && req.url.endsWith('/users'));

  if (isAnonymousRequest)
    return next(req).pipe(
      catchError(error => {
        if (req.url.includes('/sessions/refresh') && error.status === 401) {
          sessionsService.clearTokens();
          sessionsService.redirectToSignIn();
        }

        return throwError(() => error);
      })
    );

  return sessionsService.refreshTokenIfNeeded().pipe(
    switchMap(token => next(token
      ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
      : req)
    ),
    catchError(error => {
      if (error.status === 401) {
        sessionsService.clearTokens();
        sessionsService.redirectToSignIn();
      }

      return throwError(() => error);
    })
  );

}
