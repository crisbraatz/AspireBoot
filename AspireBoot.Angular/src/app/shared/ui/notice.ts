import { ChangeDetectionStrategy, Component, effect, inject, input, signal } from '@angular/core';
import { Localization } from '../localization/localization';
import { Icon } from './icon';

@Component({
  selector: 'app-notice',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (!dismissed()) {
      <section class="panel relative p-5" [class.pr-16]="dismissible()" [attr.role]="announce()">
        @if (dismissible()) {
          <button
            type="button"
            class="icon-button absolute right-3 top-3"
            [attr.aria-label]="i18n.t('common.dismiss')"
            (click)="dismissed.set(true)"
          >
            <app-icon name="close" />
          </button>
        }
        <ng-content />
      </section>
    }
  `,
})
export class Notice {
  readonly notification = input<unknown>(null);
  readonly announce = input<'alert' | 'status'>('status');
  readonly dismissible = input(true);
  protected readonly dismissed = signal(false);
  protected readonly i18n = inject(Localization);

  constructor() {
    effect(() => {
      this.notification();
      this.dismissed.set(false);
    });
  }
}
