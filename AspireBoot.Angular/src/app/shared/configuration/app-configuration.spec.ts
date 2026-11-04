import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { AppConfiguration, validateSettings } from './app-configuration';

describe('application configuration', () => {
  const settings = {
    apiUrl: 'https://api.example.com/api',
  };
  it('should accept public HTTPS configuration', () => {
    expect(validateSettings(settings)).toEqual(settings);
  });
  it.each([
    'http://api.example.com/api',
    'https://user:password@api.example.com/api',
    'https://api.example.com/api?token=secret',
    'javascript:alert(1)',
    '/api',
  ])('should reject unsafe API configuration %s', (apiUrl) => {
    expect(() => validateSettings({ ...settings, apiUrl })).toThrow();
  });
  it.each([null, {}])('should reject incomplete configuration', (value) => {
    expect(() => validateSettings(value)).toThrow();
  });
});

describe('AppConfiguration loading', () => {
  let configuration: AppConfiguration;
  let fetchSettings: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchSettings = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({ apiUrl: 'https://app.example.com/api/' }),
    });
    vi.stubGlobal('fetch', fetchSettings);
    TestBed.configureTestingModule({
      providers: [
        {
          provide: DOCUMENT,
          useValue: {
            baseURI: 'https://app.example.com/',
            location: { origin: 'https://app.example.com' },
          },
        },
      ],
    });
    configuration = TestBed.inject(AppConfiguration);
  });

  afterEach(() => vi.unstubAllGlobals());

  it('should load fresh same-origin configuration and normalize the API prefix', async () => {
    await configuration.load();

    expect(fetchSettings).toHaveBeenCalledWith(
      new URL('https://app.example.com/appsettings.json'),
      {
        cache: 'no-store',
      },
    );
    expect(configuration.value).toEqual({ apiUrl: 'https://app.example.com/api' });
  });

  it('should refuse access before configuration has loaded', () => {
    expect(() => configuration.value).toThrow('has not loaded');
  });

  it('should reject unsuccessful HTTP responses', async () => {
    fetchSettings.mockResolvedValue({ ok: false });

    await expect(configuration.load()).rejects.toThrow('could not be loaded');
    expect(() => configuration.value).toThrow('has not loaded');
  });

  it('should propagate an unavailable configuration request', async () => {
    fetchSettings.mockRejectedValue(new Error('Offline'));

    await expect(configuration.load()).rejects.toThrow('Offline');
    expect(() => configuration.value).toThrow('has not loaded');
  });

  it('should reject malformed JSON', async () => {
    fetchSettings.mockResolvedValue({
      ok: true,
      json: async () => {
        throw new SyntaxError('JSON');
      },
    });

    await expect(configuration.load()).rejects.toThrow('JSON');
    expect(() => configuration.value).toThrow('has not loaded');
  });

  it('should never expose a rejected cross-origin configuration', async () => {
    fetchSettings.mockResolvedValue({
      ok: true,
      json: async () => ({ apiUrl: 'https://other.example.com/api' }),
    });

    await expect(configuration.load()).rejects.toThrow('same origin');
    expect(() => configuration.value).toThrow('has not loaded');
  });
});
