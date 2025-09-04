import { inject } from '@angular/core';
import { throwError } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';
import { AuthService } from './auth.service';
import { HttpInterceptorFn } from '@angular/common/http';

export const authInterceptorFn: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const isAuthRequest = req.url.includes('/auth/refresh-token') || req.url.includes('/auth/signin') || req.url.includes('/auth/signout') || req.url.includes('/auth/signup');
  
  if (isAuthRequest) {
    return next(req).pipe(
      catchError((error) => {
        if (req.url.includes('/auth/refresh-token') && error.status === 401) {
          authService.removeToken();
          window.location.href = '/signin';
        }
        return throwError(() => error);
      })
    );
  }

  return authService.refreshTokenIfNeeded().pipe(
    switchMap((token) => {
      const cloned = token
        ? req.clone({ headers: req.headers.set('Authorization', `Bearer ${token}`) })
        : req;
      return next(cloned);
    }),
    catchError((error) => {
      authService.removeToken();
      return throwError(() => error);
    })
  );

};
