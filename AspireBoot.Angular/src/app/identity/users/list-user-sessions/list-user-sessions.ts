import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { EMPTY, catchError, combineLatest, finalize, switchMap } from 'rxjs';
import { UsersApi } from '../users-api';
import { Localization } from '../../../shared/localization/localization';
import { ErrorNotice } from '../../../shared/ui/error-notice';
import { Pagination } from '../../../shared/ui/pagination';

import { ListUserSessionItemResponse } from './list-user-session-item-response';
import { TrackedDetailsView } from '../../sessions/tracked-details/tracked-details-view';

@Component({
  selector: 'app-list-user-sessions',
  imports: [ErrorNotice, Pagination, TrackedDetailsView],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="panel mt-8 overflow-hidden">
      <div class="p-6">
        <div class="flex flex-wrap items-center justify-between gap-4">
          <h2 class="text-xl font-semibold">{{ i18n.t('identity.sessions') }}</h2>
          @if (!loading() && !error()) {
            <span class="rounded-full border border-line px-3 py-1.5 text-xs text-muted">{{
              i18n.t('identity.sessionsTotal', { count: count() })
            }}</span>
          }
        </div>
        <app-error-notice [error]="error()" />
        @if (loading()) {
          <p role="status" class="mt-4 text-muted">{{ i18n.t('common.loading') }}</p>
        }
        <ul class="mt-4 divide-y divide-line">
          @for (item of items(); track item.id) {
            <li class="py-4 text-sm">
              @if (item.isCurrent) {
                <p class="font-semibold text-accent">{{ i18n.t('identity.currentSession') }}</p>
              }
              <app-tracked-details [trackedDetails]="item" />
              <p class="max-w-full truncate font-mono text-xs text-muted">{{ item.id }}</p>
              <p>
                {{ i18n.t('identity.sessionCreated') }}: {{ i18n.formatDate(item.createdAtUtc) }}
              </p>
              <p>
                {{ i18n.t('identity.sessionExpires') }}: {{ i18n.formatDate(item.expiresAtUtc) }}
              </p>
            </li>
          }
        </ul>
        @if (!loading() && !error() && !items().length) {
          <p class="py-6 text-sm text-muted">{{ i18n.t('identity.sessionsEmpty') }}</p>
        }
      </div>
      <app-pagination
        [page]="pagination().page"
        [size]="pagination().size"
        [count]="count()"
        [disabled]="loading()"
        (pageChange)="pagination.set($event)"
      />
    </section>
  `,
})
export class ListUserSessions {
  readonly userId = input.required<string>();
  private readonly api = inject(UsersApi);
  protected readonly i18n = inject(Localization);
  protected readonly error = signal<unknown>(null);
  protected readonly loading = signal(true);
  protected readonly pagination = signal({ page: 1, size: 10 });
  protected readonly count = signal(0);
  protected readonly items = signal<readonly ListUserSessionItemResponse[]>([]);

  constructor() {
    combineLatest([toObservable(this.userId), toObservable(this.pagination)])
      .pipe(
        switchMap(([id, pagination]) => {
          this.loading.set(true);
          this.error.set(null);
          this.items.set([]);
          return this.api.listUserSessions(id, pagination).pipe(
            catchError((error) => {
              this.error.set(error);
              return EMPTY;
            }),
            finalize(() => this.loading.set(false)),
          );
        }),
        takeUntilDestroyed(),
      )
      .subscribe((result) => {
        this.items.set(result.items);
        this.count.set(result.count);
      });
  }
}
