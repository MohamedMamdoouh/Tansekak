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
  selector: 'app-track-rank',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="container track-rank-page">
      <nav class="breadcrumb" aria-label="مسار التنقل">
        <a routerLink="/">الرئيسية</a>
        <span class="breadcrumb-sep">/</span>
        <span>ترتيب الشعبة</span>
      </nav>

      <section class="lookup-stage" [class.lookup-stage--compact]="result">
        <div class="lookup-stage-copy">
          <h1 class="lookup-title">ترتيبك على الشعبة</h1>
          @if (!result) {
            <p class="lookup-lead">
              اكتب رقم جلوسك واعرف ترتيبك بين طلاب شعبتك في نفس سنة النتيجة.
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
              اعرض الترتيب
            }
          </button>
        </form>
      </section>

      @if (result) {
        <section class="rank-card" aria-label="ترتيب الطالب على الشعبة">
          <header class="rank-header">
            <div class="rank-header-top">
              @if (result.track) {
                <span class="rank-track-badge">{{
                  trackLabel(result.track)
                }}</span>
              }
              <span class="rank-year">{{ result.year }}</span>
            </div>
            <h2 class="rank-student-name">{{ result.arabicName }}</h2>
            <p class="rank-seating">
              <span class="rank-seating-label">رقم الجلوس</span>
              <span class="rank-seating-no">{{
                result.seatingNo
              }}</span>
            </p>
          </header>

          <div class="rank-body">
            @if (hasRank(result)) {
              <div class="rank-spotlight">
                <p class="rank-spotlight-label">ترتيبك</p>
                <p
                  class="rank-spotlight-value num"
                  [attr.aria-label]="rankAriaLabel(result)"
                >
                  {{ fmt(result.trackRank) }}
                </p>
                <p class="rank-spotlight-of">
                  من
                  <strong class="num">{{
                    fmt(result.trackTotalStudents)
                  }}</strong>
                  طالب
                </p>

                <div class="rank-stand" aria-hidden="true">
                  <div class="rank-stand-track">
                    <div
                      class="rank-stand-marker"
                      [style.inset-inline-start.%]="rankPositionPercent(result)"
                    ></div>
                  </div>
                  <div class="rank-stand-labels">
                    <span>الأوائل</span>
                    <span>منتصف القائمة</span>
                    <span>آخر القائمة</span>
                  </div>
                </div>

                <p class="rank-percentile">
                  أفضل
                  <strong class="num">{{ rankPercentile(result) }}</strong>
                  % من طلاب الشعبة
                </p>
              </div>
            } @else {
              <div class="rank-unavailable">
                <p class="rank-unavailable-title">تعذر تحديد الترتيب</p>
                <p class="rank-unavailable-text">
                  لم نتمكن من تحديد شعبتك من بيانات النتيجة. راجع حالة الطالب في
                  صفحة النتيجة الكاملة.
                </p>
              </div>
            }

            <div class="rank-meta">
              <article class="rank-meta-item">
                <span class="rank-meta-label">المجموع الكلي</span>
                <span class="rank-meta-value"
                  >{{ result.totalDegree
                  }}<span class="rank-meta-denom"
                    >/{{ thanaweyaMaxScore }}</span
                  ></span
                >
              </article>
              <article class="rank-meta-item">
                <span class="rank-meta-label">النسبة المئوية</span>
                <span class="rank-meta-value num"
                  >{{ scorePercentage(result.totalDegree) }}%</span
                >
              </article>
            </div>

            <div class="rank-actions">
              <a class="btn btn-secondary" routerLink="/thanaweya-result">
                عرض النتيجة الكاملة
              </a>
              <a
                class="btn btn-primary"
                [routerLink]="['/predict']"
                [queryParams]="predictQueryParams()"
              >
                اعرف كليتك
              </a>
            </div>
          </div>
        </section>
      }
    </div>
  `,
  styles: [
    `
      .track-rank-page {
        max-width: 540px;
        margin-inline: auto;
        padding-bottom: 3rem;
      }

      .track-rank-page .breadcrumb {
        margin-bottom: 1.25rem;
      }

      /* ── Rank card ── */
      .rank-card {
        border-radius: 16px;
        overflow: hidden;
        background: #fff;
        border: 1px solid rgba(30, 58, 138, 0.08);
        box-shadow:
          0 1px 2px rgba(15, 23, 42, 0.04),
          0 24px 56px rgba(15, 23, 42, 0.1);
        animation: rank-enter 0.5s cubic-bezier(0.22, 1, 0.36, 1);
      }

      .rank-header {
        padding: 1.35rem 1.25rem 1.2rem;
        background: linear-gradient(
          155deg,
          #0f172a 0%,
          #1e3a8a 60%,
          #1e40af 100%
        );
        color: #fff;
        text-align: center;
      }

      .rank-header-top {
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 0.5rem;
        flex-wrap: wrap;
        margin-bottom: 0.85rem;
      }

      .rank-track-badge {
        font-size: 0.72rem;
        font-weight: 700;
        padding: 0.25rem 0.65rem;
        border-radius: 999px;
        background: rgba(245, 158, 11, 0.2);
        border: 1px solid rgba(245, 158, 11, 0.35);
        color: var(--color-accent-light);
      }

      .rank-year {
        font-size: 0.72rem;
        font-weight: 700;
        padding: 0.25rem 0.55rem;
        border-radius: 999px;
        background: rgba(255, 255, 255, 0.1);
        border: 1px solid rgba(255, 255, 255, 0.15);
        opacity: 0.9;
      }

      .rank-student-name {
        font-family: var(--font-display);
        font-size: clamp(1.2rem, 4vw, 1.5rem);
        font-weight: 800;
        line-height: 1.45;
        margin: 0 0 0.75rem;
      }

      .rank-seating {
        display: inline-flex;
        flex-direction: column;
        align-items: center;
        gap: 0.15rem;
        margin: 0;
        padding: 0.45rem 0.95rem;
        border-radius: 6px;
        background: rgba(255, 255, 255, 0.07);
        border: 1px solid rgba(255, 255, 255, 0.12);
      }

      .rank-seating-label {
        font-size: 0.65rem;
        opacity: 0.75;
      }

      .rank-seating-no {
        font-family: var(--font-display);
        font-size: 1.05rem;
        font-weight: 800;
        letter-spacing: 0.04em;
        color: var(--color-accent-light);
      }

      .rank-body {
        padding: 1.75rem 1.25rem 1.5rem;
      }

      .rank-spotlight {
        text-align: center;
        margin-bottom: 1.5rem;
        padding: 1.35rem 1rem 1.25rem;
        border-radius: 12px;
        background: linear-gradient(180deg, #fffbeb 0%, #fef9ee 100%);
        border: 1px solid #f0dfa8;
      }

      .rank-spotlight-label {
        margin: 0 0 0.35rem;
        font-size: 0.78rem;
        font-weight: 700;
        color: #92400e;
        letter-spacing: 0.04em;
      }

      .rank-spotlight-value {
        margin: 0;
        font-family: var(--font-display);
        font-size: clamp(3rem, 14vw, 4.25rem);
        font-weight: 800;
        line-height: 1;
        color: var(--color-primary);
        letter-spacing: -0.02em;
      }

      .rank-spotlight-of {
        margin: 0.45rem 0 1.15rem;
        font-size: 1rem;
        color: var(--color-text-muted);
        line-height: 1.5;
      }

      .rank-spotlight-of strong {
        font-family: var(--font-display);
        font-weight: 800;
        font-size: 1.15rem;
        color: #78350f;
      }

      .rank-stand {
        margin-bottom: 0.85rem;
      }

      .rank-stand-track {
        position: relative;
        height: 8px;
        border-radius: 999px;
        background: linear-gradient(
          90deg,
          #2563eb 0%,
          #93c5fd 50%,
          #e5e7eb 100%
        );
        overflow: visible;
      }

      .rank-stand-marker {
        position: absolute;
        top: 50%;
        width: 16px;
        height: 16px;
        border-radius: 50%;
        background: var(--color-accent);
        border: 3px solid #fff;
        box-shadow: 0 2px 8px rgba(217, 119, 6, 0.45);
        transform: translate(-50%, -50%);
        transition: inset-inline-start 0.8s cubic-bezier(0.22, 1, 0.36, 1);
      }

      .rank-stand-labels {
        display: flex;
        justify-content: space-between;
        margin-top: 0.45rem;
        font-size: 0.62rem;
        font-weight: 600;
        color: var(--color-text-muted);
      }

      .rank-percentile {
        margin: 0;
        font-size: 0.88rem;
        color: #78350f;
        line-height: 1.5;
      }

      .rank-percentile strong {
        font-family: var(--font-display);
        font-weight: 800;
        font-size: 1.05rem;
        color: var(--color-primary);
      }

      .rank-unavailable {
        margin-bottom: 1.5rem;
        padding: 1rem;
        border-radius: 12px;
        background: #fffbeb;
        border: 1px solid #fde68a;
        text-align: center;
      }

      .rank-unavailable-title {
        margin: 0 0 0.35rem;
        font-weight: 800;
        color: #92400e;
      }

      .rank-unavailable-text {
        margin: 0;
        font-size: 0.88rem;
        color: #78350f;
        line-height: 1.6;
      }

      .rank-meta {
        display: grid;
        grid-template-columns: repeat(2, minmax(0, 1fr));
        gap: 0.65rem;
        margin-bottom: 1.35rem;
      }

      .rank-meta-item {
        padding: 0.85rem 0.75rem;
        background: var(--color-surface);
        border: 1px solid rgba(30, 58, 138, 0.06);
        border-radius: 12px;
        text-align: center;
      }

      .rank-meta-label {
        display: block;
        font-size: 0.68rem;
        font-weight: 700;
        color: var(--color-text-muted);
        margin-bottom: 0.25rem;
      }

      .rank-meta-value {
        font-family: var(--font-display);
        font-weight: 800;
        font-size: clamp(1.05rem, 3.5vw, 1.2rem);
        color: var(--color-primary);
        line-height: 1.2;
      }

      .rank-meta-denom {
        font-size: 0.78em;
        font-weight: 600;
        opacity: 0.55;
      }

      .rank-actions {
        display: grid;
        gap: 0.65rem;
      }

      .rank-actions .btn {
        width: 100%;
        border-radius: 12px;
      }

      @keyframes rank-enter {
        from {
          opacity: 0;
          transform: translateY(16px);
        }
        to {
          opacity: 1;
          transform: translateY(0);
        }
      }

      @media (max-width: 480px) {
        .track-rank-page {
          padding-bottom: 2rem;
        }

        .rank-meta {
          grid-template-columns: 1fr;
        }

        .rank-stand-labels {
          font-size: 0.58rem;
        }
      }

      @media (prefers-reduced-motion: reduce) {
        .rank-card {
          animation: none;
        }

        .rank-stand-marker {
          transition: none;
        }
      }
    `,
  ],
})
export class TrackRankComponent {
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

  hasRank(result: StudentResult): boolean {
    return result.trackRank != null && result.trackTotalStudents != null;
  }

  trackLabel(track: string): string {
    return this.trackLabels[track] ?? track;
  }

  rankAriaLabel(result: StudentResult): string {
    return `الترتيب ${result.trackRank} من ${result.trackTotalStudents} في ${this.trackLabel(result.track!)}`;
  }

  rankPositionPercent(result: StudentResult): number {
    if (!this.hasRank(result)) return 0;
    const rank = result.trackRank!;
    const total = result.trackTotalStudents!;
    if (total <= 1) return 0;
    return Math.min(Math.max(((rank - 1) / (total - 1)) * 100, 0), 100);
  }

  rankPercentile(result: StudentResult): string {
    if (!this.hasRank(result)) return '0';
    const rank = result.trackRank!;
    const total = result.trackTotalStudents!;
    const pct = ((total - rank + 1) / total) * 100;
    return pct.toFixed(1);
  }

  scorePercentage(totalDegree: number): string {
    return ((totalDegree / THANAWEYA_MAX_SCORE) * 100).toFixed(2);
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
}
