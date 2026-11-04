import { FormControl } from '@angular/forms';
import { accountFieldMessage, emailValidators, passwordValidators } from './account-validation';

describe('Account validation', () => {
  it.each(['Test-Password123!', 'Aa1!' + 'x'.repeat(8), 'Aa1!' + 'x'.repeat(124)])(
    'should accept passwords matching Identity policy',
    (value) => {
      expect(new FormControl(value, passwordValidators).valid).toBe(true);
    },
  );

  it.each([
    '',
    'Aa1!' + 'x'.repeat(7),
    'Aa1!' + 'x'.repeat(125),
    'test-password123!',
    'TEST-PASSWORD123!',
    'Test-Password!!!',
    'TestPassword1234',
  ])('should reject passwords violating Identity policy', (value) => {
    const control = new FormControl(value, passwordValidators);
    expect(control.invalid).toBe(true);
    expect(accountFieldMessage(control, 'password')).toBeNull();
    control.markAsTouched();
    expect(accountFieldMessage(control, 'password')).toBe(
      value ? 'identity.passwordHint' : 'identity.passwordRequired',
    );
  });

  it('should report the email error without success messages', () => {
    const control = new FormControl('', emailValidators);
    control.markAsTouched();
    expect(accountFieldMessage(control, 'email')).toBe('identity.emailRequired');
    control.setValue('invalid');
    expect(accountFieldMessage(control, 'email')).toBe('identity.emailInvalid');
    control.setValue('person@example.com');
    expect(control.valid).toBe(true);
    expect(accountFieldMessage(control, 'email')).toBeNull();
    control.setValue('x'.repeat(255));
    expect(accountFieldMessage(control, 'email')).toBe('identity.emailMaximum');
  });
});
