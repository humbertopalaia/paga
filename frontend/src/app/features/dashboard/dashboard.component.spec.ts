import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Component, input } from '@angular/core';
import { of, throwError, Subject } from 'rxjs';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { By } from '@angular/platform-browser';

import { DashboardComponent } from './dashboard.component';
import { DashboardService } from './dashboard.service';
import { DashboardResponse, ExpenseByType } from './dashboard.model';
import { MetricCardComponent } from '../../shared/metric-card/metric-card.component';
import { ExpensesChartComponent } from './expenses-chart/expenses-chart.component';

@Component({
  selector: 'app-metric-card',
  standalone: true,
  template: `<div class="metric-card-stub">
    <span class="stub-title">{{ title() }}</span>
    <span class="stub-value">{{ value() }}</span>
    <span class="stub-color">{{ valueColor() }}</span>
    <span class="stub-subtitle">{{ subtitle() }}</span>
  </div>`,
})
class MetricCardStubComponent {
  title = input.required<string>();
  value = input.required<number>();
  valueColor = input.required<string>();
  subtitle = input<string>();
}

@Component({
  selector: 'app-expenses-chart',
  standalone: true,
  template: `<div class="chart-stub">
    <span class="stub-expenses">{{ expensesByType().length }}</span>
    <span class="stub-month">{{ monthLabel() }}</span>
  </div>`,
})
class ExpensesChartStubComponent {
  expensesByType = input.required<ExpenseByType[]>();
  monthLabel = input.required<string>();
}

