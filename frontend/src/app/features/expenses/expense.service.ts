import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { PaginatedResponse } from '../../core/models';
import { Expense, CreateExpenseRequest, UpdateExpenseRequest, ExpenseListParams } from './expense.model';

@Injectable({ providedIn: 'root' })
export class ExpenseService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  getExpenses(params: ExpenseListParams): Observable<PaginatedResponse<Expense>> {
    let httpParams = new HttpParams()
      .set('pageNumber', params.pageNumber)
      .set('pageSize', params.pageSize);

    if (params.dueDateFrom) {
      httpParams = httpParams.set('dueDateFrom', params.dueDateFrom);
    }

    if (params.dueDateTo) {
      httpParams = httpParams.set('dueDateTo', params.dueDateTo);
    }

    if (params.expenseTypeId) {
      httpParams = httpParams.set('expenseTypeId', params.expenseTypeId.toString());
    }

    if (params.description) {
      httpParams = httpParams.set('description', params.description);
    }

    if (params.isRecurring !== undefined && params.isRecurring !== null) {
      httpParams = httpParams.set('isRecurring', String(params.isRecurring));
    }

    return this.http.get<PaginatedResponse<Expense>>(`${this.apiUrl}/expenses`, { params: httpParams });
  }

  getExpense(id: number): Observable<Expense> {
    return this.http.get<Expense>(`${this.apiUrl}/expenses/${id}`);
  }

  createExpense(data: CreateExpenseRequest): Observable<Expense> {
    return this.http.post<Expense>(`${this.apiUrl}/expenses`, data);
  }

  updateExpense(id: number, data: UpdateExpenseRequest): Observable<Expense> {
    return this.http.put<Expense>(`${this.apiUrl}/expenses/${id}`, data);
  }

  deleteExpense(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/expenses/${id}`);
  }
}
