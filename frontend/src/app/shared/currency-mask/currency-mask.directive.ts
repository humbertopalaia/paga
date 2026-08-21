import {
  Directive,
  ElementRef,
  HostListener,
  forwardRef,
  inject,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

@Directive({
  selector: '[appCurrencyMask]',
  standalone: true,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => CurrencyMaskDirective),
      multi: true,
    },
  ],
})
export class CurrencyMaskDirective implements ControlValueAccessor {
  private readonly el = inject(ElementRef<HTMLInputElement>);

  private onChange: (value: number | null) => void = () => {};
  private onTouched: () => void = () => {};
  private disabled = false;

  writeValue(value: number | null): void {
    if (value == null || value === 0) {
      this.el.nativeElement.value = '';
      return;
    }
    this.el.nativeElement.value = this.formatNumber(value);
  }

  registerOnChange(fn: (value: number | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
    this.el.nativeElement.disabled = isDisabled;
  }

  @HostListener('input', ['$event'])
  onInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const rawValue = input.value;

    // Strip everything that's not a digit
    const digits = rawValue.replace(/\D/g, '');

    if (digits.length === 0) {
      input.value = '';
      this.onChange(null);
      return;
    }

    // Convert digits to numeric value (last 2 digits are decimal)
    const numericValue = parseInt(digits, 10) / 100;

    // Format and update display
    const formatted = this.formatNumber(numericValue);
    input.value = formatted;

    // Position cursor at end
    const pos = formatted.length;
    input.setSelectionRange(pos, pos);

    // Emit raw numeric value
    this.onChange(numericValue);
  }

  @HostListener('focus')
  onFocus(): void {
    const input = this.el.nativeElement;
    // Position cursor at end on focus
    setTimeout(() => {
      const len = input.value.length;
      input.setSelectionRange(len, len);
    });
  }

  @HostListener('blur')
  onBlur(): void {
    this.onTouched();
  }

  /**
   * Formats a numeric value as Brazilian Real: R$ 1.234,56
   */
  private formatNumber(value: number): string {
    const fixed = value.toFixed(2);
    const [integerPart, decimalPart] = fixed.split('.');

    // Add thousand separators (period in BRL)
    const withSeparators = integerPart.replace(
      /\B(?=(\d{3})+(?!\d))/g,
      '.'
    );

    return `R$ ${withSeparators},${decimalPart}`;
  }
}
