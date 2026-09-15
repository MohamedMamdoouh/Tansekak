import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import {
  Observable,
  catchError,
  map,
  of,
  shareReplay,
  tap,
} from 'rxjs';
import { ApiService } from './api.service';
import { AuthUser } from '../models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private api = inject(ApiService);
  private router = inject(Router);

  private readonly userSignal = signal<AuthUser | null>(null);
  private sessionRequest$?: Observable<void>;
  readonly currentUser = this.userSignal.asReadonly();
  readonly isAdmin = computed(
    () => this.userSignal()?.role === 'Administrator',
  );

  loadSession(): Observable<void> {
    if (!this.sessionRequest$) {
      this.sessionRequest$ = this.fetchSession().pipe(shareReplay(1));
    }

    return this.sessionRequest$;
  }

  ensureAdminSession(): Observable<boolean> {
    this.sessionRequest$ = undefined;
    return this.fetchSession().pipe(map(() => this.isAdmin()));
  }

  clearSession(): void {
    this.userSignal.set(null);
    this.sessionRequest$ = undefined;
  }

  private fetchSession(): Observable<void> {
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
      tap((user) => {
        this.userSignal.set(user);
        this.sessionRequest$ = undefined;
      }),
      map(() => undefined),
    );
  }

  logout(): Observable<void> {
    return this.api.logout().pipe(
      catchError(() => of(null)),
      tap(() => this.clearSession()),
      map(() => undefined),
    );
  }

  logoutAndRedirect(home = false): void {
    this.logout().subscribe({
      next: () => this.router.navigate([home ? '/' : '/admin/login']),
      error: () => {
        this.clearSession();
        this.router.navigate([home ? '/' : '/admin/login']);
      },
    });
  }
}
