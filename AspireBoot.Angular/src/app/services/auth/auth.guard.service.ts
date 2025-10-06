import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, Router, RouterStateSnapshot } from '@angular/router';
import { Observable, of } from 'rxjs';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class AuthGuardService implements CanActivate {
  constructor(private authService: AuthService, private router: Router) {

  }

  canActivate(_route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<boolean> {
    const path = state.url.split('?')[0].replace(/\/$/, '') || '/';
    const publicRoutes = ['/', '/signin', '/signup'];
    const token = this.authService.getToken();

    if (token && !this.authService.isTokenExpired(token) && publicRoutes.includes(path)) {
      this.router.navigate(['/app/dashboard']);
      return of(false);
    }

    if ((!token || this.authService.isTokenExpired(token)) && path.startsWith('/app')) {
      this.router.navigate(['/signin'], { queryParams: { returnUrl: state.url } });
      return of(false);
    }

    return of(true);
  }

}
