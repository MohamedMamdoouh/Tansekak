import { Injectable, inject, signal } from '@angular/core';
import { Observable, shareReplay, tap } from 'rxjs';
import { AdmissionYear } from '../models';
import { ApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class AdmissionYearStore {
  private api = inject(ApiService);
  private yearsCache$?: Observable<AdmissionYear[]>;
  private currentYearCache$?: Observable<AdmissionYear>;
  readonly years = signal<AdmissionYear[]>([]);
  readonly currentYear = signal<AdmissionYear | null>(null);

  loadYears(): Observable<AdmissionYear[]> {
    if (!this.yearsCache$) {
      this.yearsCache$ = this.api.getAdmissionYears().pipe(
        tap((years) => {
          this.years.set(years);
          const current = years.find((year) => year.isCurrent) ?? null;
          this.currentYear.set(current);
        }),
        shareReplay(1),
      );
    }

    return this.yearsCache$;
  }

  loadCurrentYear(): Observable<AdmissionYear> {
    if (!this.currentYearCache$) {
      this.currentYearCache$ = this.api.getCurrentAdmissionYear().pipe(
        tap((year) => this.currentYear.set(year)),
        shareReplay(1),
      );
    }

    return this.currentYearCache$;
  }

  refreshCurrentYear(): Observable<AdmissionYear> {
    this.currentYearCache$ = undefined;
    return this.loadCurrentYear();
  }

  refreshYears(): Observable<AdmissionYear[]> {
    this.yearsCache$ = undefined;
    this.currentYearCache$ = undefined;
    return this.loadYears();
  }

  getMaximumScoreForYear(year: number, fallback: number): number {
    const match = this.years().find((item) => item.year === year)
      ?? this.currentYear();
    return match?.maximumScore ?? fallback;
  }
}
