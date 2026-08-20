import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';

import { UserService } from './user.service';
import { environment } from '../../../environments/environment';
import { User, CreateUserRequest, UpdateUserRequest, UserListParams } from './user.model';
import { PaginatedResponse } from '../../core/models';

describe('UserService', () => {
  let service: UserService;
  let httpMock: HttpTestingController;
  const apiUrl = environment.apiUrl;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
      ]
    });

    service = TestBed.inject(UserService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  describe('getUsers', () => {
    const mockResponse: PaginatedResponse<User> = {
      items: [
        { id: '1', name: 'João', email: 'joao@test.com', createdAt: '2024-01-15T10:00:00Z' }
      ],
      pageNumber: 1,
      pageSize: 10,
      totalCount: 1,
      totalPages: 1
    };

    it('should send GET with pageNumber and pageSize params', () => {
      const params: UserListParams = { pageNumber: 2, pageSize: 15 };

      service.getUsers(params).subscribe(response => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(r =>
        r.url === `${apiUrl}/users` && r.method === 'GET'
      );
      expect(req.request.params.get('pageNumber')).toBe('2');
      expect(req.request.params.get('pageSize')).toBe('15');
      req.flush(mockResponse);
    });

    it('should include name and email params when provided', () => {
      const params: UserListParams = { pageNumber: 1, pageSize: 10, name: 'João', email: 'joao@test.com' };

      service.getUsers(params).subscribe(response => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(r =>
        r.url === `${apiUrl}/users` && r.method === 'GET'
      );
      expect(req.request.params.get('pageNumber')).toBe('1');
      expect(req.request.params.get('pageSize')).toBe('10');
      expect(req.request.params.get('name')).toBe('João');
      expect(req.request.params.get('email')).toBe('joao@test.com');
      req.flush(mockResponse);
    });

    it('should not include name and email params when empty', () => {
      const params: UserListParams = { pageNumber: 1, pageSize: 10 };

      service.getUsers(params).subscribe(response => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(r =>
        r.url === `${apiUrl}/users` && r.method === 'GET'
      );
      expect(req.request.params.has('name')).toBeFalse();
      expect(req.request.params.has('email')).toBeFalse();
      req.flush(mockResponse);
    });
  });

  describe('getUser', () => {
    it('should send GET to /api/users/{id}', () => {
      const mockUser: User = { id: 'abc-123', name: 'Maria', email: 'maria@test.com', createdAt: '2024-02-20T08:30:00Z' };

      service.getUser('abc-123').subscribe(user => {
        expect(user).toEqual(mockUser);
      });

      const req = httpMock.expectOne(`${apiUrl}/users/abc-123`);
      expect(req.request.method).toBe('GET');
      req.flush(mockUser);
    });
  });

  describe('createUser', () => {
    it('should send POST to /api/users with correct payload', () => {
      const payload: CreateUserRequest = { name: 'Novo Usuário', email: 'novo@test.com', password: 'Senha123!' };
      const mockResponse: User = { id: 'new-id', name: 'Novo Usuário', email: 'novo@test.com', createdAt: '2024-03-01T12:00:00Z' };

      service.createUser(payload).subscribe(user => {
        expect(user).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(`${apiUrl}/users`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(payload);
      req.flush(mockResponse);
    });
  });

  describe('updateUser', () => {
    it('should send PUT to /api/users/{id} with correct payload', () => {
      const payload: UpdateUserRequest = { name: 'Nome Atualizado', email: 'atualizado@test.com', password: 'NovaSenha!' };
      const mockResponse: User = { id: 'abc-123', name: 'Nome Atualizado', email: 'atualizado@test.com', createdAt: '2024-01-15T10:00:00Z' };

      service.updateUser('abc-123', payload).subscribe(user => {
        expect(user).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(`${apiUrl}/users/abc-123`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(payload);
      req.flush(mockResponse);
    });
  });

  describe('deleteUser', () => {
    it('should send DELETE to /api/users/{id}', () => {
      service.deleteUser('abc-123').subscribe();

      const req = httpMock.expectOne(`${apiUrl}/users/abc-123`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
    });
  });
});
