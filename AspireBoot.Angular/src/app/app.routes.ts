import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { DashboardComponent } from './pages/app/dashboard/dashboard.component';
import { LandingComponent } from './pages/landing/landing.component';
import { SignInComponent } from './pages/signin/sign-in.component';
import { SignUpComponent } from './pages/signup/sign-up.component';

export const routes: Routes = [
  { path: '', component: LandingComponent, canActivate: [authGuard] },
  { path: 'sign-in', component: SignInComponent, canActivate: [authGuard] },
  { path: 'sign-up', component: SignUpComponent, canActivate: [authGuard] },
  { path: 'app/dashboard', component: DashboardComponent, canActivate: [authGuard] }
];
