import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Localization } from '../localization/localization';

@Component({
  selector: 'app-loading-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<div
    class="flex min-h-48 items-center justify-center gap-3 text-sm text-muted"
    role="status"
  >
    <span
      class="size-4 rounded-full border-2 border-line border-t-accent motion-safe:animate-spin"
      aria-hidden="true"
    ></span
    >{{ i18n.t('common.loading') }}
  </div>`,
})
export class LoadingState {
  protected readonly i18n = inject(Localization);
}
