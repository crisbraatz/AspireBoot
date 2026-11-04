import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Localization } from '../localization/localization';
import { CurrentSession } from '../../identity/sessions/get-current-session/current-session';

@Component({
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<section class="container-page py-24 text-center">
    <p class="eyebrow">404</p>
    <h1 class="page-title mt-5">{{ i18n.t('error.notFoundTitle') }}</h1>
    <p class="mt-5 text-muted">{{ i18n.t('error.notFoundDescription') }}</p>
    <a [routerLink]="session.authenticated() ? '/users' : '/'" class="btn btn-primary mt-8">{{
      i18n.t('common.back')
    }}</a>
  </section>`,
})
export class NotFoundPage {
  protected readonly i18n = inject(Localization);
  protected readonly session = inject(CurrentSession);
}