describe('DashboardComponent', () => {
  let component: DashboardComponent;
  let fixture: ComponentFixture<DashboardComponent>;
  let dashboardServiceSpy: jasmine.SpyObj<DashboardService>;

  const mockDashboardData: DashboardResponse = {
    currentBalance: 3500.75,
    monthlyIncome: 5000.0,
    monthlyExpense: 1500.25,
    expensesByType: [
      { typeName: 'Alimentação', total: 800.0 },
      { typeName: 'Transporte', total: 700.25 },
    ],
  };

  const emptyDashboardData: DashboardResponse = {
    currentBalance: 0,
    monthlyIncome: 0,
    monthlyExpense: 0,
    expensesByType: [],
  };

  beforeEach(async () => {
    dashboardServiceSpy = jasmine.createSpyObj('DashboardService', ['getDashboard']);
    dashboardServiceSpy.getDashboard.and.returnValue(of(mockDashboardData));

    await TestBed.configureTestingModule({
      imports: [DashboardComponent],
      providers: [
        provideAnimationsAsync(),
        { provide: DashboardService, useValue: dashboardServiceSpy },
      ],
    })
      .overrideComponent(DashboardComponent, {
        remove: { imports: [MetricCardComponent, ExpensesChartComponent] },
        add: { imports: [MetricCardStubComponent, ExpensesChartStubComponent] },
      })
      .compileComponents();

    fixture = TestBed.createComponent(DashboardComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  describe('loading state', () => {
    it('should display skeleton cards during loading', () => {
      const subject = new Subject<DashboardResponse>();
      dashboardServiceSpy.getDashboard.and.returnValue(subject.asObservable());

      fixture.detectChanges(); // triggers ngOnInit → loadData

      const skeletonCards = fixture.debugElement.queryAll(By.css('.skeleton-card'));
      expect(skeletonCards.length).toBe(3);

      const skeletonChart = fixture.debugElement.query(By.css('.skeleton-chart'));
      expect(skeletonChart).toBeTruthy();
    });

    it('should hide skeleton and show data after loading completes', () => {
      fixture.detectChanges();

      const skeletonCards = fixture.debugElement.queryAll(By.css('.skeleton-card'));
      expect(skeletonCards.length).toBe(0);

      const metricCards = fixture.debugElement.queryAll(By.css('app-metric-card'));
      expect(metricCards.length).toBe(3);
    });
  });

  describe('metric cards display', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should display three metric cards with correct values', () => {
      const cards = fixture.debugElement.queryAll(By.css('app-metric-card'));
      expect(cards.length).toBe(3);

      const values = cards.map(c => c.query(By.css('.stub-value')).nativeElement.textContent.trim());
      expect(values[0]).toBe('3500.75');
      expect(values[1]).toBe('5000');
      expect(values[2]).toBe('1500.25');
    });

    it('should display correct titles for cards', () => {
      const cards = fixture.debugElement.queryAll(By.css('app-metric-card'));
      const titles = cards.map(c => c.query(By.css('.stub-title')).nativeElement.textContent.trim());

      expect(titles[0]).toBe('Saldo Atual');
      expect(titles[1]).toBe('Receitas do Mês');
      expect(titles[2]).toBe('Despesas do Mês');
    });

    it('should apply blue color to positive balance', () => {
      const cards = fixture.debugElement.queryAll(By.css('app-metric-card'));
      const balanceColor = cards[0].query(By.css('.stub-color')).nativeElement.textContent.trim();
      expect(balanceColor).toBe('#3b82f6');
    });

    it('should apply red color to negative balance', () => {
      const negativeDashboard: DashboardResponse = {
        ...mockDashboardData,
        currentBalance: -500,
      };
      dashboardServiceSpy.getDashboard.and.returnValue(of(negativeDashboard));
      component.loadData();
      fixture.detectChanges();

      const cards = fixture.debugElement.queryAll(By.css('app-metric-card'));
      const balanceColor = cards[0].query(By.css('.stub-color')).nativeElement.textContent.trim();
      expect(balanceColor).toBe('#ef4444');
    });

    it('should apply green color to income card', () => {
      const cards = fixture.debugElement.queryAll(By.css('app-metric-card'));
      const incomeColor = cards[1].query(By.css('.stub-color')).nativeElement.textContent.trim();
      expect(incomeColor).toBe('#10b981');
    });

    it('should apply red color to expense card', () => {
      const cards = fixture.debugElement.queryAll(By.css('app-metric-card'));
      const expenseColor = cards[2].query(By.css('.stub-color')).nativeElement.textContent.trim();
      expect(expenseColor).toBe('#ef4444');
    });

    it('should show "Total acumulado" subtitle on balance card', () => {
      const cards = fixture.debugElement.queryAll(By.css('app-metric-card'));
      const subtitle = cards[0].query(By.css('.stub-subtitle')).nativeElement.textContent.trim();
      expect(subtitle).toBe('Total acumulado');
    });

    it('should show month label as subtitle on income and expense cards', () => {
      const cards = fixture.debugElement.queryAll(By.css('app-metric-card'));
      const incomeSubtitle = cards[1].query(By.css('.stub-subtitle')).nativeElement.textContent.trim();
      const expenseSubtitle = cards[2].query(By.css('.stub-subtitle')).nativeElement.textContent.trim();

      // Both should have the same month label
      expect(incomeSubtitle).toBe(expenseSubtitle);
      // Should contain a capitalized month name in pt-BR and year (e.g., "Agosto de 2026")
      expect(incomeSubtitle).toMatch(/[A-Z][a-záéíóúãõê]+ (de )?\d{4}/);
    });
  });

  describe('error state', () => {
    it('should display error state when service fails', () => {
      dashboardServiceSpy.getDashboard.and.returnValue(
        throwError(() => new Error('Server error'))
      );

      fixture.detectChanges();

      const errorState = fixture.debugElement.query(By.css('.error-state'));
      expect(errorState).toBeTruthy();

      const errorMessage = fixture.debugElement.query(By.css('.error-message'));
      expect(errorMessage.nativeElement.textContent.trim()).toBe('Erro ao carregar dados');
    });

    it('should display "Tentar Novamente" button in error state', () => {
      dashboardServiceSpy.getDashboard.and.returnValue(
        throwError(() => new Error('Server error'))
      );

      fixture.detectChanges();

      const retryButton = fixture.debugElement.query(By.css('.error-state button'));
      expect(retryButton).toBeTruthy();
      expect(retryButton.nativeElement.textContent.trim()).toContain('Tentar Novamente');
    });

    it('should retry loading when retry button is clicked', () => {
      dashboardServiceSpy.getDashboard.and.returnValue(
        throwError(() => new Error('Server error'))
      );
      fixture.detectChanges();

      // Now reset and return success
      dashboardServiceSpy.getDashboard.and.returnValue(of(mockDashboardData));
      const retryButton = fixture.debugElement.query(By.css('.error-state button'));
      retryButton.nativeElement.click();
      fixture.detectChanges();

      const errorState = fixture.debugElement.query(By.css('.error-state'));
      expect(errorState).toBeNull();

      const cards = fixture.debugElement.queryAll(By.css('app-metric-card'));
      expect(cards.length).toBe(3);
    });

    it('should not display metric cards in error state', () => {
      dashboardServiceSpy.getDashboard.and.returnValue(
        throwError(() => new Error('Server error'))
      );
      fixture.detectChanges();

      const cards = fixture.debugElement.queryAll(By.css('app-metric-card'));
      expect(cards.length).toBe(0);
    });
  });

  describe('expenses chart', () => {
    it('should render chart component when expensesByType has data', () => {
      fixture.detectChanges();

      const chart = fixture.debugElement.query(By.css('app-expenses-chart'));
      expect(chart).toBeTruthy();

      const expensesCount = chart.query(By.css('.stub-expenses')).nativeElement.textContent.trim();
      expect(expensesCount).toBe('2');
    });

    it('should pass empty array to chart when expensesByType is empty', () => {
      dashboardServiceSpy.getDashboard.and.returnValue(of(emptyDashboardData));
      fixture.detectChanges();

      const chart = fixture.debugElement.query(By.css('app-expenses-chart'));
      expect(chart).toBeTruthy();

      const expensesCount = chart.query(By.css('.stub-expenses')).nativeElement.textContent.trim();
      expect(expensesCount).toBe('0');
    });

    it('should pass current month label to chart component', () => {
      fixture.detectChanges();

      const chart = fixture.debugElement.query(By.css('app-expenses-chart'));
      const monthLabel = chart.query(By.css('.stub-month')).nativeElement.textContent.trim();
      expect(monthLabel).toMatch(/[A-Z][a-záéíóúãõê]+ (de )?\d{4}/);
    });
  });

  describe('month selection', () => {
    it('should call service with current month on init', () => {
      fixture.detectChanges();

      const now = new Date();
      const expectedMonth = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;
      expect(dashboardServiceSpy.getDashboard).toHaveBeenCalledWith(expectedMonth);
    });

    it('should trigger new request when month is changed', () => {
      fixture.detectChanges();
      dashboardServiceSpy.getDashboard.calls.reset();
      dashboardServiceSpy.getDashboard.and.returnValue(of(emptyDashboardData));

      const newDate = new Date(2024, 2, 1); // March 2024
      const mockDatepicker = { close: jasmine.createSpy('close') };
      component.onMonthSelected(newDate, mockDatepicker);
      fixture.detectChanges();

      expect(dashboardServiceSpy.getDashboard).toHaveBeenCalledWith('2024-03');
      expect(mockDatepicker.close).toHaveBeenCalled();
    });

    it('should update displayed data after month change', () => {
      fixture.detectChanges();

      const newData: DashboardResponse = {
        currentBalance: 1000,
        monthlyIncome: 2000,
        monthlyExpense: 500,
        expensesByType: [{ typeName: 'Lazer', total: 500 }],
      };
      dashboardServiceSpy.getDashboard.and.returnValue(of(newData));

      const newDate = new Date(2024, 0, 1); // January 2024
      component.onMonthSelected(newDate, { close: () => {} });
      fixture.detectChanges();

      const cards = fixture.debugElement.queryAll(By.css('app-metric-card'));
      const values = cards.map(c => c.query(By.css('.stub-value')).nativeElement.textContent.trim());
      expect(values[0]).toBe('1000');
      expect(values[1]).toBe('2000');
      expect(values[2]).toBe('500');
    });

    it('should show loading state while fetching new month data', () => {
      fixture.detectChanges();

      const subject = new Subject<DashboardResponse>();
      dashboardServiceSpy.getDashboard.and.returnValue(subject.asObservable());

      const newDate = new Date(2024, 5, 1); // June 2024
      component.onMonthSelected(newDate, { close: () => {} });
      fixture.detectChanges();

      const skeletonCards = fixture.debugElement.queryAll(By.css('.skeleton-card'));
      expect(skeletonCards.length).toBe(3);

      // Complete the request
      subject.next(mockDashboardData);
      subject.complete();
      fixture.detectChanges();

      const skeletonAfter = fixture.debugElement.queryAll(By.css('.skeleton-card'));
      expect(skeletonAfter.length).toBe(0);
    });
  });
});
