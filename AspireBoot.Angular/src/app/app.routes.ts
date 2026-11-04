import { Routes } from '@angular/router';
import {
  authenticationGuard,
  guestGuard,
  profileGuard,
} from './identity/auth/authentication.guard';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    canActivate: [guestGuard],
    loadComponent: () => import('./home/home.page').then((module) => module.HomePage),
  },
  ...[
    'login',
    'register',
    'forgot-password',
    'reset-password',
    'confirm-email',
    'confirm-email-change',
    'resend-confirmation',
  ].map((mode) => ({
    path: 'auth/' + mode,
    canActivate: [guestGuard],
    data: { mode },
    loadComponent: () =>
      import('./identity/auth/account-access/account-access.page').then(
        (module) => module.AccountAccessPage,
      ),
  })),
  ...['confirm-email', 'confirm-email-change'].map((mode) => ({
    path: 'account/' + mode,
    data: { mode },
    loadComponent: () =>
      import('./identity/auth/account-access/account-access.page').then(
        (module) => module.AccountAccessPage,
      ),
  })),
  {
    path: '',
    canActivate: [authenticationGuard, profileGuard],
    children: [
      {
        path: 'account',
        loadComponent: () =>
          import('./identity/auth/account-security/account-security.page').then(
            (module) => module.AccountSecurityPage,
          ),
      },
      {
        path: 'users',
        loadChildren: () =>
          import('./identity/users/users.routes').then((module) => module.usersRoutes),
      },
      {
        path: 'profile/edit',
        loadComponent: () =>
          import('./identity/users/update-user/update-user.page').then(
            (module) => module.UpdateUserPage,
          ),
      },
      {
        path: 'profile',
        loadComponent: () =>
          import('./identity/users/get-user/get-user.page').then((module) => module.GetUserPage),
      },
    ],
  },
  {
    path: '**',
    loadComponent: () => import('./shared/ui/not-found.page').then((module) => module.NotFoundPage),
  },
];
