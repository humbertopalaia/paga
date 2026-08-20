import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';

import { ExpenseTypeService } from './expense-type.service';
import { environment } from '../../../environments/environment';
import { ExpenseType, CreateExpenseTypeRequest, UpdateExpenseTypeRequest, ExpenseTypeListParams } from './expense-type.model';
import { PaginatedResponse } from '../../core/models';

describe('ExpenseTypeService', () => {
  let service: ExpenseTypeService;
  let httpMock: HttpTestingController;
  const apiUrl = environment.apiUrl;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
      ]
    });

    service = TestBed.inject(ExpenseTypeService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  describe('getExpenseTypes', () => {
    const mockResponse: PaginatedResponse<ExpenseType> = {
      items: [
        { id: 1, name: 'Alimentação' },
        { id: 2, name: 'Transporte' }
      ],
      pageNumber: 1,
      pageSize: 10,
      totalCount: 2,
      totalPages: 1
    };

    it('should send GET with pageNumber and pageSize params', () => {
      const params: ExpenseTypeListParams = { pageNumber: 2, pageSize: 15 };

      service.getExpenseTypes(params).subscribe(response => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(r =>
        r.url === `${apiUrl}/expense-types` && r.method === 'GET'
      );
      expect(req.request.params.get('pageNumber')).toBe('2');
      expect(req.request.params.get('pageSize')).toBe('15');
      req.flush(mockResponse);
    });

    it('should include name param when provided', () => {
      const params: ExpenseTypeListParams = { pageNumber: 1, pageSize: 10, name: 'Alimentação' };

      service.getExpenseTypes(params).subscribe(response => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(r =>
        r.url === `${apiUrl}/expense-types` && r.method === 'GET'
      );
      expect(req.request.params.get('pageNumber')).toBe('1');
      expect(req.request.params.get('pageSize')).toBe('10');
      expect(req.request.params.get('name')).toBe('Alimentação');
      req.flush(mockResponse);
    });

    it('should not include name param when not provided', () => {
      const params: ExpenseTypeListParams = { pageNumber: 1, pageSize: 10 };

      service.getExpenseTypes(params).subscribe(response => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(r =>
        r.url === `${apiUrl}/expense-types` && r.method === 'GET'
      );
      expect(req.request.params.has('name')).toBeFalse();
      req.flush(mockResponse);
    });
  });

  describe('getExpenseType', () => {
    it('should send GET to /expense-types/{id}', () => {
      const mockExpenseType: ExpenseType = { id: 5, name: 'Lazer' };

      service.getExpenseType(5).subscribe(result => {
        expect(result).toEqual(mockExpenseType);
      });

      const req = httpMock.expectOne(`${apiUrl}/expense-types/5`);
      expect(req.request.method).toBe('GET');
      req.flush(mockExpenseType);
    });
  });

  describe('createExpenseType', () => {
    it('should send POST to /expense-types with correct payload', () => {
      const payload: CreateExpenseTypeRequest = { name: 'Educação' };
      const mockResponse: ExpenseType = { id: 3, name: 'Educação' };

      service.createExpenseType(payload).subscribe(result => {
        expect(result).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(`${apiUrl}/expense-types`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(payload);
      req.flush(mockResponse);
    });
  });

  describe('updateExpenseType', () => {
    it('should send PUT to /expense-types/{id} with correct payload', () => {
      const payload: UpdateExpenseTypeRequest = { name: 'Saúde e Bem-estar' };
      const mockResponse: ExpenseType = { id: 4, name: 'Saúde e Bem-estar' };

      service.updateExpenseType(4, payload).subscribe(result => {
        expect(result).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(`${apiUrl}/expense-types/4`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(payload);
      req.flush(mockResponse);
    });
  });

  describe('deleteExpenseType', () => {
    it('should send DELETE to /expense-types/{id}', () => {
      service.deleteExpenseType(7).subscribe();

      const req = httpMock.expectOne(`${apiUrl}/expense-types/7`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
    });
  });
});
