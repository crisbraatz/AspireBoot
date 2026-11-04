import { TestBed } from '@angular/core/testing';
import { Localization } from './localization';

describe('Localization', () => {
  let description: HTMLMetaElement;
  let socialDescription: HTMLMetaElement;
  let socialLocale: HTMLMetaElement;
  let originalLanguage: string;

  beforeEach(() => {
    originalLanguage = document.documentElement.lang;
    document.documentElement.lang = 'en-US';
    description = document.createElement('meta');
    description.name = 'description';
    socialDescription = document.createElement('meta');
    socialDescription.setAttribute('property', 'og:description');
    socialLocale = document.createElement('meta');
    socialLocale.setAttribute('property', 'og:locale');
    for (const meta of [description, socialDescription]) {
      meta.setAttribute('data-en-us', 'Your space to connect and take care of your profile.');
      meta.setAttribute('data-pt-br', 'Seu espaço para se conectar e cuidar do seu perfil.');
    }
    socialLocale.setAttribute('data-en-us', 'en_US');
    socialLocale.setAttribute('data-pt-br', 'pt_BR');
    document.head.append(description, socialDescription, socialLocale);
  });

  afterEach(() => {
    description.remove();
    socialDescription.remove();
    socialLocale.remove();
    document.documentElement.lang = originalLanguage;
    localStorage.removeItem('aspireboot.locale');
    vi.restoreAllMocks();
  });

  it('should initialize the document and metadata in English', () => {
    const localization = TestBed.inject(Localization);

    expect(localization.locale()).toBe('en-US');
    expect(document.documentElement.lang).toBe('en-US');
    expect(description.content).toBe('Your space to connect and take care of your profile.');
    expect(socialDescription.content).toBe(description.content);
    expect(socialLocale.content).toBe('en_US');
  });

  it('should keep saved Portuguese UI and document metadata in the same language', () => {
    document.documentElement.lang = 'pt-BR';

    const localization = TestBed.inject(Localization);

    expect(localization.locale()).toBe('pt-BR');
    expect(description.content).toBe('Seu espaço para se conectar e cuidar do seu perfil.');
    expect(socialDescription.content).toBe(description.content);
    expect(socialLocale.content).toBe('pt_BR');
  });

  it('should update metadata in both directions and persist the selected locale', () => {
    const localization = TestBed.inject(Localization);

    localization.setLocale('pt-BR');

    expect(document.documentElement.lang).toBe('pt-BR');
    expect(description.content).toBe('Seu espaço para se conectar e cuidar do seu perfil.');
    expect(socialDescription.content).toBe(description.content);
    expect(socialLocale.content).toBe('pt_BR');
    expect(localStorage.getItem('aspireboot.locale')).toBe('pt-BR');

    localization.setLocale('en-US');

    expect(document.documentElement.lang).toBe('en-US');
    expect(description.content).toBe('Your space to connect and take care of your profile.');
    expect(socialDescription.content).toBe(description.content);
    expect(socialLocale.content).toBe('en_US');
    expect(localStorage.getItem('aspireboot.locale')).toBe('en-US');
  });

  it('should use English for an unknown document language', () => {
    document.documentElement.lang = 'fr-FR';

    const localization = TestBed.inject(Localization);

    expect(localization.locale()).toBe('en-US');
    expect(document.documentElement.lang).toBe('en-US');
    expect(description.content).toBe('Your space to connect and take care of your profile.');
  });

  it('should ignore unsupported locale selections', () => {
    const localization = TestBed.inject(Localization);

    localization.setLocale('fr-FR');

    expect(localization.locale()).toBe('en-US');
    expect(document.documentElement.lang).toBe('en-US');
    expect(socialLocale.content).toBe('en_US');
    expect(localStorage.getItem('aspireboot.locale')).toBeNull();
  });

  it('should keep language and metadata working when preference storage is blocked', () => {
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new DOMException('Blocked', 'SecurityError');
    });
    const localization = TestBed.inject(Localization);

    localization.setLocale('pt-BR');

    expect(document.documentElement.lang).toBe('pt-BR');
    expect(description.content).toBe('Seu espaço para se conectar e cuidar do seu perfil.');
    expect(socialLocale.content).toBe('pt_BR');
  });
});
