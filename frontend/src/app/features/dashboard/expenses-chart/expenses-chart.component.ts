import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { NgxChartsModule, Color, ScaleType } from '@swimlane/ngx-charts';
import { ExpenseByType } from '../dashboard.model';

@Component({
  selector: 'app-expenses-chart',
  standalone: true,
  imports: [NgxChartsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './expenses-chart.component.html',
  styleUrl: './expenses-chart.component.scss',
})
export class ExpensesChartComponent {
  expensesByType = input.required<ExpenseByType[]>();
  monthLabel = input.required<string>();

  chartData = computed(() =>
    this.expensesByType().map(item => ({ name: item.typeName, value: item.total }))
  );

  hasData = computed(() => this.expensesByType().length > 0);

  colorScheme: Color = {
    name: 'blue',
    selectable: true,
    group: ScaleType.Ordinal,
    domain: ['#3b82f6', '#60a5fa', '#93c5fd', '#1d4ed8', '#2563eb'],
  };

  formatCurrency(value: number): string {
    return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
  }
}
