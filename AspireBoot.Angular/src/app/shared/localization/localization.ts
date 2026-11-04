import { DOCUMENT } from '@angular/common';
import { Injectable, inject, signal } from '@angular/core';
import { english, MessageKey, portuguese } from './messages';

export type Locale = 'pt-BR' | 'en-US';

@Injectable({ providedIn: 'root' })
export class Localization {
  private readonly document = inject(DOCUMENT);
  readonly locale = signal<Locale>(
    this.document.documentElement.lang === 'pt-BR' ? 'pt-BR' : 'en-US',
  );

  constructor() {
    this.updateDocument();
  }

  formatDate(value: string): string {
    return new Intl.DateTimeFormat(this.locale(), {
      dateStyle: 'short',
      timeStyle: 'short',
    }).format(new Date(value));
  }

  setLocale(value: string): void {
    if (value !== 'pt-BR' && value !== 'en-US') return;
    this.locale.set(value);
    this.updateDocument();
    try {
      localStorage.setItem('aspireboot.locale', value);
    } catch {
      /* Preferences are optional. */
    }
  }

  t(key: MessageKey, parameters: Record<string, string | number> = {}): string {
    let message: string = this.locale() === 'pt-BR' ? portuguese[key] : english[key];
    for (const [name, value] of Object.entries(parameters))
      message = message.replaceAll(`{${name}}`, String(value));
    return message;
  }

  private updateDocument(): void {
    const locale = this.locale();
    this.document.documentElement.lang = locale;
    for (const meta of this.document.querySelectorAll<HTMLMetaElement>(
      'meta[data-pt-br][data-en-us]',
    ))
      meta.content = meta.getAttribute('data-' + locale.toLowerCase()) ?? meta.content;
  }
}
