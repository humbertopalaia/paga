import {
  Component,
  ChangeDetectionStrategy,
  OnInit,
  inject,
  signal,
  computed,
} from '@angular/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatNativeDateModule, MAT_DATE_LOCALE } from '@angular/material/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

import { DashboardService } from './dashboard.service';
import { DashboardResponse } from './dashboard.model';
import { MetricCardComponent } from '../../shared/metric-card/metric-card.component';
import { ExpensesChartComponent } from './expenses-chart/expenses-chart.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    MatDatepickerModule,
    MatFormFieldModule,
    MatInputModule,
    MatNativeDateModule,
    MatButtonModule,
    MatIconModule,
    MetricCardComponent,
    ExpensesChartComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
  providers: [{ provide: MAT_DATE_LOCALE, useValue: 'pt-BR' }],
})
export class DashboardComponent implements OnInit {
  private readonly dashboardService = inject(DashboardService);

  loading = signal(true);
  error = signal(false);
  data = signal<DashboardResponse | null>(null);
  selectedMonth = signal<string>(this.getCurrentMonth());

  monthLabel = computed(() => this.getMonthLabel(this.selectedMonth()));

  selectedMonthDate = computed(() => {
    const [year, month] = this.selectedMonth().split('-').map(Number);
    return new Date(year, month - 1, 1);
  });

  balanceColor = computed(() => {
    const d = this.data();
    if (!d || d.currentBalance >= 0) return '#3b82f6';
    return '#ef4444';
  });

  ngOnInit(): void {
    this.loadData();
  }

  onMonthSelected(date: Date, datepicker: { close: () => void }): void {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    this.selectedMonth.set(`${year}-${month}`);
    datepicker.close();
    this.loadData();
  }

  retry(): void {
    this.loadData();
  }

  loadData(): void {
    this.loading.set(true);
    this.error.set(false);

    this.dashboardService.getDashboard(this.selectedMonth()).subscribe({
      next: (response) => {
        this.data.set(response);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(true);
        this.loading.set(false);
      },
    });
  }

  private getCurrentMonth(): string {
    const now = new Date();
    const year = now.getFullYear();
    const month = String(now.getMonth() + 1).padStart(2, '0');
    return `${year}-${month}`;
  }

  private getMonthLabel(yyyyMm: string): string {
    const [year, month] = yyyyMm.split('-').map(Number);
    const date = new Date(year, month - 1);
    const label = date.toLocaleDateString('pt-BR', { month: 'long', year: 'numeric' });
    return label.charAt(0).toUpperCase() + label.slice(1);
  }
}
