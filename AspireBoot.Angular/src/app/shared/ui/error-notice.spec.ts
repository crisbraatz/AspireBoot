import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ErrorNotice } from './error-notice';

describe('ErrorNotice', () => {
  beforeEach(() => {
    document.documentElement.lang = 'en-US';
  });

  it('should dismiss an error and reopen for a new failure', async () => {
    const fixture = TestBed.createComponent(ErrorNotice);
    fixture.componentRef.setInput(
      'error',
      new HttpErrorResponse({ status: 400, error: { traceId: 'reference' } }),
    );
    fixture.detectChanges();
    await fixture.whenStable();
    const root = fixture.nativeElement as HTMLElement;
    expect(root.textContent).toContain('Review the information provided');
    expect(root.textContent).toContain('reference');
    root.querySelector<HTMLButtonElement>('[aria-label="Dismiss notification"]')!.click();
    fixture.detectChanges();
    expect(root.querySelector('[role="alert"]')).toBeNull();
    fixture.componentRef.setInput(
      'error',
      new HttpErrorResponse({ status: 500, error: { traceId: 'email-failure' } }),
    );
    await fixture.whenStable();
    expect(root.querySelector('[role="alert"]')).not.toBeNull();
    expect(root.textContent).toContain('temporarily unavailable');
    expect(root.textContent).toContain('email-failure');
  });

  it.each([0, 500, 502, 503, 504])(
    'should keep service errors visible for status %s',
    async (status) => {
      const fixture = TestBed.createComponent(ErrorNotice);
      fixture.componentRef.setInput('error', new HttpErrorResponse({ status }));
      fixture.componentRef.setInput('retryable', true);
      await fixture.whenStable();
      expect(fixture.nativeElement.querySelector('[aria-label="Dismiss notification"]')).toBeNull();
      expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
      expect(fixture.nativeElement.textContent).toContain('Try again');
    },
  );

  it('should explain rejected registration without exposing account existence', async () => {
    const fixture = TestBed.createComponent(ErrorNotice);
    fixture.componentRef.setInput('validationMessage', 'identity.registrationFailed');
    fixture.componentRef.setInput('error', new HttpErrorResponse({ status: 400 }));
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain(
      'We could not create an account with these details.',
    );
    expect(fixture.nativeElement.textContent).not.toContain('highlighted');
    expect(fixture.nativeElement.textContent).not.toContain('already registered');
  });

  it('should explain an unchanged password using the API validation code', async () => {
    const fixture = TestBed.createComponent(ErrorNotice);
    fixture.componentRef.setInput(
      'error',
      new HttpErrorResponse({
        status: 400,
        error: { codes: { newPassword: ['identity.auth.password-unchanged'] } },
      }),
    );
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain(
      'Choose a password different from your current password.',
    );
  });

  it('should explain an incorrect current password separately from other validation failures', async () => {
    const fixture = TestBed.createComponent(ErrorNotice);
    fixture.componentRef.setInput('credentialMessage', 'identity.currentPasswordInvalid');
    fixture.componentRef.setInput(
      'error',
      new HttpErrorResponse({
        status: 400,
        error: { codes: { currentPassword: ['identity.auth.PasswordMismatch'] } },
      }),
    );
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain('Your current password was not accepted.');
    expect(fixture.nativeElement.textContent).not.toContain('Review the information provided');
    fixture.componentRef.setInput(
      'error',
      new HttpErrorResponse({
        status: 400,
        error: { codes: { newPassword: ['identity.auth.PasswordTooShort'] } },
      }),
    );
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain('Review the information provided');
    expect(fixture.nativeElement.textContent).not.toContain(
      'Your current password was not accepted.',
    );
  });
});
