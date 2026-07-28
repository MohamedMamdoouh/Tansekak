import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export function digitsOnlyValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (value == null || String(value).trim() === '') return null;
    return /^\d+$/.test(String(value).trim()) ? null : { digitsOnly: true };
  };
}

export function stripToDigits(value: string): string {
  return value.replace(/\D/g, '');
}

export function applyDigitsOnlyInput(
  event: Event,
  control: AbstractControl | null,
): void {
  const input = event.target as HTMLInputElement;
  const digits = stripToDigits(input.value);
  if (input.value !== digits) {
    input.value = digits;
    control?.setValue(digits);
  }
}
