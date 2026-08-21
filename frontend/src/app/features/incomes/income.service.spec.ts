import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';

import { IncomeService } from './income.service';
import { environment } from '../../../environments/environment';
import { Income, CreateIncomeRequest, UpdateIncomeRequest, IncomeListParams } from './income.model';
import { PaginatedResponse } from '../../core/models';

describe('IncomeService', () => {
  let service: IncomeService;
  let httpMock: HttpTestingController;
  const apiUrl = environment.apiUrl;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
      ]
    });

    service = TestBed.inject(IncomeService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  describe('getIncomes', () => {
    const mockResponse: PaginatedResponse<Income> = {
      items: [
        { id: 1, date: '2024-03-15', description: 'Salário', value: 5000, isRecurring: true, frequency: 'monthly' },
        { id: 2, date: '2024-03-20', description: 'Freelance', value: 1500, isRecurring: false, frequency: null }
      ],
      pageNumber: 1,
      pageSize: 10,
      totalCount: 2,
      totalPages: 1
    };

    it('should send GET with only pageNumber and pageSize when no filters provided', () => {
      const params: IncomeListParams = { pageNumber: 1, pageSize: 10 };

      service.getIncomes(params).subscribe(response => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(r =>
        r.url === `${apiUrl}/incomes` && r.method === 'GET'
      );
      expect(req.request.params.get('pageNumber')).toBe('1');
      expect(req.request.params.get('pageSize')).toBe('10');
      expect(req.request.params.has('dateFrom')).toBeFalse();
      expect(req.request.params.has('dateTo')).toBeFalse();
      expect(req.request.params.has('description')).toBeFalse();
      expect(req.request.params.has('isRecurring')).toBeFalse();
      req.flush(mockResponse);
    });

    it('should include all filter params when all provided', () => {
      const params: IncomeListParams = {
        pageNumber: 2,
        pageSize: 15,
        dateFrom: '2024-01-01',
        dateTo: '2024-03-31',
        description: 'Salário',
        isRecurring: true
      };

      service.getIncomes(params).subscribe(response => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(r =>
        r.url === `${apiUrl}/incomes` && r.method === 'GET'
      );
      expect(req.request.params.get('pageNumber')).toBe('2');
      expect(req.request.params.get('pageSize')).toBe('15');
      expect(req.request.params.get('dateFrom')).toBe('2024-01-01');
      expect(req.request.params.get('dateTo')).toBe('2024-03-31');
      expect(req.request.params.get('description')).toBe('Salário');
      expect(req.request.params.get('isRecurring')).toBe('true');
      req.flush(mockResponse);
    });

    it('should include only dateFrom param when provided alone', () => {
      const params: IncomeListParams = { pageNumber: 1, pageSize: 10, dateFrom: '2024-02-01' };

      service.getIncomes(params).subscribe(response => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(r =>
        r.url === `${apiUrl}/incomes` && r.method === 'GET'
      );
      expect(req.request.params.get('dateFrom')).toBe('2024-02-01');
      expect(req.request.params.has('dateTo')).toBeFalse();
      expect(req.request.params.has('description')).toBeFalse();
      expect(req.request.params.has('isRecurring')).toBeFalse();
      req.flush(mockResponse);
    });

    it('should include isRecurring=true when filter is true', () => {
      const params: IncomeListParams = { pageNumber: 1, pageSize: 10, isRecurring: true };

      service.getIncomes(params).subscribe(response => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(r =>
        r.url === `${apiUrl}/incomes` && r.method === 'GET'
      );
      expect(req.request.params.get('isRecurring')).toBe('true');
      req.flush(mockResponse);
    });

    it('should include isRecurring=false when filter is false', () => {
      const params: IncomeListParams = { pageNumber: 1, pageSize: 10, isRecurring: false };

      service.getIncomes(params).subscribe(response => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(r =>
        r.url === `${apiUrl}/incomes` && r.method === 'GET'
      );
      expect(req.request.params.get('isRecurring')).toBe('false');
      req.flush(mockResponse);
    });

    it('should include only dateTo param when provided alone', () => {
      const params: IncomeListParams = { pageNumber: 1, pageSize: 10, dateTo: '2024-12-31' };

      service.getIncomes(params).subscribe(response => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(r =>
        r.url === `${apiUrl}/incomes` && r.method === 'GET'
      );
      expect(req.request.params.has('dateFrom')).toBeFalse();
      expect(req.request.params.get('dateTo')).toBe('2024-12-31');
      req.flush(mockResponse);
    });

    it('should include only description param when provided alone', () => {
      const params: IncomeListParams = { pageNumber: 1, pageSize: 10, description: 'Freelance' };

      service.getIncomes(params).subscribe(response => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(r =>
        r.url === `${apiUrl}/incomes` && r.method === 'GET'
      );
      expect(req.request.params.has('dateFrom')).toBeFalse();
      expect(req.request.params.has('dateTo')).toBeFalse();
      expect(req.request.params.get('description')).toBe('Freelance');
      expect(req.request.params.has('isRecurring')).toBeFalse();
      req.flush(mockResponse);
    });
  });

  describe('getIncome', () => {
    it('should send GET to /incomes/{id}', () => {
      const mockIncome: Income = {
        id: 3,
        date: '2024-03-10',
        description: 'Dividendos',
        value: 250.50,
        isRecurring: false,
        frequency: null
      };

      service.getIncome(3).subscribe(result => {
        expect(result).toEqual(mockIncome);
      });

      const req = httpMock.expectOne(`${apiUrl}/incomes/3`);
      expect(req.request.method).toBe('GET');
      req.flush(mockIncome);
    });
  });

  describe('createIncome', () => {
    it('should send POST to /incomes with correct payload', () => {
      const payload: CreateIncomeRequest = {
        date: '2024-04-01',
        description: 'Salário Abril',
        value: 6000,
        isRecurring: true,
        frequency: 'monthly'
      };
      const mockResponse: Income = {
        id: 4,
        date: '2024-04-01',
        description: 'Salário Abril',
        value: 6000,
        isRecurring: true,
        frequency: 'monthly'
      };

      service.createIncome(payload).subscribe(result => {
        expect(result).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(`${apiUrl}/incomes`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(payload);
      req.flush(mockResponse);
    });

    it('should send POST with non-recurring income payload', () => {
      const payload: CreateIncomeRequest = {
        date: '2024-05-15',
        description: 'Venda de equipamento',
        value: 800,
        isRecurring: false,
        frequency: null
      };
      const mockResponse: Income = {
        id: 5,
        date: '2024-05-15',
        description: 'Venda de equipamento',
        value: 800,
        isRecurring: false,
        frequency: null
      };

      service.createIncome(payload).subscribe(result => {
        expect(result).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(`${apiUrl}/incomes`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(payload);
      req.flush(mockResponse);
    });
  });

  describe('updateIncome', () => {
    it('should send PUT to /incomes/{id} with correct payload', () => {
      const payload: UpdateIncomeRequest = {
        date: '2024-04-01',
        description: 'Salário Atualizado',
        value: 6500,
        isRecurring: true,
        frequency: 'monthly'
      };
      const mockResponse: Income = {
        id: 4,
        date: '2024-04-01',
        description: 'Salário Atualizado',
        value: 6500,
        isRecurring: true,
        frequency: 'monthly'
      };

      service.updateIncome(4, payload).subscribe(result => {
        expect(result).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(`${apiUrl}/incomes/4`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(payload);
      req.flush(mockResponse);
    });
  });

  describe('deleteIncome', () => {
    it('should send DELETE to /incomes/{id}', () => {
      service.deleteIncome(7).subscribe();

      const req = httpMock.expectOne(`${apiUrl}/incomes/7`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
    });
  });
});
