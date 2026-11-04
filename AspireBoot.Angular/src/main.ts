import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';

bootstrapApplication(App, appConfig).catch(() => {
  const root = document.querySelector('app-root');
  if (root)
    root.textContent =
      'Não foi possível iniciar a aplicação. Confira appsettings.json e recarregue. / Could not start the application. Check appsettings.json and reload.';
});
