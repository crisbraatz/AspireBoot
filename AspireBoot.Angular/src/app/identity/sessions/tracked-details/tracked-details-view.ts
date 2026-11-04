import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { Localization } from '../../../shared/localization/localization';
import { ListUserSessionItemResponse } from '../../users/list-user-sessions/list-user-session-item-response';

@Component({
  selector: 'app-tracked-details',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="mb-3 space-y-1">
      <p class="font-medium">
        {{ known(trackedDetails()?.browser) }} <span class="text-muted">&middot;</span>
        {{ known(trackedDetails()?.operatingSystem) }}
      </p>
      <p class="text-xs text-muted">
        {{ device() }} <span aria-hidden="true">&middot;</span> {{ i18n.t('identity.ipAddress') }}:
        <span class="font-mono">{{
          trackedDetails()?.remoteIpAddress || i18n.t('identity.unavailable')
        }}</span>
      </p>
    </div>
  `,
})
export class TrackedDetailsView {
  readonly trackedDetails = input<Pick<
    ListUserSessionItemResponse,
    'browser' | 'operatingSystem' | 'device' | 'remoteIpAddress'
  > | null>();
  protected readonly i18n = inject(Localization);

  protected known(value?: string): string {
    return value && value !== 'Unknown' ? value : this.i18n.t('identity.unknownDevice');
  }

  protected device(): string {
    switch (this.trackedDetails()?.device) {
      case 'Desktop':
        return this.i18n.t('identity.deviceDesktop');
      case 'Mobile':
        return this.i18n.t('identity.deviceMobile');
      case 'Tablet':
        return this.i18n.t('identity.deviceTablet');
      default:
        return this.i18n.t('identity.unknownDevice');
    }
  }
}
