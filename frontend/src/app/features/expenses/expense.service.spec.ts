import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';

import { ExpenseService } from './expense.service';
import { environment } from '../../../environments/environment';
import { Expense, CreateExpenseRequest, UpdateExpenseRequest, ExpenseListParams } from './expense.model';
import { PaginatedResponse } from '../../core/models';

describe('ExpenseService', () => {
  let service: ExpenseService;
  let httpMock: HttpTestingController;
  const apiUrl = environment.apiUrl;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
      ]
    });

    service = TestBed.inject(ExpenseService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  describe('getExpenses', () => {
    const mockResponse: PaginatedResponse<Expense> = {
      items: [
        { id: 1, dueDate: '2024-06-15', description: 'Internet', expenseTypeId: 3, expenseTypeName: 'Serviços', value: 120.50, isRecurring: true, frequency: 'monthly' },
        { id: 2, dueDate: '2024-06-20', description: 'Supermercado', expenseTypeId: 1, expenseTypeName: 'Alimentação', value: 450, isRecurring: false, frequency: null }
      ],
      pageNumber: 1,
      pageSize: 10,
      totalCount: 2,
      totalPages: 1
    };

    it('should send GET with only pageNumber and pageSize when no filters provided', () => {
      const params: ExpenseListParams = { pageNumber: 1, pageSize: 10 };

      service.getExpenses(params).subscribe(response => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(r =>
        r.url === `${apiUrl}/expenses` && r.method === 'GET'
      );
      expect(req.request.params.get('pageNumber')).toBe('1');
      expect(req.request.params.get('pageSize')).toBe('10');
      expect(req.request.params.has('dueDateFrom')).toBeFalse();
      expect(req.request.params.has('dueDateTo')).toBeFalse();
      expect(req.request.params.has('expenseTypeId')).toBeFalse();
      expect(req.request.params.has('description')).toBeFalse();
      expect(req.request.params.has('isRecurring')).toBeFalse();
      req.flush(mockResponse);
    });

    it('should include all filter params when all provided', () => {
      const params: ExpenseListParams = {
        pageNumber: 2,
        pageSize: 15,
        dueDateFrom: '2024-01-01',
        dueDateTo: '2024-06-30',
        expenseTypeId: 3,
        description: 'Internet',
        isRecurring: true
      };

      service.getExpenses(params).subscribe(response => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(r =>
        r.url === `${apiUrl}/expenses` && r.method === 'GET'
      );
      expect(req.request.params.get('pageNumber')).toBe('2');
      expect(req.request.params.get('pageSize')).toBe('15');
      expect(req.request.params.get('dueDateFrom')).toBe('2024-01-01');
      expect(req.request.params.get('dueDateTo')).toBe('2024-06-30');
      expect(req.request.params.get('expenseTypeId')).toBe('3');
      expect(req.request.params.get('description')).toBe('Internet');
      expect(req.request.params.get('isRecurring')).toBe('true');
      req.flush(mockResponse);
    });

    it('should include only dueDateFrom param when provided alone', () => {
      const params: ExpenseListParams = { pageNumber: 1, pageSize: 10, dueDateFrom: '2024-02-01' };

      service.getExpenses(params).subscribe(response => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(r =>
        r.url === `${apiUrl}/expenses` && r.method === 'GET'
      );
      expect(req.request.params.get('dueDateFrom')).toBe('2024-02-01');
      expect(req.request.params.has('dueDateTo')).toBeFalse();
      expect(req.request.params.has('expenseTypeId')).toBeFalse();
      expect(req.request.params.has('description')).toBeFalse();
      expect(req.request.params.has('isRecurring')).toBeFalse();
      req.flush(mockResponse);
    });

    it('should include only dueDateTo param when provided alone', () => {
      const params: ExpenseListParams = { pageNumber: 1, pageSize: 10, dueDateTo: '2024-12-31' };

      service.getExpenses(params).subscribe(response => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(r =>
        r.url === `${apiUrl}/expenses` && r.method === 'GET'
      );
      expect(req.request.params.has('dueDateFrom')).toBeFalse();
      expect(req.request.params.get('dueDateTo')).toBe('2024-12-31');
      expect(req.request.params.has('expenseTypeId')).toBeFalse();
      expect(req.request.params.has('description')).toBeFalse();
      expect(req.request.params.has('isRecurring')).toBeFalse();
      req.flush(mockResponse);
    });

    it('should include only expenseTypeId param when provided alone', () => {
      const params: ExpenseListParams = { pageNumber: 1, pageSize: 10, expenseTypeId: 5 };

      service.getExpenses(params).subscribe(response => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(r =>
        r.url === `${apiUrl}/expenses` && r.method === 'GET'
      );
      expect(req.request.params.has('dueDateFrom')).toBeFalse();
      expect(req.request.params.has('dueDateTo')).toBeFalse();
      expect(req.request.params.get('expenseTypeId')).toBe('5');
      expect(req.request.params.has('description')).toBeFalse();
      expect(req.request.params.has('isRecurring')).toBeFalse();
      req.flush(mockResponse);
    });

    it('should include only description param when provided alone', () => {
      const params: ExpenseListParams = { pageNumber: 1, pageSize: 10, description: 'Aluguel' };

      service.getExpenses(params).subscribe(response => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(r =>
        r.url === `${apiUrl}/expenses` && r.method === 'GET'
      );
      expect(req.request.params.has('dueDateFrom')).toBeFalse();
      expect(req.request.params.has('dueDateTo')).toBeFalse();
      expect(req.request.params.has('expenseTypeId')).toBeFalse();
      expect(req.request.params.get('description')).toBe('Aluguel');
      expect(req.request.params.has('isRecurring')).toBeFalse();
      req.flush(mockResponse);
    });

    it('should include isRecurring=true when filter is true', () => {
      const params: ExpenseListParams = { pageNumber: 1, pageSize: 10, isRecurring: true };

      service.getExpenses(params).subscribe(response => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(r =>
        r.url === `${apiUrl}/expenses` && r.method === 'GET'
      );
      expect(req.request.params.get('isRecurring')).toBe('true');
      req.flush(mockResponse);
    });

    it('should include isRecurring=false when filter is false', () => {
      const params: ExpenseListParams = { pageNumber: 1, pageSize: 10, isRecurring: false };

      service.getExpenses(params).subscribe(response => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(r =>
        r.url === `${apiUrl}/expenses` && r.method === 'GET'
      );
      expect(req.request.params.get('isRecurring')).toBe('false');
      req.flush(mockResponse);
    });
  });

  describe('getExpense', () => {
    it('should send GET to /expenses/{id}', () => {
      const mockExpense: Expense = {
        id: 1,
        dueDate: '2024-06-15',
        description: 'Internet',
        expenseTypeId: 3,
        expenseTypeName: 'Serviços',
        value: 120.50,
        isRecurring: true,
        frequency: 'monthly'
      };

      service.getExpense(1).subscribe(result => {
        expect(result).toEqual(mockExpense);
      });

      const req = httpMock.expectOne(`${apiUrl}/expenses/1`);
      expect(req.request.method).toBe('GET');
      req.flush(mockExpense);
    });
  });

  describe('createExpense', () => {
    it('should send POST to /expenses with correct payload', () => {
      const payload: CreateExpenseRequest = {
        dueDate: '2024-07-10',
        description: 'Energia Elétrica',
        expenseTypeId: 2,
        value: 230.75,
        isRecurring: true,
        frequency: 'monthly'
      };
      const mockResponse: Expense = {
        id: 10,
        dueDate: '2024-07-10',
        description: 'Energia Elétrica',
        expenseTypeId: 2,
        expenseTypeName: 'Moradia',
        value: 230.75,
        isRecurring: true,
        frequency: 'monthly'
      };

      service.createExpense(payload).subscribe(result => {
        expect(result).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(`${apiUrl}/expenses`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(payload);
      req.flush(mockResponse);
    });

    it('should send POST with non-recurring expense payload', () => {
      const payload: CreateExpenseRequest = {
        dueDate: '2024-08-05',
        description: 'Conserto do carro',
        expenseTypeId: 4,
        value: 1500,
        isRecurring: false,
        frequency: null
      };
      const mockResponse: Expense = {
        id: 11,
        dueDate: '2024-08-05',
        description: 'Conserto do carro',
        expenseTypeId: 4,
        expenseTypeName: 'Transporte',
        value: 1500,
        isRecurring: false,
        frequency: null
      };

      service.createExpense(payload).subscribe(result => {
        expect(result).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(`${apiUrl}/expenses`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(payload);
      req.flush(mockResponse);
    });
  });

  describe('updateExpense', () => {
    it('should send PUT to /expenses/{id} with correct payload', () => {
      const payload: UpdateExpenseRequest = {
        dueDate: '2024-07-15',
        description: 'Internet Atualizado',
        expenseTypeId: 3,
        value: 140,
        isRecurring: true,
        frequency: 'monthly'
      };
      const mockResponse: Expense = {
        id: 1,
        dueDate: '2024-07-15',
        description: 'Internet Atualizado',
        expenseTypeId: 3,
        expenseTypeName: 'Serviços',
        value: 140,
        isRecurring: true,
        frequency: 'monthly'
      };

      service.updateExpense(1, payload).subscribe(result => {
        expect(result).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(`${apiUrl}/expenses/1`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(payload);
      req.flush(mockResponse);
    });
  });

  describe('deleteExpense', () => {
    it('should send DELETE to /expenses/{id}', () => {
      service.deleteExpense(7).subscribe();

      const req = httpMock.expectOne(`${apiUrl}/expenses/7`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
    });
  });
});
