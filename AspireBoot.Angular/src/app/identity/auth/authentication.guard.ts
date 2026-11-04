import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { CurrentUser } from '../users/get-current-user/current-user';
import { CurrentSession } from '../sessions/get-current-session/current-session';

export const guestGuard: CanActivateFn = (route) => {
  if (!inject(CurrentSession).authenticated()) return true;
  const router = inject(Router);
  const mode = route.data?.['mode'];
  return ['confirm-email', 'confirm-email-change'].includes(mode)
    ? router.createUrlTree(['/account', mode], { queryParams: route.queryParams })
    : router.parseUrl('/users');
};

export const authenticationGuard: CanActivateFn = async (_, state) => {
  const session = inject(CurrentSession);
  const router = inject(Router);
  try {
    if (await session.getCurrentSession()) return true;
    return router.createUrlTree(['/auth/login'], { queryParams: { returnUrl: state.url } });
  } catch {
    session.error.set('unavailable');
  }
  return false;
};

export const profileGuard: CanActivateFn = async () => {
  const current = inject(CurrentUser);
  const session = inject(CurrentSession);
  try {
    if (await current.ensureLoaded()) return true;
  } catch {
    // Missing or unavailable profiles are account errors, not a provisioning flow.
  }
  session.error.set('unavailable');
  return false;
};
