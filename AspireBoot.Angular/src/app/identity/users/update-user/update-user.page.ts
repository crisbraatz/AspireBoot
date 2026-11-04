import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { fieldCodes } from '../../../shared/http/api-problem';
import { Localization } from '../../../shared/localization/localization';
import { MessageKey } from '../../../shared/localization/messages';
import { ErrorNotice } from '../../../shared/ui/error-notice';
import { Icon } from '../../../shared/ui/icon';
import { CurrentUser } from '../get-current-user/current-user';
import { nameMessage, validateName } from '../name-validation';
import { UserAvatar } from '../user-avatar/user-avatar';
import { UsersApi } from '../users-api';
import { avatarMessage, isHeicAvatar } from './avatar-validation';
import { AvatarConverter } from './avatar-converter';

@Component({
  imports: [ErrorNotice, Icon, ReactiveFormsModule, RouterLink, UserAvatar],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './update-user.page.html',
})
export class UpdateUserPage {
  private readonly api = inject(UsersApi);
  private readonly avatarConverter = inject(AvatarConverter);
  private readonly current = inject(CurrentUser);
  private readonly destroy = inject(DestroyRef);
  private readonly router = inject(Router);
  private objectUrl: string | null = null;
  private selection = 0;
  protected readonly i18n = inject(Localization);
  protected readonly error = signal<unknown>(null);
  protected readonly avatarError = signal<MessageKey | null>(null);
  protected readonly preview = signal(this.current.user()?.avatarUrl);
  protected readonly file = signal<File | null>(null);
  protected readonly saving = signal(false);
  protected readonly converting = signal(false);
  protected readonly form = inject(FormBuilder).nonNullable.group({
    newName: [this.current.user()?.name ?? '', validateName],
  });
  protected readonly nameMessage = nameMessage;

  constructor() {
    this.destroy.onDestroy(() => {
      this.selection++;
      this.revokePreview();
    });
  }

  protected async choose(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    this.discard();
    const selection = this.selection;
    const file = input.files?.[0];
    if (!file) return;
    const error = avatarMessage(file);
    this.avatarError.set(error);
    if (error) {
      input.value = '';
      return;
    }
    this.converting.set(isHeicAvatar(file));
    try {
      const avatar = isHeicAvatar(file) ? await this.avatarConverter.toJpeg(file) : file;
      if (this.destroy.destroyed || selection !== this.selection) return;
      const error = avatarMessage(avatar);
      if (error) {
        this.avatarError.set(error);
        input.value = '';
        return;
      }
      this.file.set(avatar);
      this.objectUrl = URL.createObjectURL(avatar);
      this.preview.set(this.objectUrl);
    } catch {
      if (!this.destroy.destroyed && selection === this.selection) {
        this.avatarError.set('users.avatarInvalid');
        input.value = '';
      }
    } finally {
      if (selection === this.selection) this.converting.set(false);
    }
  }

  protected discard(input?: HTMLInputElement): void {
    this.selection++;
    this.converting.set(false);
    this.revokePreview();
    this.file.set(null);
    this.preview.set(this.current.user()?.avatarUrl);
    this.avatarError.set(null);
    if (input) input.value = '';
  }

  protected async submit(): Promise<void> {
    if (this.saving() || this.converting()) return;
    this.form.markAllAsTouched();
    if (this.form.invalid || this.avatarError()) return;
    this.saving.set(true);
    this.error.set(null);
    try {
      await firstValueFrom(
        this.api
          .updateUser({ newName: this.form.controls.newName.value, avatar: this.file() })
          .pipe(takeUntilDestroyed(this.destroy)),
      );
      if (this.destroy.destroyed) return;
      await this.router.navigateByUrl('/profile', { info: { saved: true } });
    } catch (error) {
      if (this.destroy.destroyed) return;
      this.error.set(error);
      if (fieldCodes(error, 'avatar').length) this.avatarError.set('users.avatarInvalid');
    } finally {
      this.saving.set(false);
    }
  }

  private revokePreview(): void {
    if (this.objectUrl) URL.revokeObjectURL(this.objectUrl);
    this.objectUrl = null;
  }
}
