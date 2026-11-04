import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  viewChild,
  PendingTasks,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthApi } from '../auth-api';
import { UsersApi } from '../../users/users-api';
import { SessionsApi } from '../../sessions/sessions-api';
import { Localization } from '../../../shared/localization/localization';
import { ErrorNotice } from '../../../shared/ui/error-notice';
import { Icon } from '../../../shared/ui/icon';
import { Pagination } from '../../../shared/ui/pagination';
import { Notice } from '../../../shared/ui/notice';
import { fieldCodes } from '../../../shared/http/api-problem';
import { MessageKey } from '../../../shared/localization/messages';
import {
  accountFieldMessage,
  emailValidators,
  passwordValidators,
  passwordsDiffer,
} from '../account-validation';
import { CurrentSession } from '../../sessions/get-current-session/current-session';
import { CurrentUser } from '../../users/get-current-user/current-user';

import { ListUserSessionItemResponse } from '../../users/list-user-sessions/list-user-session-item-response';
import { TrackedDetailsView } from '../../sessions/tracked-details/tracked-details-view';

@Component({
  imports: [
    Icon,
    Notice,
    Pagination,
    ReactiveFormsModule,
    ErrorNotice,
    RouterLink,
    TrackedDetailsView,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './account-security.page.html',
})
export class AccountSecurityPage {
  private readonly authApi = inject(AuthApi);
  private readonly usersApi = inject(UsersApi);
  private readonly sessionsApi = inject(SessionsApi);
  private readonly destroy = inject(DestroyRef);
  private readonly session = inject(CurrentSession);
  private readonly current = inject(CurrentUser);
  private readonly deleteConfirmation =
    viewChild.required<ElementRef<HTMLDialogElement>>('deleteConfirmation');
  private readonly deleteOtherSessionsConfirmation = viewChild.required<
    ElementRef<HTMLDialogElement>
  >('deleteOtherSessionsConfirmation');
  private loadVersion = 0;
  protected readonly i18n = inject(Localization);
  protected readonly error = signal<unknown>(null);
  protected readonly busy = signal(false);
  protected readonly saved = signal(false);
  protected readonly sessions = signal<readonly ListUserSessionItemResponse[]>([]);
  protected readonly sessionsError = signal<unknown>(null);
  protected readonly loading = signal(true);
  protected readonly page = signal(1);
  protected readonly size = signal(10);
  protected readonly count = signal(0);
  protected readonly emailForm = inject(FormBuilder).nonNullable.group({
    newEmail: ['', emailValidators],
    password: ['', passwordValidators],
  });
  protected readonly passwordForm = inject(FormBuilder).nonNullable.group(
    {
      currentPassword: ['', passwordValidators],
      newPassword: ['', passwordValidators],
    },
    { validators: passwordsDiffer },
  );
  protected readonly deleteForm = inject(FormBuilder).nonNullable.group({
    password: ['', passwordValidators],
  });
  protected readonly accountFieldMessage = accountFieldMessage;

  constructor() {
    inject(PendingTasks).run(() => this.listUserSessions(1));
  }

  protected async changeEmail(): Promise<void> {
    this.emailForm.markAllAsTouched();
    if (this.busy() || this.emailForm.invalid) return;
    this.busy.set(true);
    this.error.set(null);
    this.saved.set(false);
    try {
      await firstValueFrom(
        this.authApi
          .changeEmail(this.emailForm.getRawValue())
          .pipe(takeUntilDestroyed(this.destroy)),
      );
      if (!this.destroy.destroyed) {
        this.emailForm.reset();
        this.saved.set(true);
      }
    } catch (error) {
      if (!this.destroy.destroyed) this.error.set(error);
    } finally {
      this.emailForm.controls.password.reset();
      this.busy.set(false);
    }
  }

  protected async changePassword(): Promise<void> {
    this.passwordForm.markAllAsTouched();
    if (this.busy() || this.passwordForm.invalid) return;
    this.busy.set(true);
    this.error.set(null);
    try {
      await firstValueFrom(
        this.authApi
          .changePassword(this.passwordForm.getRawValue())
          .pipe(takeUntilDestroyed(this.destroy)),
      );
      if (!this.destroy.destroyed) this.session.expire();
    } catch (error) {
      if (!this.destroy.destroyed) this.error.set(error);
    } finally {
      this.passwordForm.reset();
      this.busy.set(false);
    }
  }

  protected newPasswordMessage(): MessageKey | null {
    const control = this.passwordForm.controls.newPassword;
    const validation = accountFieldMessage(control, 'password');
    if (validation) return validation;
    if (
      (control.touched && this.passwordForm.hasError('passwordUnchanged')) ||
      fieldCodes(this.error(), 'newPassword').includes('identity.auth.password-unchanged')
    )
      return 'identity.passwordUnchanged';
    return null;
  }

  protected openDeleteConfirmation(): void {
    this.deleteForm.markAllAsTouched();
    if (this.busy() || this.deleteForm.invalid || !this.current.user()?.id) return;
    const dialog = this.deleteConfirmation().nativeElement;
    if (!dialog.open) dialog.showModal();
  }

  protected closeDeleteConfirmation(): void {
    this.deleteConfirmation().nativeElement.close();
  }

  protected async deleteUser(): Promise<void> {
    this.deleteForm.markAllAsTouched();
    if (this.busy() || this.deleteForm.invalid) return;
    const userId = this.current.user()?.id;
    if (!userId || !this.deleteConfirmation().nativeElement.open) return;
    this.closeDeleteConfirmation();
    this.busy.set(true);
    this.error.set(null);
    try {
      await firstValueFrom(
        this.usersApi
          .deleteUser(this.deleteForm.getRawValue())
          .pipe(takeUntilDestroyed(this.destroy)),
      );
      if (!this.destroy.destroyed) this.session.accountDeleted();
    } catch (error) {
      if (!this.destroy.destroyed) this.error.set(error);
    } finally {
      this.deleteForm.reset();
      this.busy.set(false);
    }
  }

  protected async deleteSession(id: string, current: boolean): Promise<void> {
    if (this.busy()) return;
    if (current) {
      await this.session.deleteCurrentSession();
      return;
    }
    this.busy.set(true);
    try {
      await firstValueFrom(
        this.sessionsApi.deleteSession(id).pipe(takeUntilDestroyed(this.destroy)),
      );
      if (!this.destroy.destroyed) await this.listUserSessions(1);
    } catch (error) {
      if (!this.destroy.destroyed) this.error.set(error);
    } finally {
      this.busy.set(false);
    }
  }

  protected openDeleteOtherSessionsConfirmation(): void {
    if (this.busy() || this.loading() || this.sessionsError() || this.count() <= 1) return;
    const dialog = this.deleteOtherSessionsConfirmation().nativeElement;
    if (!dialog.open) dialog.showModal();
  }

  protected closeDeleteOtherSessionsConfirmation(): void {
    this.deleteOtherSessionsConfirmation().nativeElement.close();
  }

  protected async deleteOtherSessions(): Promise<void> {
    if (this.busy() || !this.deleteOtherSessionsConfirmation().nativeElement.open) return;
    this.closeDeleteOtherSessionsConfirmation();
    this.busy.set(true);
    try {
      await firstValueFrom(
        this.sessionsApi.deleteOtherSessions().pipe(takeUntilDestroyed(this.destroy)),
      );
      if (!this.destroy.destroyed) await this.listUserSessions(1);
    } catch (error) {
      if (!this.destroy.destroyed) this.error.set(error);
    } finally {
      this.busy.set(false);
    }
  }

  protected async listUserSessions(page: number, size = this.size()): Promise<void> {
    const version = ++this.loadVersion;
    this.loading.set(true);
    this.sessions.set([]);
    this.sessionsError.set(null);
    try {
      const userId = this.current.user()?.id;
      if (!userId) throw new Error('Current profile unavailable.');
      const result = await firstValueFrom(
        this.usersApi
          .listUserSessions(userId, { page, size })
          .pipe(takeUntilDestroyed(this.destroy)),
      );
      if (this.destroy.destroyed || version !== this.loadVersion) return;
      this.sessions.set(result.items);
      this.count.set(result.count);
      this.page.set(page);
      this.size.set(size);
    } catch (error) {
      if (!this.destroy.destroyed && version === this.loadVersion) this.sessionsError.set(error);
    } finally {
      if (version === this.loadVersion) this.loading.set(false);
    }
  }
}
