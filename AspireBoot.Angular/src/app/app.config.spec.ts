import { ApplicationInitStatus } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { appConfig } from './app.config';
import { AppConfiguration } from './shared/configuration/app-configuration';
import { CurrentSession } from './identity/sessions/get-current-session/current-session';

describe('application initialization', () => {
  let load: ReturnType<typeof vi.fn>;
  let getCurrentSession: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    load = vi.fn().mockResolvedValue(undefined);
    getCurrentSession = vi.fn().mockResolvedValue(true);
    TestBed.configureTestingModule({
      providers: [
        ...appConfig.providers,
        { provide: AppConfiguration, useValue: { load } },
        { provide: CurrentSession, useValue: { getCurrentSession } },
      ],
    });
  });

  it('should load public configuration before restoring the session', async () => {
    let finishConfiguration!: () => void;
    load.mockReturnValue(
      new Promise<void>((resolve) => {
        finishConfiguration = resolve;
      }),
    );
    const initialization = TestBed.inject(ApplicationInitStatus);

    expect(getCurrentSession).not.toHaveBeenCalled();
    finishConfiguration();
    await initialization.donePromise;

    expect(load).toHaveBeenCalledOnce();
    expect(getCurrentSession).toHaveBeenCalledOnce();
  });

  it('should keep public pages available when session restoration is unavailable', async () => {
    getCurrentSession.mockRejectedValue(new Error('Unavailable'));
    const initialization = TestBed.inject(ApplicationInitStatus);

    await initialization.donePromise;

    expect(initialization.done).toBe(true);
  });

  it('should stop startup when configuration is unsafe or unavailable', async () => {
    load.mockRejectedValue(new Error('Invalid configuration'));
    const initialization = TestBed.inject(ApplicationInitStatus);

    await expect(initialization.donePromise).rejects.toThrow('Invalid configuration');
    expect(getCurrentSession).not.toHaveBeenCalled();
  });
});
