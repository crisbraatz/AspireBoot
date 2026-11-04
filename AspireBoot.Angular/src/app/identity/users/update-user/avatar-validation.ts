import { MessageKey } from '../../../shared/localization/messages';

export function avatarMessage(
  file: Pick<File, 'size' | 'type'> & { name?: string },
): MessageKey | null {
  if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type) && !isHeicAvatar(file))
    return 'users.avatarType';
  return file.size > 4 * 1024 * 1024 ? 'users.avatarSize' : null;
}

export function isHeicAvatar(file: Pick<File, 'type'> & { name?: string }): boolean {
  return (
    ['image/heic', 'image/heif'].includes(file.type) ||
    (['', 'application/octet-stream'].includes(file.type) &&
      /\.(heic|heif)$/i.test(file.name ?? ''))
  );
}
