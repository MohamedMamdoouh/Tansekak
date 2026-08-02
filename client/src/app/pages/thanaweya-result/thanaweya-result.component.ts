import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ApiService } from '../../api.service';
import { StudentResult, TRACK_LABELS } from '../../models';
import {
  applyDigitsOnlyInput,
  digitsOnlyValidator,
} from '../../form-validators';
import { formatNumber } from '../../format-number.util';

const NOT_FOUND_MESSAGE = 'لم يتم العثور على نتيجة لهذا الرقم.';
const GENERIC_ERROR_MESSAGE = 'حدث خطأ أثناء البحث. حاول مرة أخرى لاحقا.';
const THANAWEYA_MAX_SCORE = 320;

@Component({
  selector: 'app-thanaweya-result',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="container thanaweya-page">
      <nav class="breadcrumb" aria-label="مسار التنقل">
        <a routerLink="/">الرئيسية</a>
        <span class="breadcrumb-sep">/</span>
        <span>نتيجة الثانوية</span>
      </nav>

      <section class="lookup-stage" [class.lookup-stage--compact]="result">
        <div class="lookup-stage-copy">
          <h1 class="lookup-title">نتيجة الثانوية برقم الجلوس</h1>
          @if (!result) {
            <p class="lookup-lead">
              اكتب رقم جلوسك وشوف نتيجتك — وبعدها اعرف الكليات المتاحة لمجموعك.
            </p>
          }
        </div>

        <form class="lookup-form" [formGroup]="form" (ngSubmit)="submit()">
          <label for="seatingNo" class="lookup-label">رقم الجلوس</label>
          <div class="lookup-input-wrap">
            <input
              id="seatingNo"
              type="text"
              inputmode="numeric"
              pattern="[0-9]*"
              formControlName="seatingNo"
              placeholder="1234567"
              autocomplete="off"
              (input)="onSeatingInput($event)"
            />
          </div>
          @if (
            form.get('seatingNo')?.invalid && form.get('seatingNo')?.touched
          ) {
            @if (form.get('seatingNo')?.errors?.['required']) {
              <small class="field-error">يرجى إدخال رقم الجلوس</small>
            } @else if (form.get('seatingNo')?.errors?.['digitsOnly']) {
              <small class="field-error"
                >رقم الجلوس يجب أن يحتوي على أرقام فقط</small
              >
            }
          }

          @if (error) {
            <div class="lookup-notice" role="status">{{ error }}</div>
          }

          <button
            class="btn btn-primary btn-lg lookup-submit"
            type="submit"
            [disabled]="loading"
          >
            @if (loading) {
              <span class="lookup-spinner" aria-hidden="true"></span>
              جاري البحث...
            } @else {
              اعرض النتيجة
            }
          </button>
        </form>
      </section>

      @if (result) {
        <section class="result-card" aria-label="نتيجة الطالب">
          <div class="result-profile">
            <div class="result-avatar" aria-hidden="true">
              {{ studentInitial(result.arabicName) }}
            </div>
            <div class="result-profile-body">
              <p class="result-eyebrow">
                الثانوية العامة
                <span class="result-year-badge num">{{ result.year }}</span>
              </p>
              <h2 class="result-name">{{ result.arabicName }}</h2>
              <p class="result-seating">
                <span class="result-seating-label">رقم الجلوس</span>
                <span class="result-seating-no num">{{
                  result.seatingNo
                }}</span>
              </p>
            </div>
          </div>

          <div
            class="result-score"
            [attr.aria-label]="
              'المجموع الكلي ' +
              result.totalDegree +
              ' من ' +
              thanaweyaMaxScore +
              '، النسبة ' +
              resultPercentage(result.totalDegree) +
              '%'
            "
          >
            <div
              class="result-score-ring"
              [style.--score-pct]="scoreProgress(result.totalDegree)"
            >
              <div class="result-score-ring-inner">
                <span class="result-score-value num">{{
                  result.totalDegree
                }}</span>
                <span class="result-score-max"
                  >/ {{ thanaweyaMaxScore }}</span
                >
              </div>
            </div>
            <div class="result-score-meta">
              <div class="result-score-head">
                <span class="result-score-label">المجموع الكلي</span>
                <span class="result-score-pct num"
                  >{{ resultPercentage(result.totalDegree) }}%</span
                >
              </div>
              <div class="result-score-bar" aria-hidden="true">
                <div
                  class="result-score-bar-fill"
                  [style.width.%]="scoreProgress(result.totalDegree)"
                ></div>
              </div>
            </div>
          </div>

          <div class="result-details">
            <div class="result-detail">
              <span class="result-detail-icon" aria-hidden="true">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              </span>
              <div class="result-detail-text">
                <span class="result-detail-label">حالة الطالب</span>
                <span class="result-detail-value">{{
                  result.studentCaseDesc
                }}</span>
              </div>
            </div>
            @if (result.track) {
              <div class="result-detail">
                <span class="result-detail-icon" aria-hidden="true">
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253" />
                  </svg>
                </span>
                <div class="result-detail-text">
                  <span class="result-detail-label">الشعبة</span>
                  <span class="result-detail-value">{{
                    trackLabel(result.track)
                  }}</span>
                </div>
              </div>
            }
          </div>

          @if (hasTrackRank(result)) {
            <div class="result-rank">
              <div class="result-rank-head">
                <span class="result-rank-label">الترتيب على الشعبة</span>
                @if (result.track) {
                  <span class="result-rank-track">{{
                    trackLabel(result.track)
                  }}</span>
                }
              </div>
              <div class="result-rank-stats">
                <div class="result-rank-stat result-rank-stat--primary">
                  <span class="result-rank-stat-value num">{{
                    fmt(result.trackRank)
                  }}</span>
                  <span class="result-rank-stat-label">ترتيبك</span>
                </div>
                <span class="result-rank-sep" aria-hidden="true">من</span>
                <div class="result-rank-stat">
                  <span class="result-rank-stat-value num">{{
                    fmt(result.trackTotalStudents)
                  }}</span>
                  <span class="result-rank-stat-label">طالب</span>
                </div>
              </div>
              <a class="result-rank-link" routerLink="/track-rank"
                >تفاصيل الترتيب ←</a
              >
            </div>
          }

          <footer class="result-footer">
            <a
              class="result-cta"
              [routerLink]="['/predict']"
              [queryParams]="predictQueryParams()"
            >
              <span>اعرف الكليات المتاحة لمجموعك</span>
              <span class="result-cta-arrow" aria-hidden="true">←</span>
            </a>
          </footer>
        </section>
      }
    </div>
  `,
  styles: [
    `
      .thanaweya-page {
        max-width: 580px;
        margin-inline: auto;
        padding-bottom: 3rem;
      }

      .thanaweya-page .breadcrumb {
        margin-bottom: 1.25rem;
      }

      /* ── Result card ── */
      .result-card {
        animation: result-enter 0.5s cubic-bezier(0.22, 1, 0.36, 1);
        padding: 1.35rem;
        border-radius: 20px;
        background: #fff;
        border: 1px solid rgba(30, 58, 138, 0.08);
        box-shadow:
          0 16px 48px rgba(15, 23, 42, 0.08),
          0 2px 8px rgba(15, 23, 42, 0.04);
      }

      .result-profile {
        display: flex;
        align-items: center;
        gap: 1rem;
        margin-bottom: 1.35rem;
        padding-bottom: 1.25rem;
        border-bottom: 1px solid #f1f5f9;
      }

      .result-avatar {
        flex-shrink: 0;
        display: grid;
        place-items: center;
        width: 3.25rem;
        height: 3.25rem;
        border-radius: 14px;
        font-family: var(--font-display);
        font-size: 1.25rem;
        font-weight: 800;
        color: var(--color-primary);
        background: linear-gradient(
          135deg,
          var(--color-surface-alt) 0%,
          #dbeafe 100%
        );
        border: 1px solid rgba(37, 99, 235, 0.12);
      }

      .result-profile-body {
        min-width: 0;
        flex: 1;
      }

      .result-eyebrow {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        flex-wrap: wrap;
        margin: 0 0 0.35rem;
        font-size: 0.72rem;
        font-weight: 700;
        color: var(--color-text-muted);
      }

      .result-year-badge {
        padding: 0.15rem 0.5rem;
        border-radius: 999px;
        font-size: 0.68rem;
        font-weight: 800;
        color: var(--color-primary);
        background: var(--color-surface-alt);
      }

      .result-name {
        margin: 0 0 0.4rem;
        font-family: var(--font-display);
        font-size: clamp(1.15rem, 4vw, 1.35rem);
        font-weight: 800;
        line-height: 1.45;
        color: #0f172a;
        overflow-wrap: anywhere;
      }

      .result-seating {
        display: flex;
        align-items: center;
        gap: 0.45rem;
        flex-wrap: wrap;
        margin: 0;
        font-size: 0.82rem;
      }

      .result-seating-label {
        font-weight: 600;
        color: var(--color-text-muted);
      }

      .result-seating-no {
        font-family: var(--font-display);
        font-weight: 800;
        letter-spacing: 0.04em;
        color: var(--color-primary);
        padding: 0.15rem 0.55rem;
        border-radius: 6px;
        background: #f8fafc;
        border: 1px solid #e2e8f0;
      }

      .result-score {
        display: flex;
        align-items: center;
        gap: 1.15rem;
        margin-bottom: 1.25rem;
        padding: 1.15rem 1.1rem;
        border-radius: 16px;
        background: linear-gradient(
          135deg,
          #f8fafc 0%,
          var(--color-surface-alt) 100%
        );
        border: 1px solid rgba(37, 99, 235, 0.1);
      }

      .result-score-ring {
        --score-pct: 0;
        flex-shrink: 0;
        display: grid;
        place-items: center;
        width: 6.5rem;
        height: 6.5rem;
        border-radius: 50%;
        background: conic-gradient(
          var(--color-primary-light) calc(var(--score-pct) * 1%),
          #e2e8f0 calc(var(--score-pct) * 1%)
        );
        transition: background 0.9s cubic-bezier(0.22, 1, 0.36, 1);
      }

      .result-score-ring-inner {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        width: 5.5rem;
        height: 5.5rem;
        border-radius: 50%;
        background: #fff;
        box-shadow: 0 2px 8px rgba(15, 23, 42, 0.06);
        line-height: 1.1;
      }

      .result-score-value {
        font-family: var(--font-display);
        font-size: clamp(1.65rem, 6vw, 1.85rem);
        font-weight: 800;
        color: var(--color-primary);
      }

      .result-score-max {
        font-size: 0.72rem;
        font-weight: 600;
        color: var(--color-text-muted);
      }

      .result-score-meta {
        flex: 1;
        min-width: 0;
      }

      .result-score-head {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 0.5rem;
        margin-bottom: 0.65rem;
      }

      .result-score-label {
        font-size: 0.85rem;
        font-weight: 700;
        color: var(--color-text);
      }

      .result-score-pct {
        font-family: var(--font-display);
        font-size: 0.88rem;
        font-weight: 800;
        color: var(--color-accent);
        padding: 0.2rem 0.55rem;
        border-radius: 999px;
        background: rgba(217, 119, 6, 0.1);
      }

      .result-score-bar {
        height: 8px;
        border-radius: 999px;
        background: #e2e8f0;
        overflow: hidden;
      }

      .result-score-bar-fill {
        height: 100%;
        border-radius: inherit;
        background: linear-gradient(
          90deg,
          var(--color-primary-light) 0%,
          var(--color-primary) 100%
        );
        transition: width 0.9s cubic-bezier(0.22, 1, 0.36, 1);
      }

      .result-details {
        display: grid;
        gap: 0.65rem;
        margin-bottom: 1.15rem;
      }

      .result-detail {
        display: flex;
        align-items: flex-start;
        gap: 0.75rem;
        padding: 0.85rem 0.95rem;
        border-radius: 12px;
        background: #fafbfc;
        border: 1px solid #f1f5f9;
      }

      .result-detail-icon {
        flex-shrink: 0;
        display: grid;
        place-items: center;
        width: 2rem;
        height: 2rem;
        border-radius: 8px;
        color: var(--color-primary-light);
        background: var(--color-surface-alt);
      }

      .result-detail-icon svg {
        width: 1.1rem;
        height: 1.1rem;
      }

      .result-detail-text {
        display: grid;
        gap: 0.15rem;
        min-width: 0;
      }

      .result-detail-label {
        font-size: 0.68rem;
        font-weight: 700;
        color: var(--color-text-muted);
      }

      .result-detail-value {
        font-size: 0.92rem;
        font-weight: 700;
        color: var(--color-primary);
        line-height: 1.45;
        overflow-wrap: anywhere;
      }

      .result-rank {
        margin-bottom: 1.15rem;
        padding: 1rem 1.05rem;
        border-radius: 14px;
        background: #fffbeb;
        border: 1px solid #fde68a;
      }

      .result-rank-head {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 0.5rem;
        flex-wrap: wrap;
        margin-bottom: 0.85rem;
      }

      .result-rank-label {
        font-size: 0.72rem;
        font-weight: 700;
        color: var(--color-text-muted);
      }

      .result-rank-track {
        font-size: 0.72rem;
        font-weight: 700;
        color: #92400e;
        padding: 0.2rem 0.55rem;
        border-radius: 999px;
        background: #fff;
        border: 1px solid #fde68a;
      }

      .result-rank-stats {
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 0.85rem;
        margin-bottom: 0.85rem;
      }

      .result-rank-stat {
        display: grid;
        gap: 0.1rem;
        text-align: center;
        min-width: 0;
      }

      .result-rank-stat--primary .result-rank-stat-value {
        color: var(--color-primary);
        font-size: clamp(1.75rem, 6vw, 2.25rem);
      }

      .result-rank-stat-value {
        font-family: var(--font-display);
        font-size: clamp(1.35rem, 4.5vw, 1.65rem);
        font-weight: 800;
        line-height: 1.1;
        color: #78350f;
      }

      .result-rank-stat-label {
        font-size: 0.68rem;
        font-weight: 700;
        color: var(--color-text-muted);
      }

      .result-rank-sep {
        font-size: 0.85rem;
        font-weight: 700;
        color: #a8a29e;
        flex-shrink: 0;
      }

      .result-rank-link {
        display: block;
        text-align: center;
        font-size: 0.82rem;
        font-weight: 700;
        color: var(--color-primary-light);
      }

      .result-rank-link:hover {
        color: var(--color-primary);
      }

      .result-footer {
        padding-top: 0.25rem;
      }

      .result-cta {
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 0.55rem;
        width: 100%;
        padding: 0.95rem 1.15rem;
        border-radius: 12px;
        font-weight: 700;
        font-size: 0.95rem;
        color: #fff;
        background: var(--color-primary-light);
        box-shadow: 0 4px 16px rgba(37, 99, 235, 0.22);
        transition:
          background 0.2s ease,
          transform 0.2s ease;
      }

      .result-cta:hover {
        background: var(--color-primary);
        transform: translateY(-1px);
        box-shadow: 0 8px 24px rgba(37, 99, 235, 0.28);
      }

      .result-cta-arrow {
        font-size: 1.1rem;
        color: var(--color-accent-light);
        transition: transform 0.2s ease;
      }

      .result-cta:hover .result-cta-arrow {
        transform: translateX(-3px);
      }

      @keyframes result-enter {
        from {
          opacity: 0;
          transform: translateY(14px);
        }
        to {
          opacity: 1;
          transform: translateY(0);
        }
      }

      @media (max-width: 480px) {
        .thanaweya-page {
          padding-bottom: 2rem;
        }

        .result-card {
          padding: 1.1rem;
        }

        .result-profile {
          align-items: flex-start;
        }

        .result-score {
          flex-direction: column;
          text-align: center;
        }

        .result-score-meta {
          width: 100%;
        }

        .result-rank-stats {
          gap: 0.55rem;
        }
      }

      @media (prefers-reduced-motion: reduce) {
        .result-card {
          animation: none;
        }

        .result-score-ring,
        .result-score-bar-fill {
          transition: none;
        }

        .result-cta:hover {
          transform: none;
        }

        .result-cta:hover .result-cta-arrow {
          transform: none;
        }
      }
    `,
  ],
})
export class ThanaweyaResultComponent {
  private fb = inject(FormBuilder);
  private api = inject(ApiService);

  readonly thanaweyaMaxScore = THANAWEYA_MAX_SCORE;
  readonly trackLabels = TRACK_LABELS;
  readonly fmt = formatNumber;

  loading = false;
  error = '';
  result: StudentResult | null = null;

  form = this.fb.group({
    seatingNo: ['', [Validators.required, digitsOnlyValidator()]],
  });

  resultPercentage(totalDegree: number): string {
    return ((totalDegree / THANAWEYA_MAX_SCORE) * 100).toFixed(2);
  }

  scoreProgress(totalDegree: number): number {
    return Math.min(
      Math.max((totalDegree / THANAWEYA_MAX_SCORE) * 100, 0),
      100,
    );
  }

  trackLabel(track: string): string {
    return this.trackLabels[track] ?? track;
  }

  studentInitial(name: string): string {
    const trimmed = name.trim();
    return trimmed ? trimmed.charAt(0) : '؟';
  }

  onSeatingInput(event: Event): void {
    applyDigitsOnlyInput(event, this.form.get('seatingNo'));
    this.error = '';
    this.result = null;
  }

  submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    const seatingNo = this.form.value.seatingNo!.trim();
    this.loading = true;
    this.error = '';
    this.result = null;

    this.api.getThanaweyaResult(seatingNo).subscribe({
      next: (data) => {
        this.loading = false;
        this.result = data;
      },
      error: (err: HttpErrorResponse) => {
        this.loading = false;
        if (err.status === 404) {
          this.error = NOT_FOUND_MESSAGE;
        } else {
          this.error = GENERIC_ERROR_MESSAGE;
        }
      },
    });
  }

  predictQueryParams(): { score: number; track?: string } {
    if (!this.result) return { score: 0 };
    const params: { score: number; track?: string } = {
      score: this.result.totalDegree,
    };
    if (this.result.track) {
      params.track = this.result.track;
    }
    return params;
  }

  hasTrackRank(result: StudentResult): boolean {
    return result.trackRank != null && result.trackTotalStudents != null;
  }
}
