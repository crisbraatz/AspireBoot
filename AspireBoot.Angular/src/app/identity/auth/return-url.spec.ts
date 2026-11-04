import { safeReturnUrl } from './return-url';

describe('return URL', () => {
  it('should retain internal routes and pagination', () => {
    expect(safeReturnUrl('/users?page=2&size=20')).toBe('/users?page=2&size=20');
  });
  it.each([
    null,
    '',
    'https://evil.example',
    '//evil.example',
    '/\\evil.example',
    '/auth/callback?code=secret',
    '/users\nLocation:evil',
  ])('should reject an unsafe or looping destination', (value) => {
    expect(safeReturnUrl(value)).toBe('/users');
  });
});
