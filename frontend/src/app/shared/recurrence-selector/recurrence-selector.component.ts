import {
  Component,
  ChangeDetectionStrategy,
  forwardRef,
  signal,
} from '@angular/core';
import { NG_VALUE_ACCESSOR, ControlValueAccessor } from '@angular/forms';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';

export interface RecurrenceValue {
  isRecurring: boolean;
  frequency: string | null;
}

@Component({
  selector: 'app-recurrence-selector',
  standalone: true,
  imports: [MatSlideToggleModule, MatFormFieldModule, MatSelectModule],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => RecurrenceSelectorComponent),
      multi: true,
    },
  ],
  templateUrl: './recurrence-selector.component.html',
  styleUrl: './recurrence-selector.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecurrenceSelectorComponent implements ControlValueAccessor {
  readonly isRecurring = signal(false);
  readonly frequency = signal<string | null>(null);
  readonly disabled = signal(false);

  readonly frequencyOptions = [
    { value: 'weekly', label: 'Semanal' },
    { value: 'monthly', label: 'Mensal' },
    { value: 'yearly', label: 'Anual' },
  ];

  private onChange: (value: RecurrenceValue) => void = () => {};
  private onTouched: () => void = () => {};

  writeValue(value: RecurrenceValue | null): void {
    if (value) {
      this.isRecurring.set(value.isRecurring);
      this.frequency.set(value.frequency);
    } else {
      this.isRecurring.set(false);
      this.frequency.set(null);
    }
  }

  registerOnChange(fn: (value: RecurrenceValue) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled.set(isDisabled);
  }

  onToggleChange(checked: boolean): void {
    this.isRecurring.set(checked);
    if (!checked) {
      this.frequency.set(null);
    }
    this.emitValue();
    this.onTouched();
  }

  onFrequencyChange(value: string): void {
    this.frequency.set(value);
    this.emitValue();
    this.onTouched();
  }

  private emitValue(): void {
    this.onChange({
      isRecurring: this.isRecurring(),
      frequency: this.isRecurring() ? this.frequency() : null,
    });
  }
}
