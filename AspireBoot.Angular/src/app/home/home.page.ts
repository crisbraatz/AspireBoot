import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Localization } from '../shared/localization/localization';

@Component({
  host: { class: 'flex flex-1 flex-col' },
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './home.page.html',
})
export class HomePage {
  protected readonly i18n = inject(Localization);
}
