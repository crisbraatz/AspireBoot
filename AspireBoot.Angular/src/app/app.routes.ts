import { Routes } from '@angular/router';
import { LandingComponent } from './pages/landing/landing.component';
import { SignInComponent } from './pages/signin/signin.component';
import { SignUpComponent } from './pages/signup/signup.component';
import { DashboardComponent } from './pages/app/dashboard/dashboard.component';
import { AuthGuardService } from './services/auth/auth.guard.service';

export const routes: Routes = [
    { path: '', component: LandingComponent, canActivate: [AuthGuardService] },
    { path: 'signin', component: SignInComponent, canActivate: [AuthGuardService] },
    { path: 'signup', component: SignUpComponent, canActivate: [AuthGuardService] },
    { path: 'app/dashboard', component: DashboardComponent, canActivate: [AuthGuardService] }
];
