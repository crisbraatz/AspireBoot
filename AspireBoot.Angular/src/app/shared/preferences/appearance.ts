import { DOCUMENT } from '@angular/common';
import { Injectable, inject, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class Appearance {
  private readonly document = inject(DOCUMENT);
  readonly theme = signal<'light' | 'dark'>(
    this.document.documentElement.dataset['theme'] === 'dark' ? 'dark' : 'light',
  );

  toggle(): void {
    const theme = this.theme() === 'dark' ? 'light' : 'dark';
    this.theme.set(theme);
    this.document.documentElement.dataset['theme'] = theme;
    try {
      localStorage.setItem('aspireboot.theme', theme);
    } catch {
      /* Preferences are optional. */
    }
  }
}
