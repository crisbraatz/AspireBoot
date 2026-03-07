import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { of } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (_route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const path = state.url.split('?')[0].replace(/\/$/, '') || '/';
  const publicRoutes = ['/', '/sign-in', '/sign-up'];
  const token = authService.getToken();

  if (token && !authService.isTokenExpired(token) && publicRoutes.includes(path)) {
    router.navigate(['/app/dashboard']);

    return of(false);
  }

  if ((!token || authService.isTokenExpired(token)) && path.startsWith('/app')) {
    router.navigate(['/sign-in'], { queryParams: { returnUrl: state.url } });

    return of(false);
  }

  return of(true);

};
