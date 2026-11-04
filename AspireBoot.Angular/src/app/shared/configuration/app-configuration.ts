import { DOCUMENT } from '@angular/common';
import { Injectable, inject } from '@angular/core';

export interface AppSettings {
  readonly apiUrl: string;
}

export function validateSettings(value: unknown): AppSettings {
  if (!value || typeof value !== 'object') throw new Error('Invalid application configuration.');
  const settings = value as Partial<AppSettings>;
  if (typeof settings.apiUrl !== 'string') throw new Error('Missing application URL.');
  const url = new URL(settings.apiUrl);
  if (url.protocol !== 'https:' || url.username || url.password || url.search || url.hash)
    throw new Error('Application URLs must use HTTPS without credentials, query or fragment.');
  return { apiUrl: url.href.replace(/\/+$/, '') };
}

@Injectable({ providedIn: 'root' })
export class AppConfiguration {
  private readonly document = inject(DOCUMENT);
  private settings: AppSettings | null = null;

  get value(): AppSettings {
    if (!this.settings) throw new Error('Application configuration has not loaded.');
    return this.settings;
  }

  async load(): Promise<void> {
    const response = await fetch(new URL('appsettings.json', this.document.baseURI), {
      cache: 'no-store',
    });
    if (!response.ok) throw new Error('Application configuration could not be loaded.');
    const settings = validateSettings(await response.json());
    if (new URL(settings.apiUrl).origin !== this.document.location.origin)
      throw new Error('The browser API must use the same origin as the application.');
    this.settings = settings;
  }
}
