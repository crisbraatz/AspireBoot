import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  EMPTY,
  Subject,
  catchError,
  combineLatest,
  finalize,
  from,
  startWith,
  switchMap,
} from 'rxjs';
import { Localization } from '../../../shared/localization/localization';
import { ErrorNotice } from '../../../shared/ui/error-notice';
import { LoadingState } from '../../../shared/ui/loading-state';
import { CurrentSession } from '../../sessions/get-current-session/current-session';
import { CurrentUser } from '../get-current-user/current-user';
import { GetUserResponse } from './get-user-response';
import { ListUserSessions } from '../list-user-sessions/list-user-sessions';
import { UserAvatar } from '../user-avatar/user-avatar';
import { UsersApi } from '../users-api';
import { Icon } from '../../../shared/ui/icon';
import { Notice } from '../../../shared/ui/notice';

@Component({
  imports: [ErrorNotice, Icon, LoadingState, Notice, RouterLink, UserAvatar, ListUserSessions],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './get-user.page.html',
})
export class GetUserPage {
  private readonly router = inject(Router);
  private readonly api = inject(UsersApi);
  private readonly destroy = inject(DestroyRef);
  private readonly route = inject(ActivatedRoute);
  private readonly reload = new Subject<void>();
  protected readonly current = inject(CurrentUser);
  protected readonly i18n = inject(Localization);
  protected readonly session = inject(CurrentSession);
  protected readonly error = signal<unknown>(null);
  protected readonly loading = signal(true);
  protected readonly user = signal<GetUserResponse | null>(null);
  protected readonly saved =
    (this.router.currentNavigation()?.extras.info as { saved?: boolean } | undefined)?.saved ===
    true;

  constructor() {
    combineLatest([this.route.paramMap, this.reload.pipe(startWith(undefined))])
      .pipe(
        switchMap(([params]) => {
          this.error.set(null);
          this.user.set(null);
          this.loading.set(true);
          const id = params.get('id');
          return (id ? this.api.getUser(id) : from(this.current.reload())).pipe(
            catchError((error) => {
              if (
                error instanceof HttpErrorResponse &&
                error.status === 404 &&
                !this.session.isAdministrator
              ) {
                void this.router.navigateByUrl('/users', { replaceUrl: true });
                return EMPTY;
              }
              this.error.set(error);
              return EMPTY;
            }),
            finalize(() => this.loading.set(false)),
          );
        }),
        takeUntilDestroyed(this.destroy),
      )
      .subscribe((user) => this.user.set(user));
  }

  protected retry(): void {
    this.reload.next();
  }
}
