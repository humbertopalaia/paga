import {
  HttpInterceptorFn,
  HttpRequest,
  HttpHandlerFn,
  HttpErrorResponse,
  HttpEvent,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { BehaviorSubject, Observable, throwError } from 'rxjs';
import { catchError, filter, switchMap, take } from 'rxjs/operators';

import { AuthService } from './auth.service';

let isRefreshing = false;
let refreshSubject = new BehaviorSubject<string | null>(null);

export const authInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
): Observable<HttpEvent<unknown>> => {
  const authService = inject(AuthService);

  if (req.url.includes('/auth/login') || req.url.includes('/auth/refresh')) {
    return next(req);
  }

  const token = authService.getAccessToken();
  const authReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        return handle401(authService, req, next);
      }
      return throwError(() => error);
    })
  );
};

function handle401(
  authService: AuthService,
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
): Observable<HttpEvent<unknown>> {
  if (!isRefreshing) {
    isRefreshing = true;
    refreshSubject.next(null);

    return authService.refresh().pipe(
      switchMap(() => {
        isRefreshing = false;
        const newToken = authService.getAccessToken()!;
        refreshSubject.next(newToken);
        return next(req.clone({ setHeaders: { Authorization: `Bearer ${newToken}` } }));
      }),
      catchError(err => {
        isRefreshing = false;
        authService.logout();
        return throwError(() => err);
      })
    );
  }

  return refreshSubject.pipe(
    filter((token): token is string => token !== null),
    take(1),
    switchMap(token =>
      next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }))
    )
  );
}
