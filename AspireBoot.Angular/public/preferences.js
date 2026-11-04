// Only display preferences are persisted. Authentication tokens never belong in browser storage.
(function () {
  let theme = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  let locale = 'en-US';
  try {
    const savedTheme = localStorage.getItem('aspireboot.theme');
    const savedLocale = localStorage.getItem('aspireboot.locale');
    if (savedTheme === 'light' || savedTheme === 'dark') theme = savedTheme;
    if (savedLocale === 'pt-BR' || savedLocale === 'en-US') locale = savedLocale;
  } catch {
    /* Storage can be disabled; the application still works. */
  }
  document.documentElement.dataset.theme = theme;
  document.documentElement.lang = locale;
  // Apply document translations before Angular starts, including when API initialization fails.
  for (const meta of document.querySelectorAll('meta[data-pt-br][data-en-us]'))
    meta.content = meta.getAttribute('data-' + locale.toLowerCase()) ?? meta.content;
})();
