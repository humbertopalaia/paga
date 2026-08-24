import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';

import { DashboardService } from './dashboard.service';
import { environment } from '../../../environments/environment';
import { DashboardResponse } from './dashboard.model';

describe('DashboardService', () => {
  let service: DashboardService;
  let httpMock: HttpTestingController;
  const apiUrl = environment.apiUrl;

  const mockResponse: DashboardResponse = {
    currentBalance: 3500.50,
    monthlyIncome: 5000,
    monthlyExpense: 1500,
    expensesByType: [
      { typeName: 'Alimentação', total: 800 },
      { typeName: 'Transporte', total: 700 }
    ]
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
      ]
    });

    service = TestBed.inject(DashboardService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  describe('getDashboard', () => {
    it('should send GET to /dashboard without params when month is undefined', () => {
      service.getDashboard().subscribe(response => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(r =>
        r.url === `${apiUrl}/dashboard` && r.method === 'GET'
      );
      expect(req.request.method).toBe('GET');
      expect(req.request.params.has('month')).toBeFalse();
      req.flush(mockResponse);
    });

    it('should send GET to /dashboard with month param when provided', () => {
      service.getDashboard('2024-03').subscribe(response => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(r =>
        r.url === `${apiUrl}/dashboard` && r.method === 'GET'
      );
      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('month')).toBe('2024-03');
      req.flush(mockResponse);
    });

    it('should return DashboardResponse with correct shape', () => {
      service.getDashboard('2024-01').subscribe(response => {
        expect(response.currentBalance).toBe(3500.50);
        expect(response.monthlyIncome).toBe(5000);
        expect(response.monthlyExpense).toBe(1500);
        expect(response.expensesByType.length).toBe(2);
        expect(response.expensesByType[0].typeName).toBe('Alimentação');
        expect(response.expensesByType[0].total).toBe(800);
      });

      const req = httpMock.expectOne(r =>
        r.url === `${apiUrl}/dashboard` && r.method === 'GET'
      );
      req.flush(mockResponse);
    });
  });
});
