import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { CurrencyPipe } from '@angular/common';

@Component({
  selector: 'app-metric-card',
  standalone: true,
  imports: [CurrencyPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './metric-card.component.html',
  styleUrl: './metric-card.component.scss',
})
export class MetricCardComponent {
  title = input.required<string>();
  value = input.required<number>();
  valueColor = input.required<string>();
  subtitle = input<string>();
}
