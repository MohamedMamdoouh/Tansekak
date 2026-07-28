import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, map, of, tap } from 'rxjs';
import { ApiService } from './api.service';
import { AuthUser } from './models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private api = inject(ApiService);
  private router = inject(Router);

  private readonly userSignal = signal<AuthUser | null>(null);
  readonly currentUser = this.userSignal.asReadonly();
  readonly isAdmin = computed(
    () => this.userSignal()?.role === 'Administrator',
  );

  loadSession(): Observable<void> {
    return this.api.me().pipe(
      tap((user) => this.userSignal.set(user)),
      catchError(() => {
        this.userSignal.set(null);
        return of(null);
      }),
      map(() => undefined),
    );
  }

  login(email: string, password: string): Observable<void> {
    return this.api.login(email, password).pipe(
      tap((user) => this.userSignal.set(user)),
      map(() => undefined),
    );
  }

  logout(): Observable<void> {
    return this.api.logout().pipe(
      tap(() => this.userSignal.set(null)),
      map(() => undefined),
    );
  }

  logoutAndRedirect(home = false): void {
    this.logout().subscribe(() => {
      this.router.navigate([home ? '/' : '/admin/login']);
    });
  }
}
