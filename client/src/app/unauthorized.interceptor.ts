import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './services/auth.service';

export const unauthorizedInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401) {
        const isAdminRoute =
          req.url.includes('/api/admin/') && !req.url.includes('/api/admin/auth/login');

        auth.clearSession();

        if (isAdminRoute && !router.url.startsWith('/admin/login')) {
          void router.navigate(['/admin/login']);
        }
      }

      return throwError(() => error);
    }),
  );
};
