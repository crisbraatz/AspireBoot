import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter, withInMemoryScrolling } from '@angular/router';
import { routes } from './app.routes';
import { AppConfiguration } from './shared/configuration/app-configuration';
import { CurrentSession } from './identity/sessions/get-current-session/current-session';
import { authenticationInterceptor } from './identity/auth/authentication.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideAppInitializer(() => {
      const configuration = inject(AppConfiguration);
      const session = inject(CurrentSession);
      return configuration.load().then(() => session.getCurrentSession().catch(() => false));
    }),
    provideHttpClient(withInterceptors([authenticationInterceptor])),
    provideRouter(routes, withInMemoryScrolling({ scrollPositionRestoration: 'enabled' })),
  ],
};
