import { AbstractControl, ValidationErrors, Validators } from '@angular/forms';
import { MessageKey } from '../../shared/localization/messages';

export const emailValidators = [Validators.required, Validators.email, Validators.maxLength(254)];

export const passwordValidators = [
  Validators.required,
  Validators.minLength(12),
  Validators.maxLength(128),
  (control: AbstractControl): ValidationErrors | null => {
    const password = String(control.value ?? '');
    return /[A-Z]/.test(password) &&
      /[a-z]/.test(password) &&
      /[0-9]/.test(password) &&
      /[^a-zA-Z0-9]/.test(password) &&
      new Set(password).size >= 4
      ? null
      : { password: true };
  },
];

export function accountFieldMessage(
  control: AbstractControl,
  field: 'email' | 'password',
): MessageKey | null {
  if (!control.touched || control.valid) return null;
  if (control.hasError('required'))
    return field === 'email' ? 'identity.emailRequired' : 'identity.passwordRequired';
  if (field === 'email')
    return control.hasError('maxlength') ? 'identity.emailMaximum' : 'identity.emailInvalid';
  return 'identity.passwordHint';
}

export function passwordsDiffer(control: AbstractControl): ValidationErrors | null {
  const current = control.get('currentPassword')?.value;
  const next = control.get('newPassword')?.value;
  return current && next && current === next ? { passwordUnchanged: true } : null;
}
