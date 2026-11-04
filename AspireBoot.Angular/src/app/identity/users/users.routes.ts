import { Routes } from '@angular/router';

export const usersRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./list-users/list-users.page').then((module) => module.ListUsersPage),
  },
  {
    path: ':id',
    loadComponent: () => import('./get-user/get-user.page').then((module) => module.GetUserPage),
  },
];
