import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Component, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { By } from '@angular/platform-browser';

import { ExpensesChartComponent } from './expenses-chart.component';
import { ExpenseByType } from '../dashboard.model';

@Component({
  standalone: true,
  imports: [ExpensesChartComponent],
  template: `
    <app-expenses-chart
      [expensesByType]="expensesByType"
      [monthLabel]="monthLabel"
    />
  `,
})
class TestHostComponent {
  expensesByType: ExpenseByType[] = [
    { typeName: 'Alimentação', total: 800 },
    { typeName: 'Transporte', total: 700 },
    { typeName: 'Lazer', total: 300 },
  ];
  monthLabel = 'Janeiro 2025';
}

describe('ExpensesChartComponent', () => {
  let fixture: ComponentFixture<TestHostComponent>;
  let hostComponent: TestHostComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestHostComponent],
      providers: [provideAnimationsAsync()],
      schemas: [CUSTOM_ELEMENTS_SCHEMA],
    }).compileComponents();

    fixture = TestBed.createComponent(TestHostComponent);
    hostComponent = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should render chart section title with month label', () => {
    const title = fixture.debugElement.query(By.css('.chart-title'));
    expect(title).toBeTruthy();
    expect(title.nativeElement.textContent.trim()).toBe('Despesas por Tipo - Janeiro 2025');
  });

  it('should update title when monthLabel changes', () => {
    hostComponent.monthLabel = 'Março 2024';
    fixture.detectChanges();

    const title = fixture.debugElement.query(By.css('.chart-title'));
    expect(title.nativeElement.textContent.trim()).toBe('Despesas por Tipo - Março 2024');
  });

  it('should render chart container when data is provided', () => {
    const chartContainer = fixture.debugElement.query(By.css('.chart-container'));
    expect(chartContainer).toBeTruthy();

    const emptyState = fixture.debugElement.query(By.css('.empty-state'));
    expect(emptyState).toBeNull();
  });

  it('should render ngx-charts-bar-horizontal when data is provided', () => {
    const chart = fixture.debugElement.query(By.css('ngx-charts-bar-horizontal'));
    expect(chart).toBeTruthy();
  });

  it('should show empty state when expensesByType is empty', () => {
    hostComponent.expensesByType = [];
    fixture.detectChanges();

    const emptyState = fixture.debugElement.query(By.css('.empty-state'));
    expect(emptyState).toBeTruthy();
    expect(emptyState.nativeElement.textContent.trim()).toBe('Nenhuma despesa registrada neste mês');

    const chartContainer = fixture.debugElement.query(By.css('.chart-container'));
    expect(chartContainer).toBeNull();
  });

  it('should not show chart container when expensesByType is empty', () => {
    hostComponent.expensesByType = [];
    fixture.detectChanges();

    const chartContainer = fixture.debugElement.query(By.css('.chart-container'));
    expect(chartContainer).toBeNull();
  });

  it('should transform data to chart format', () => {
    const componentDebugEl = fixture.debugElement.query(By.directive(ExpensesChartComponent));
    const component = componentDebugEl.componentInstance as ExpensesChartComponent;

    const chartData = component.chartData();
    expect(chartData.length).toBe(3);
    expect(chartData[0]).toEqual({ name: 'Alimentação', value: 800 });
    expect(chartData[1]).toEqual({ name: 'Transporte', value: 700 });
    expect(chartData[2]).toEqual({ name: 'Lazer', value: 300 });
  });

  it('should have hasData as true when array is not empty', () => {
    const componentDebugEl = fixture.debugElement.query(By.directive(ExpensesChartComponent));
    const component = componentDebugEl.componentInstance as ExpensesChartComponent;
    expect(component.hasData()).toBeTrue();
  });

  it('should have hasData as false when array is empty', () => {
    hostComponent.expensesByType = [];
    fixture.detectChanges();

    const componentDebugEl = fixture.debugElement.query(By.directive(ExpensesChartComponent));
    const component = componentDebugEl.componentInstance as ExpensesChartComponent;
    expect(component.hasData()).toBeFalse();
  });

  it('should format currency in BRL', () => {
    const componentDebugEl = fixture.debugElement.query(By.directive(ExpensesChartComponent));
    const component = componentDebugEl.componentInstance as ExpensesChartComponent;

    const formatted = component.formatCurrency(1234.56);
    expect(formatted).toContain('R$');
    expect(formatted).toContain('1.234,56');
  });
});
