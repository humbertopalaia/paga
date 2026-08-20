import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export const passwordMatchValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const password = control.get('password');
  const confirmation = control.get('passwordConfirmation');

  if (!password || !confirmation) return null;
  if (!confirmation.value) return null;

  return password.value === confirmation.value ? null : { passwordMismatch: true };
};
