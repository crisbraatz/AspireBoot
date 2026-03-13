import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { LandingComponent } from './pages/landing/landing.component';

export const routes: Routes = [
  { path: '', component: LandingComponent, canActivate: [authGuard] },
  {
    path: 'sign-in',
    loadComponent: () => import('./pages/signin/sign-in.component').then((m) => m.SignInComponent),
    canActivate: [authGuard]
  },
  {
    path: 'sign-up',
    loadComponent: () => import('./pages/signup/sign-up.component').then((m) => m.SignUpComponent),
    canActivate: [authGuard]
  },
  {
    path: 'app/dashboard',
    loadComponent: () => import('./pages/app/dashboard/dashboard.component').then((m) => m.DashboardComponent),
    canActivate: [authGuard]
  }
];
