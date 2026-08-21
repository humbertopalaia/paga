import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { CurrencyMaskDirective } from './currency-mask.directive';

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, CurrencyMaskDirective],
  template: `<input appCurrencyMask [formControl]="control" />`,
})
class TestHostComponent {
  control = new FormControl<number | null>(null);
}

describe('CurrencyMaskDirective', () => {
  let fixture: ComponentFixture<TestHostComponent>;
  let input: HTMLInputElement;
  let component: TestHostComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestHostComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(TestHostComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    input = fixture.nativeElement.querySelector('input');
  });

  it('should create the directive', () => {
    expect(input).toBeTruthy();
  });

  it('should format typed digits as BRL currency', () => {
    simulateInput('123456');
    expect(input.value).toBe('R$ 1.234,56');
  });

  it('should expose raw numeric value to form control', () => {
    simulateInput('123456');
    expect(component.control.value).toBe(1234.56);
  });

  it('should reject non-numeric characters', () => {
    simulateInput('12a3b4');
    expect(input.value).toBe('R$ 12,34');
    expect(component.control.value).toBe(12.34);
  });

  it('should handle writeValue from form control', () => {
    component.control.setValue(2500.5);
    fixture.detectChanges();
    expect(input.value).toBe('R$ 2.500,50');
  });

  it('should clear input when value is null', () => {
    component.control.setValue(100);
    fixture.detectChanges();
    expect(input.value).toBe('R$ 100,00');

    component.control.setValue(null);
    fixture.detectChanges();
    expect(input.value).toBe('');
  });

  it('should emit null when input is cleared', () => {
    simulateInput('');
    expect(component.control.value).toBeNull();
  });

  function simulateInput(value: string): void {
    input.value = value;
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  }
});
