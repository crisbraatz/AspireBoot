import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { SessionsService } from '../services/sessions.service';

export const sessionGuard: CanActivateFn = (_route, state) => {
  const sessionsService = inject(SessionsService);
  const router = inject(Router);

  const path = state.url.split('?')[0].replace(/\/$/, '') || '/';
  const publicRoutes = ['/', '/sign-in', '/sign-up'];
  const token = sessionsService.getToken();

  if (token && !sessionsService.isTokenExpired(token) && publicRoutes.includes(path)) {
    return router.createUrlTree(['/app/dashboard']);
  }

  if (!path.startsWith('/app'))
    return true;

  if (token && !sessionsService.isTokenExpired(token))
    return true;

  return sessionsService.refreshTokenIfNeeded().pipe(
    map(() => true),
    catchError(() => of(router.createUrlTree(
      ['/sign-in'],
      { queryParams: { returnUrl: state.url } }
    )))
  );

};
