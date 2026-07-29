import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../api.service';
import { AdmissionResult, PredictResponse, TRACK_LABELS } from '../../models';

@Component({
  selector: 'app-results',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="container">
      <nav class="breadcrumb" aria-label="مسار التنقل">
        <a routerLink="/">الرئيسية</a>
        <span class="breadcrumb-sep">/</span>
        <a routerLink="/predict">اعرف كليتك</a>
        <span class="breadcrumb-sep">/</span>
        <span>النتائج</span>
      </nav>

      <section class="results-summary" aria-label="ملخص النتائج">
        <div class="results-score-block">
          <div class="results-score-value">{{ score }}</div>
          <div class="results-score-label">المجموع الكلي</div>
        </div>
        <div class="results-summary-text">
          <span class="track-badge">{{ trackLabel(track) }}</span>
          <h1>الكليات المتاحة لمجموعك</h1>
          <p>
            دي الكليات اللي مجموعك يسمح بيها حسب حدود القبول الرسمية — استخدمها
            كدليل وانت بتكتب رغباتك.
          </p>
        </div>
        @if (totalCount > 0 && !loading) {
          <div class="results-count-badge">
            <span class="results-count-num">{{ summaryCount() }}</span>
            <span>{{ summaryLabel() }}</span>
          </div>
        }
      </section>

      <div class="results-toolbar">
        <div class="search-box">
          <span class="search-box-icon" aria-hidden="true">
            <svg
              width="18"
              height="18"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="2"
            >
              <circle cx="11" cy="11" r="7" />
              <path d="M20 20l-3-3" />
            </svg>
          </span>
          <input
            [(ngModel)]="search"
            placeholder="ابحث باسم الجامعة أو الكلية..."
            aria-label="بحث في النتائج"
          />
        </div>
        <a routerLink="/predict" class="results-back">
          <svg
            width="16"
            height="16"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2.5"
            aria-hidden="true"
          >
            <path d="M19 12H5M12 19l-7-7 7-7" />
          </svg>
          تعديل المجموع
        </a>
      </div>

      @if (search.trim() && hasMoreToLoad()) {
        <p class="search-note">
          البحث حاليا على {{ allResults.length }} كلية محملة فقط. اعرض كل
          الكليات المتاحة للبحث في القائمة كاملة.
        </p>
      }

      @if (loading) {
        <div class="results-loading" aria-label="جاري التحميل">
          @for (i of [1, 2, 3, 4, 5, 6]; track i) {
            <div class="skeleton-card"></div>
          }
        </div>
      }

      @if (error) {
        <div
          class="disclaimer-box"
          style="border-color: #fecaca; background: #fef2f2; color: #991b1b"
        >
          {{ error }}
        </div>
      }

      @if (!loading && allResults.length > 0) {
        <div class="results-grid">
          @for (item of displayedResults(); track resultKey(item)) {
            <article class="result-card">
              <div class="result-card-icon" aria-hidden="true">
                <svg
                  width="22"
                  height="22"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  stroke-width="2"
                >
                  <path d="M12 14l9-5-9-5-9 5 9 5z" />
                  <path
                    d="M12 14l6.16-3.422a12.083 12.083 0 01.665 6.479A11.952 11.952 0 0012 20.055a11.952 11.952 0 00-6.824-2.998 12.078 12.078 0 01.665-6.479L12 14z"
                  />
                </svg>
              </div>
              <div class="result-card-body">
                <h3 class="result-card-faculty">
                  {{ facultyLabel(item.faculty.nameAr) }}
                </h3>
                <p class="result-card-university">
                  <svg
                    width="14"
                    height="14"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    stroke-width="2"
                    aria-hidden="true"
                  >
                    <path d="M3 21h18M5 21V7l7-4 7 4v14M9 21v-6h6v6" />
                  </svg>
                  {{ item.university.nameAr }}
                </p>
              </div>
            </article>
          }
        </div>

        @if (hasMoreToLoad()) {
          <div class="results-actions">
            <button
              class="btn btn-secondary btn-lg"
              (click)="loadMore()"
              [disabled]="loadingMore"
            >
              {{ loadMoreLabel() }}
            </button>
            <button
              class="btn btn-primary btn-lg"
              (click)="loadAllRemaining()"
              [disabled]="loadingMore"
            >
              {{ loadingMore ? 'جاري تحميل الكل...' : 'عرض كل الكليات' }}
            </button>
          </div>
        }

        <div class="results-footer-note">
          <div class="disclaimer-box">
            <strong>تنبيه:</strong> النتائج توقعية مش رسمية — القرار النهائي
            لموقع التنسيق الإلكتروني الرسمي.
          </div>
        </div>
      } @else if (!loading && !error && totalCount === 0) {
        <div class="results-empty">
          <div class="results-empty-icon" aria-hidden="true">🔍</div>
          <h3>مفيش نتائج</h3>
          <p>
            للأسف، مفيش كليات متاحة لمجموعك حسب البيانات الحالية. جرب تعديل
            مجموعك أو شوف صفحة افهم التنسيق لمعرفة خيارات تانية.
          </p>
          <div class="results-actions">
            <a routerLink="/predict" class="btn btn-primary">تعديل المجموع</a>
            <a routerLink="/guide" class="btn btn-secondary">افهم التنسيق</a>
          </div>
        </div>
      } @else if (!loading && allResults.length === 0 && search.trim()) {
        <div class="results-empty">
          <div class="results-empty-icon" aria-hidden="true">🔍</div>
          <h3>مفيش نتائج</h3>
          <p>
            مفيش كليات بتطابق "{{ search.trim() }}" — جرب كلمة تانية أو امسح
            البحث.
          </p>
          <button class="btn btn-secondary" (click)="search = ''">
            مسح البحث
          </button>
        </div>
      }
    </div>
  `,
  styles: [
    `
      .search-note {
        background: #eff6ff;
        color: #1e40af;
        padding: 0.75rem 1rem;
        border-radius: 0.5rem;
        margin-bottom: 1rem;
        font-size: 0.95rem;
      }
      .results-actions {
        display: flex;
        flex-wrap: wrap;
        gap: 0.75rem;
        justify-content: center;
        margin-top: 1.5rem;
      }
    `,
  ],
})
export class ResultsComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private api = inject(ApiService);

  track = '';
  score = 0;
  page = 1;
  pageSize = 20;
  totalCount = 0;
  hasMore = false;
  search = '';
  loading = false;
  loadingMore = false;
  error = '';
  allResults: AdmissionResult[] = [];

  ngOnInit(): void {
    this.route.queryParams.subscribe((params) => {
      this.track = params['track'] ?? '';
      this.score = Number(params['score'] ?? 0);
      if (!this.track || !this.score) {
        this.router.navigate(['/predict'], {
          queryParams: { error: 'missing' },
        });
        return;
      }
      this.resetAndFetch();
    });
  }

  private resetAndFetch(): void {
    this.page = 1;
    this.totalCount = 0;
    this.hasMore = false;
    this.allResults = [];
    this.search = '';
    this.error = '';
    this.fetchPage();
  }

  loadMore(): void {
    if (this.loadingMore || !this.hasMoreToLoad()) return;
    this.page += 1;
    this.fetchPage();
  }

  loadAllRemaining(): void {
    if (this.loadingMore || !this.hasMoreToLoad()) return;
    this.loadingMore = true;
    this.fetchUntilComplete();
  }

  private fetchPage(): void {
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
      .subscribe({
        next: (res) => this.applyPage(res, isFirstPage),
        error: (err) => this.handleError(err),
      });
  }

  private fetchUntilComplete(): void {
    if (!this.hasMoreToLoad()) {
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
      .subscribe({
        next: (res) => {
          this.applyPage(res, false);
          if (this.hasMoreToLoad()) {
            this.fetchUntilComplete();
          } else {
            this.loadingMore = false;
          }
        },
        error: (err) => this.handleError(err),
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

  private handleError(err: { error?: { message?: string } }): void {
    this.error = err.error?.message ?? 'تعذر تحميل النتائج.';
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
    return TRACK_LABELS[track] ?? track;
  }

  facultyLabel(name: string): string {
    const trimmed = name.trim();
    return trimmed.startsWith('كلية') ? trimmed : `كلية ${trimmed}`;
  }
}
