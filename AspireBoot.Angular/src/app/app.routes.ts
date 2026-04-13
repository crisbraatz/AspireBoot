import { Routes } from '@angular/router';
import { sessionGuard } from './core/guards/sessions.guard';
import { LandingComponent } from './pages/landing/landing.component';

export const routes: Routes = [
  { path: '', component: LandingComponent, canActivate: [sessionGuard] },
  {
    path: 'sign-in',
    loadComponent: () => import('./pages/signin/sign-in.component').then((m) => m.SignInComponent),
    canActivate: [sessionGuard]
  },
  {
    path: 'sign-up',
    loadComponent: () => import('./pages/signup/sign-up.component').then((m) => m.SignUpComponent),
    canActivate: [sessionGuard]
  },
  {
    path: 'app/dashboard',
    loadComponent: () => import('./pages/app/dashboard/dashboard.component').then((m) => m.DashboardComponent),
    canActivate: [sessionGuard]
  },
  { path: '**', redirectTo: '' }
];
