import { FormControl } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { nameMessage, validateName } from './name-validation';

describe('name validation', () => {
  it.each(['Ab', ' User Name ', 'João da Silva', 'Jose\u0301', 'n'.repeat(100)])(
    'should accept a valid trimmed name',
    (name) => {
      expect(validateName(new FormControl(name))).toBeNull();
    },
  );
  it.each(['User123', '123', 'User-Name', 'User 😀'])(
    'should reject non-letter characters',
    (name) => {
      expect(validateName(new FormControl(name))).toEqual({ characters: true });
    },
  );
  it.each([null, '', '   '])('should reject missing names', (name) => {
    expect(validateName(new FormControl(name))).toEqual({ required: true });
  });
  it('should reject shorter and longer names', () => {
    expect(validateName(new FormControl(' A '))).toEqual({ minimum: true });
    expect(validateName(new FormControl('n'.repeat(101)))).toEqual({ maximum: true });
  });
  it('should show client errors after interaction and handle server annotation errors', () => {
    const control = new FormControl('', validateName);
    expect(nameMessage(control, null)).toBeNull();
    control.markAsTouched();
    expect(nameMessage(control, null)).toBe('users.nameRequired');
    control.setValue('Valid Name');
    expect(
      nameMessage(
        control,
        new HttpErrorResponse({
          status: 400,
          error: {
            errors: {
              newName: ['The field NewName must be a string with a maximum length of 100.'],
            },
          },
        }),
        'newName',
      ),
    ).toBe('users.nameInvalid');
  });
});
