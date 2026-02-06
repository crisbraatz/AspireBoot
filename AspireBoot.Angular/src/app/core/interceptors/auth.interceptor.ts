import { HttpInterceptorFn } from "@angular/common/http";
import { inject } from "@angular/core";
import { catchError, switchMap, throwError } from "rxjs";
import { AuthService } from "../services/auth.service";

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  const authEndpoints = ['/auth/refresh-token', '/auth/sign-in', '/auth/sign-out', '/auth/sign-up'];
  const isAuthRequest = authEndpoints.some(endpoint => req.url.includes(endpoint));

  if (isAuthRequest) {
    return next(req).pipe(
      catchError(error => {
        if (req.url.includes('/auth/refresh-token') && error.status === 401) {
          authService.clearTokens();
          authService.redirectToSignIn();
        }

        return throwError(() => error);
      })
    );
  }

  return authService.refreshTokenIfNeeded().pipe(
    switchMap(token => {
      const authReq = token
        ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
        : req;

      return next(authReq);
    }),
    catchError(error => {
      if (error.status === 401) {
        authService.clearTokens();
        authService.redirectToSignIn();
      }

      return throwError(() => error);
    })
  );

}
