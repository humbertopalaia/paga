import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Component } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { LOCALE_ID } from '@angular/core';
import { MetricCardComponent } from './metric-card.component';

registerLocaleData(localePt, 'pt-BR');

@Component({
  standalone: true,
  imports: [MetricCardComponent],
  template: `
    <app-metric-card
      [title]="title"
      [value]="value"
      [valueColor]="valueColor"
      [subtitle]="subtitle"
    />
  `,
})
class TestHostComponent {
  title = 'Saldo Atual';
  value = 12500.75;
  valueColor = '#3b82f6';
  subtitle: string | undefined = 'Total acumulado';
}

describe('MetricCardComponent', () => {
  let fixture: ComponentFixture<TestHostComponent>;
  let hostComponent: TestHostComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestHostComponent],
      providers: [{ provide: LOCALE_ID, useValue: 'pt-BR' }],
    }).compileComponents();

    fixture = TestBed.createComponent(TestHostComponent);
    hostComponent = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should render the title', () => {
    const titleEl = fixture.nativeElement.querySelector('.metric-title');
    expect(titleEl.textContent.trim()).toBe('Saldo Atual');
  });

  it('should render value formatted as BRL currency', () => {
    const valueEl = fixture.nativeElement.querySelector('.metric-value');
    expect(valueEl.textContent.trim()).toContain('R$');
    expect(valueEl.textContent.trim()).toContain('12.500,75');
  });

  it('should apply the valueColor to the value element', () => {
    const valueEl = fixture.nativeElement.querySelector('.metric-value') as HTMLElement;
    expect(valueEl.style.color).toBe('rgb(59, 130, 246)');
  });

  it('should render subtitle when provided', () => {
    const subtitleEl = fixture.nativeElement.querySelector('.metric-subtitle');
    expect(subtitleEl).toBeTruthy();
    expect(subtitleEl.textContent.trim()).toBe('Total acumulado');
  });

  it('should not render subtitle when not provided', () => {
    hostComponent.subtitle = undefined;
    fixture.detectChanges();
    const subtitleEl = fixture.nativeElement.querySelector('.metric-subtitle');
    expect(subtitleEl).toBeNull();
  });

  it('should render zero value correctly', () => {
    hostComponent.value = 0;
    fixture.detectChanges();
    const valueEl = fixture.nativeElement.querySelector('.metric-value');
    expect(valueEl.textContent.trim()).toContain('R$');
    expect(valueEl.textContent.trim()).toContain('0,00');
  });

  it('should render negative value correctly', () => {
    hostComponent.value = -1500.5;
    fixture.detectChanges();
    const valueEl = fixture.nativeElement.querySelector('.metric-value');
    expect(valueEl.textContent.trim()).toContain('R$');
    expect(valueEl.textContent.trim()).toContain('1.500,50');
  });
});
