import { AbstractControl, ValidationErrors } from '@angular/forms';
import { fieldErrors } from '../../shared/http/api-problem';
import { MessageKey } from '../../shared/localization/messages';

export function validateName(control: AbstractControl): ValidationErrors | null {
  const name = typeof control.value === 'string' ? control.value.trim().replace(/\s+/g, ' ') : '';
  if (!name) return { required: true };
  if (name.length < 2) return { minimum: true };
  if (name.length > 100) return { maximum: true };
  return /^\p{L}[\p{L}\p{M}]*(?: \p{L}[\p{L}\p{M}]*)*$/u.test(name) ? null : { characters: true };
}

export function nameMessage(
  control: AbstractControl,
  error: unknown,
  field = 'name',
): MessageKey | null {
  if (fieldErrors(error, field).length) return 'users.nameInvalid';
  if (!control.touched) return null;
  if (control.hasError('required')) return 'users.nameRequired';
  if (control.hasError('minimum')) return 'users.nameMinimum';
  if (control.hasError('maximum')) return 'users.nameMaximum';
  return control.hasError('characters') ? 'users.nameCharacters' : null;
}
