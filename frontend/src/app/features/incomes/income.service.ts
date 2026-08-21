import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { PaginatedResponse } from '../../core/models';
import { Income, CreateIncomeRequest, UpdateIncomeRequest, IncomeListParams } from './income.model';

@Injectable({ providedIn: 'root' })
export class IncomeService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  getIncomes(params: IncomeListParams): Observable<PaginatedResponse<Income>> {
    let httpParams = new HttpParams()
      .set('pageNumber', params.pageNumber)
      .set('pageSize', params.pageSize);

    if (params.dateFrom) {
      httpParams = httpParams.set('dateFrom', params.dateFrom);
    }

    if (params.dateTo) {
      httpParams = httpParams.set('dateTo', params.dateTo);
    }

    if (params.description) {
      httpParams = httpParams.set('description', params.description);
    }

    if (params.isRecurring !== undefined && params.isRecurring !== null) {
      httpParams = httpParams.set('isRecurring', String(params.isRecurring));
    }

    return this.http.get<PaginatedResponse<Income>>(`${this.apiUrl}/incomes`, { params: httpParams });
  }

  getIncome(id: number): Observable<Income> {
    return this.http.get<Income>(`${this.apiUrl}/incomes/${id}`);
  }

  createIncome(data: CreateIncomeRequest): Observable<Income> {
    return this.http.post<Income>(`${this.apiUrl}/incomes`, data);
  }

  updateIncome(id: number, data: UpdateIncomeRequest): Observable<Income> {
    return this.http.put<Income>(`${this.apiUrl}/incomes/${id}`, data);
  }

  deleteIncome(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/incomes/${id}`);
  }
}
