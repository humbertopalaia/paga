import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { PaginatedResponse } from '../../core/models';
import { ExpenseType, CreateExpenseTypeRequest, UpdateExpenseTypeRequest, ExpenseTypeListParams } from './expense-type.model';

@Injectable({ providedIn: 'root' })
export class ExpenseTypeService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  getExpenseTypes(params: ExpenseTypeListParams): Observable<PaginatedResponse<ExpenseType>> {
    let httpParams = new HttpParams()
      .set('pageNumber', params.pageNumber)
      .set('pageSize', params.pageSize);

    if (params.name) {
      httpParams = httpParams.set('name', params.name);
    }

    return this.http.get<PaginatedResponse<ExpenseType>>(`${this.apiUrl}/expense-types`, { params: httpParams });
  }

  getExpenseType(id: number): Observable<ExpenseType> {
    return this.http.get<ExpenseType>(`${this.apiUrl}/expense-types/${id}`);
  }

  createExpenseType(data: CreateExpenseTypeRequest): Observable<ExpenseType> {
    return this.http.post<ExpenseType>(`${this.apiUrl}/expense-types`, data);
  }

  updateExpenseType(id: number, data: UpdateExpenseTypeRequest): Observable<ExpenseType> {
    return this.http.put<ExpenseType>(`${this.apiUrl}/expense-types/${id}`, data);
  }

  deleteExpenseType(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/expense-types/${id}`);
  }
}
