import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { EMPTY, switchMap } from 'rxjs';
import { ApiService } from '../../services/api.service';
import { AdmissionResult, PredictResponse } from '../../models';
import { PREDICT_RESULTS_PAGE_SIZE } from '../../constants/pagination.constants';
import { canonicalizeTrack, getTrackLabel } from '../../utils/track-label.util';
import {
  isValidResultsQuery,
  parseResultsScore,
} from '../../utils/results-query.util';
import { resolveApiError } from '../../utils/api-error.util';
import { HttpErrorResponse } from '@angular/common/http';

@Component({
  selector: 'app-results',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './results.component.html',
  styleUrl: './results.component.scss',
})
export class ResultsComponent {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private api = inject(ApiService);
  private destroyRef = inject(DestroyRef);

  track = '';
  score = 0;
  page = 1;
  pageSize = PREDICT_RESULTS_PAGE_SIZE;
  totalCount = 0;
  hasMore = false;
  search = '';
  loading = false;
  loadingMore = false;
  error = '';
  allResults: AdmissionResult[] = [];
  private fetchGeneration = 0;

  constructor() {
    this.route.queryParams
      .pipe(
        switchMap((params) => {
          this.track = canonicalizeTrack(params['track'] ?? '');
          this.score = parseResultsScore(params['score']);

          if (!isValidResultsQuery(this.track, this.score)) {
            this.router.navigate(['/predict'], {
              queryParams: { error: 'missing' },
            });
            return EMPTY;
          }

          this.resetAndFetch();
          return EMPTY;
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe();
  }

  private resetAndFetch(): void {
    this.page = 1;
    this.totalCount = 0;
    this.hasMore = false;
    this.allResults = [];
    this.search = '';
    this.error = '';
    this.fetchGeneration += 1;
    this.fetchPage(this.fetchGeneration);
  }

  loadMore(): void {
    if (this.loadingMore || !this.hasMoreToLoad()) return;
    this.page += 1;
    this.fetchPage(this.fetchGeneration);
  }

  loadAllRemaining(): void {
    if (this.loadingMore || !this.hasMoreToLoad()) return;
    this.loadingMore = true;
    this.fetchUntilComplete(this.fetchGeneration);
  }

  private fetchPage(generation: number): void {
    const isFirstPage = this.page === 1;
    if (isFirstPage) {
      this.loading = true;
    } else {
      this.loadingMore = true;
    }
    this.error = '';

    this.api
      .predict({
        track: this.track,
        score: this.score,
        page: this.page,
        pageSize: this.pageSize,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          if (generation !== this.fetchGeneration) return;
          this.applyPage(res, isFirstPage);
        },
        error: (err) => {
          if (generation !== this.fetchGeneration) return;
          this.handleError(err);
        },
      });
  }

  private fetchUntilComplete(generation: number): void {
    if (generation !== this.fetchGeneration || !this.hasMoreToLoad()) {
      this.loadingMore = false;
      return;
    }

    this.page += 1;
    this.api
      .predict({
        track: this.track,
        score: this.score,
        page: this.page,
        pageSize: this.pageSize,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          if (generation !== this.fetchGeneration) return;
          this.applyPage(res, false);
          if (this.hasMoreToLoad()) {
            this.fetchUntilComplete(generation);
          } else {
            this.loadingMore = false;
          }
        },
        error: (err) => {
          if (generation !== this.fetchGeneration) return;
          this.handleError(err);
        },
      });
  }

  private applyPage(res: PredictResponse, replace: boolean): void {
    this.totalCount = res.totalCount;
    this.hasMore = res.hasMore;
    this.allResults = replace
      ? [...res.results]
      : [...this.allResults, ...res.results];
    this.loading = false;
    this.loadingMore = false;
  }

  private handleError(err: HttpErrorResponse): void {
    this.error = resolveApiError(err, 'تعذر تحميل النتائج.');
    this.loading = false;
    this.loadingMore = false;
  }

  hasMoreToLoad(): boolean {
    return this.hasMore || this.allResults.length < this.totalCount;
  }

  remainingCount(): number {
    return Math.max(0, this.totalCount - this.allResults.length);
  }

  loadMoreLabel(): string {
    if (this.loadingMore) return 'جاري التحميل...';
    const remaining = this.remainingCount();
    return remaining > 0
      ? `تحميل المزيد (${remaining} متبقية)`
      : 'تحميل المزيد';
  }

  summaryCount(): number {
    if (this.search.trim()) {
      return this.displayedResults().length;
    }
    return this.totalCount;
  }

  summaryLabel(): string {
    if (this.search.trim()) {
      return `كلية في البحث (من ${this.totalCount} متاحة)`;
    }
    return 'كلية متاحة';
  }

  displayedResults(): AdmissionResult[] {
    return this.filtered(this.allResults);
  }

  resultKey(item: AdmissionResult): string {
    return `${item.university.nameAr}|${item.faculty.nameAr}`;
  }

  filtered(items: AdmissionResult[]): AdmissionResult[] {
    if (!this.search.trim()) return items;
    const q = this.search.trim();
    return items.filter(
      (x) => x.university.nameAr.includes(q) || x.faculty.nameAr.includes(q),
    );
  }

  trackLabel(track: string): string {
    return getTrackLabel(track);
  }

  facultyLabel(name: string): string {
    const trimmed = name.trim();
    return trimmed.startsWith('كلية') ? trimmed : `كلية ${trimmed}`;
  }
}
