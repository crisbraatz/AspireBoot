import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { EMPTY, Subject, catchError, combineLatest, finalize, startWith, switchMap } from 'rxjs';
import { Localization } from '../../../shared/localization/localization';
import { ErrorNotice } from '../../../shared/ui/error-notice';
import { LoadingState } from '../../../shared/ui/loading-state';
import { Pagination } from '../../../shared/ui/pagination';
import { CurrentUser } from '../get-current-user/current-user';
import { ListUsersResponse } from './list-users-response';
import { UserAvatar } from '../user-avatar/user-avatar';
import { UsersApi } from '../users-api';

@Component({
  imports: [ErrorNotice, LoadingState, Pagination, RouterLink, UserAvatar],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './list-users.page.html',
})
export class ListUsersPage {
  private readonly api = inject(UsersApi);
  private readonly destroy = inject(DestroyRef);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly reload = new Subject<void>();
  protected readonly current = inject(CurrentUser);
  protected readonly i18n = inject(Localization);
  protected readonly error = signal<unknown>(null);
  protected readonly loading = signal(true);
  protected readonly result = signal<ListUsersResponse | null>(null);

  constructor() {
    combineLatest([this.route.queryParamMap, this.reload.pipe(startWith(undefined))])
      .pipe(
        switchMap(([params]) => {
          const page = Number(params.get('page') ?? 1);
          const size = Number(params.get('size') ?? 10);
          this.error.set(null);
          this.loading.set(true);
          return this.api
            .listUsers({
              page: Number.isSafeInteger(page) && page > 0 ? page : 1,
              size: Number.isInteger(size) && size >= 1 && size <= 100 ? size : 10,
            })
            .pipe(
              catchError((error) => {
                this.error.set(error);
                return EMPTY;
              }),
              finalize(() => this.loading.set(false)),
            );
        }),
        takeUntilDestroyed(this.destroy),
      )
      .subscribe((result) => this.result.set(result));
  }

  protected paginate(value: { page: number; size: number }): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: value,
      queryParamsHandling: 'merge',
    });
  }

  protected retry(): void {
    this.reload.next();
  }
}
