import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap, map, catchError, EMPTY } from 'rxjs';

import { environment } from '../../../environments/environment';
import { LoginRequest, RefreshRequest, TokenResponse } from './auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly apiUrl = environment.apiUrl;
  private readonly REFRESH_TOKEN_KEY = 'paga-refresh-token';

  private accessTokenSignal = signal<string | null>(null);

  readonly isAuthenticated = computed(() => {
    const token = this.accessTokenSignal();
    if (!token) return false;
    return !this.isTokenExpired(token);
  });

  constructor() {
    this.bootstrap();
  }

  login(email: string, password: string): Observable<void> {
    const body: LoginRequest = { email, password };
    return this.http.post<TokenResponse>(`${this.apiUrl}/auth/login`, body).pipe(
      tap(response => this.storeTokens(response)),
      map(() => undefined as void)
    );
  }

  refresh(): Observable<void> {
    const refreshToken = this.getRefreshToken();
    const body: RefreshRequest = { refreshToken: refreshToken ?? '' };
    return this.http.post<TokenResponse>(`${this.apiUrl}/auth/refresh`, body).pipe(
      tap(response => this.storeTokens(response)),
      map(() => undefined as void)
    );
  }

  logout(): void {
    const refreshToken = this.getRefreshToken();
    if (refreshToken) {
      this.http.post(`${this.apiUrl}/auth/logout`, { refreshToken }).pipe(
        catchError(() => EMPTY)
      ).subscribe();
    }

    this.clearTokens();
    this.router.navigate(['/login']);
  }

  getAccessToken(): string | null {
    return this.accessTokenSignal();
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(this.REFRESH_TOKEN_KEY);
  }

  private storeTokens(response: TokenResponse): void {
    this.accessTokenSignal.set(response.accessToken);
    localStorage.setItem(this.REFRESH_TOKEN_KEY, response.refreshToken);
  }

  private clearTokens(): void {
    this.accessTokenSignal.set(null);
    localStorage.removeItem(this.REFRESH_TOKEN_KEY);
  }

  private isTokenExpired(token: string): boolean {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload.exp * 1000 < Date.now();
    } catch {
      return true;
    }
  }

  private bootstrap(): void {
    const refreshToken = this.getRefreshToken();
    if (refreshToken) {
      this.refresh().pipe(
        catchError(() => {
          this.clearTokens();
          return EMPTY;
        })
      ).subscribe();
    }
  }
}
