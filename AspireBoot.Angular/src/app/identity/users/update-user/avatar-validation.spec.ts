import { avatarMessage, isHeicAvatar } from './avatar-validation';

describe('avatar validation', () => {
  it.each(['image/jpeg', 'image/png', 'image/webp'])(
    'should accept a supported image at the maximum size',
    (type) => {
      expect(avatarMessage({ size: 4 * 1024 * 1024, type })).toBeNull();
    },
  );
  it('should reject unsupported types and oversized files', () => {
    expect(avatarMessage({ size: 100, type: 'image/svg+xml' })).toBe('users.avatarType');
    expect(avatarMessage({ size: 4 * 1024 * 1024 + 1, type: 'image/png' })).toBe(
      'users.avatarSize',
    );
  });
  it.each(['image/heic', 'image/heif'])('should accept an iPhone photo by MIME type', (type) => {
    expect(avatarMessage({ size: 1024, type })).toBeNull();
    expect(avatarMessage({ size: 4 * 1024 * 1024 + 1, type })).toBe('users.avatarSize');
  });
  it.each(['photo.HEIC', 'photo.heif'])(
    'should accept HEIC extensions when the browser omits the MIME type',
    (name) => {
      expect(avatarMessage({ size: 1024, type: '', name })).toBeNull();
    },
  );
  it('should preserve supported images with a misleading HEIC filename', () => {
    const file = { size: 100, type: 'image/jpeg', name: 'photo.heic' };

    expect(avatarMessage(file)).toBeNull();
    expect(isHeicAvatar(file)).toBe(false);
  });

  it('should not accept an unsupported MIME type based only on its filename', () => {
    expect(avatarMessage({ size: 100, type: 'image/svg+xml', name: 'photo.heic' })).toBe(
      'users.avatarType',
    );
  });
});
