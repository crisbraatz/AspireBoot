export function safeReturnUrl(value: string | null): string {
  if (
    !value ||
    !value.startsWith('/') ||
    value.startsWith('//') ||
    [...value].some((character) => character === '\\' || character.charCodeAt(0) <= 32) ||
    value.startsWith('/auth/')
  )
    return '/users';
  return value;
}
