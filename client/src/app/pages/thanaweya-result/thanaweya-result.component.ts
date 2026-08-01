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
        <section class="result-slip" aria-label="نتيجة الطالب">
          <div class="slip-sheet">
            <div class="slip-watermark" aria-hidden="true">نتيجة</div>

            <header class="slip-header">
              <div class="slip-header-brand">
                <span class="slip-header-eyebrow">بيان نتيجة</span>
                <h2 class="slip-header-title">الثانوية العامة</h2>
              </div>
              <div class="slip-header-year">
                <span class="slip-header-year-label">عام</span>
                <span class="slip-header-year-value num">{{
                  fmt(result.year)
                }}</span>
              </div>
            </header>

            <div class="slip-divider" aria-hidden="true"></div>

            <div class="slip-identity">
              <p class="slip-name">{{ result.arabicName }}</p>
              <div class="slip-seating">
                <span class="slip-seating-label">رقم الجلوس</span>
                <span class="slip-seating-no num">{{
                  fmt(result.seatingNo)
                }}</span>
              </div>
            </div>

            <div
              class="slip-score"
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
              <div class="slip-score-head">
                <span class="slip-score-label">المجموع الكلي</span>
                <span class="slip-score-pct num"
                  >{{ resultPercentage(result.totalDegree) }}%</span
                >
              </div>
              <div class="slip-score-display">
                <span class="slip-score-value num">{{
                  fmt(result.totalDegree)
                }}</span>
                <span class="slip-score-max num"
                  >/ {{ fmt(thanaweyaMaxScore) }}</span
                >
              </div>
              <div class="slip-scale" aria-hidden="true">
                <div class="slip-scale-track">
                  <div
                    class="slip-scale-fill"
                    [style.width.%]="scoreProgress(result.totalDegree)"
                  ></div>
                </div>
                <div class="slip-scale-labels">
                  <span class="num">0</span>
                  <span class="num">{{ fmt(thanaweyaMaxScore) }}</span>
                </div>
              </div>
            </div>

            <dl class="slip-details">
              <div class="slip-detail">
                <dt>حالة الطالب</dt>
                <dd>{{ result.studentCaseDesc }}</dd>
              </div>
              @if (result.track) {
                <div class="slip-detail">
                  <dt>الشعبة</dt>
                  <dd>{{ trackLabel(result.track) }}</dd>
                </div>
              }
            </dl>

            @if (hasTrackRank(result)) {
              <div class="slip-rank">
                <div class="slip-rank-head">
                  <span class="slip-rank-label">الترتيب على الشعبة</span>
                  @if (result.track) {
                    <span class="slip-rank-track">{{
                      trackLabel(result.track)
                    }}</span>
                  }
                </div>
                <div class="slip-rank-stats">
                  <div class="slip-rank-stat slip-rank-stat--primary">
                    <span class="slip-rank-stat-value num">{{
                      fmt(result.trackRank)
                    }}</span>
                    <span class="slip-rank-stat-label">ترتيبك</span>
                  </div>
                  <span class="slip-rank-sep" aria-hidden="true">من</span>
                  <div class="slip-rank-stat">
                    <span class="slip-rank-stat-value num">{{
                      fmt(result.trackTotalStudents)
                    }}</span>
                    <span class="slip-rank-stat-label">طالب</span>
                  </div>
                </div>
                <a class="slip-rank-link" routerLink="/track-rank"
                  >تفاصيل الترتيب ←</a
                >
              </div>
            }

            <footer class="slip-footer">
              <p class="slip-footer-hint">الخطوة التالية</p>
              <a
                class="slip-cta"
                [routerLink]="['/predict']"
                [queryParams]="predictQueryParams()"
              >
                <span>اعرف الكليات المتاحة لمجموعك</span>
                <span class="slip-cta-arrow" aria-hidden="true">←</span>
              </a>
            </footer>
          </div>
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

      /* ── Result slip ── */
      .result-slip {
        animation: slip-enter 0.55s cubic-bezier(0.22, 1, 0.36, 1);
      }

      .slip-sheet {
        position: relative;
        overflow: hidden;
        padding: 1.5rem 1.35rem 1.6rem;
        border-radius: 6px;
        background: linear-gradient(180deg, #fdfbf7 0%, #f7f3ec 100%);
        border: 1px solid #cfc8bc;
        box-shadow:
          0 28px 56px rgba(15, 23, 42, 0.11),
          0 4px 12px rgba(15, 23, 42, 0.05),
          inset 0 1px 0 rgba(255, 255, 255, 0.8);
      }

      .slip-watermark {
        position: absolute;
        top: 50%;
        left: 50%;
        transform: translate(-50%, -50%) rotate(-18deg);
        font-family: var(--font-display);
        font-size: clamp(4rem, 18vw, 6.5rem);
        font-weight: 800;
        color: rgba(30, 58, 138, 0.04);
        pointer-events: none;
        user-select: none;
        white-space: nowrap;
      }

      .slip-header {
        position: relative;
        display: flex;
        align-items: flex-start;
        justify-content: space-between;
        gap: 1rem;
        padding-bottom: 1rem;
      }

      .slip-header-eyebrow {
        display: block;
        font-size: 0.68rem;
        font-weight: 700;
        color: var(--color-accent);
        margin-bottom: 0.2rem;
      }

      .slip-header-title {
        margin: 0;
        font-family: var(--font-display);
        font-size: clamp(1.1rem, 3.5vw, 1.3rem);
        font-weight: 800;
        color: var(--color-primary);
        line-height: 1.35;
      }

      .slip-header-year {
        flex-shrink: 0;
        display: grid;
        justify-items: center;
        gap: 0.1rem;
        padding: 0.5rem 0.85rem;
        border: 2px solid var(--color-primary);
        border-radius: 4px;
        background: #fff;
      }

      .slip-header-year-label,
      .slip-header-year-value {
        font-family: var(--font-display);
        font-weight: 800;
        color: var(--color-primary);
      }

      .slip-header-year-label {
        font-size: 0.62rem;
        color: var(--color-text-muted);
      }

      .slip-header-year-value {
        font-size: 1.2rem;
        line-height: 1;
      }

      .slip-divider {
        height: 3px;
        margin-bottom: 1.2rem;
        border-radius: 1px;
        background: repeating-linear-gradient(
          90deg,
          var(--color-primary) 0,
          var(--color-primary) 8px,
          var(--color-accent) 8px,
          var(--color-accent) 16px
        );
      }

      .slip-identity {
        position: relative;
        text-align: center;
        margin-bottom: 1.4rem;
      }

      .slip-name {
        margin: 0 0 0.9rem;
        font-family: var(--font-display);
        font-size: clamp(1.3rem, 4.5vw, 1.6rem);
        font-weight: 800;
        line-height: 1.45;
        color: #0f172a;
      }

      .slip-seating {
        display: inline-grid;
        gap: 0.2rem;
        padding: 0.5rem 1.25rem;
        border: 1px solid #b8b0a4;
        border-radius: 4px;
        background: rgba(255, 255, 255, 0.75);
      }

      .slip-seating-label {
        font-size: 0.65rem;
        font-weight: 700;
        color: var(--color-text-muted);
      }

      .slip-seating-no {
        font-family: var(--font-display);
        font-size: 1.1rem;
        font-weight: 800;
        letter-spacing: 0.06em;
        color: var(--color-primary);
      }

      .slip-score {
        position: relative;
        margin-bottom: 1.25rem;
        padding: 1.25rem 1.1rem 1.05rem;
        border-radius: 6px;
        background: linear-gradient(
          155deg,
          #0f172a 0%,
          #1e3a8a 55%,
          #1e40af 100%
        );
        color: #fff;
        box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.08);
      }

      .slip-score-head {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 0.75rem;
        margin-bottom: 0.6rem;
      }

      .slip-score-label {
        font-size: 0.74rem;
        font-weight: 700;
        opacity: 0.85;
      }

      .slip-score-value,
      .slip-score-max,
      .slip-score-pct {
        font-family: var(--font-display);
        font-weight: 800;
      }

      .slip-score-pct {
        font-size: 0.92rem;
        color: var(--color-accent-light);
        padding: 0.2rem 0.55rem;
        border-radius: 999px;
        background: rgba(245, 158, 11, 0.15);
      }

      .slip-score-display {
        display: flex;
        align-items: baseline;
        justify-content: center;
        gap: 0.4rem;
        margin-bottom: 1.05rem;
        line-height: 1;
      }

      .slip-score-value {
        font-size: clamp(3.1rem, 13vw, 4rem);
        letter-spacing: -0.01em;
      }

      .slip-score-max {
        font-size: clamp(1rem, 3.5vw, 1.25rem);
        font-weight: 600;
        opacity: 0.55;
      }

      .slip-scale-track {
        height: 7px;
        border-radius: 999px;
        background: rgba(255, 255, 255, 0.14);
        overflow: hidden;
      }

      .slip-scale-fill {
        height: 100%;
        border-radius: inherit;
        background: linear-gradient(
          90deg,
          var(--color-accent-light) 0%,
          var(--color-accent) 100%
        );
        transition: width 0.9s cubic-bezier(0.22, 1, 0.36, 1);
      }

      .slip-scale-labels {
        display: flex;
        justify-content: space-between;
        margin-top: 0.4rem;
        font-size: 0.65rem;
        font-weight: 600;
        opacity: 0.55;
      }

      .slip-details {
        margin: 0 0 1rem;
        padding: 0;
        display: grid;
        gap: 0;
        border: 1px solid #ddd6cb;
        border-radius: 4px;
        overflow: hidden;
        background: #fff;
      }

      .slip-detail {
        display: grid;
        grid-template-columns: minmax(7rem, 38%) 1fr;
        gap: 0.75rem;
        padding: 0.8rem 1rem;
        border-bottom: 1px solid #f0ece6;
      }

      .slip-detail:last-child {
        border-bottom: none;
      }

      .slip-detail dt,
      .slip-detail dd {
        margin: 0;
        line-height: 1.5;
      }

      .slip-detail dt {
        font-size: 0.72rem;
        font-weight: 700;
        color: var(--color-text-muted);
      }

      .slip-detail dd {
        font-size: 0.9rem;
        font-weight: 700;
        color: var(--color-primary);
        overflow-wrap: anywhere;
      }

      .slip-rank {
        margin-bottom: 1.15rem;
        padding: 1rem 1.05rem;
        border-radius: 6px;
        background: #fff;
        border: 1px solid #e8dfd0;
        box-shadow: 0 4px 16px rgba(217, 119, 6, 0.08);
      }

      .slip-rank-head {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 0.5rem;
        flex-wrap: wrap;
        margin-bottom: 0.85rem;
      }

      .slip-rank-label {
        font-size: 0.72rem;
        font-weight: 700;
        color: var(--color-text-muted);
      }

      .slip-rank-track {
        font-size: 0.72rem;
        font-weight: 700;
        color: #92400e;
        padding: 0.2rem 0.55rem;
        border-radius: 999px;
        background: #fffbeb;
        border: 1px solid #fde68a;
      }

      .slip-rank-stats {
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 0.85rem;
        margin-bottom: 0.85rem;
      }

      .slip-rank-stat {
        display: grid;
        gap: 0.1rem;
        text-align: center;
        min-width: 0;
      }

      .slip-rank-stat--primary .slip-rank-stat-value {
        color: var(--color-primary);
        font-size: clamp(1.75rem, 6vw, 2.25rem);
      }

      .slip-rank-stat-value {
        font-family: var(--font-display);
        font-size: clamp(1.35rem, 4.5vw, 1.65rem);
        font-weight: 800;
        line-height: 1.1;
        color: #78350f;
      }

      .slip-rank-stat-label {
        font-size: 0.68rem;
        font-weight: 700;
        color: var(--color-text-muted);
      }

      .slip-rank-sep {
        font-size: 0.85rem;
        font-weight: 700;
        color: #a8a29e;
        flex-shrink: 0;
      }

      .slip-rank-link {
        display: block;
        text-align: center;
        font-size: 0.82rem;
        font-weight: 700;
        color: var(--color-primary-light);
      }

      .slip-rank-link:hover {
        color: var(--color-primary);
      }

      .slip-footer {
        padding-top: 1.15rem;
        border-top: 1px dashed #cfc8bc;
      }

      .slip-footer-hint {
        margin: 0 0 0.65rem;
        font-size: 0.68rem;
        font-weight: 700;
        color: var(--color-text-muted);
        text-align: center;
      }

      .slip-cta {
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 0.55rem;
        width: 100%;
        padding: 0.95rem 1.15rem;
        border-radius: 6px;
        font-weight: 700;
        font-size: 0.95rem;
        color: #fff;
        background: var(--color-primary-light);
        box-shadow: 0 4px 16px rgba(37, 99, 235, 0.22);
        transition:
          background 0.2s ease,
          transform 0.2s ease;
      }

      .slip-cta:hover {
        background: var(--color-primary);
        transform: translateY(-1px);
        box-shadow: 0 8px 24px rgba(37, 99, 235, 0.28);
      }

      .slip-cta-arrow {
        font-size: 1.1rem;
        color: var(--color-accent-light);
        transition: transform 0.2s ease;
      }

      .slip-cta:hover .slip-cta-arrow {
        transform: translateX(-3px);
      }

      @keyframes slip-enter {
        from {
          opacity: 0;
          transform: translateY(18px);
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

        .slip-sheet {
          padding: 1.15rem 1rem 1.3rem;
        }

        .slip-header {
          flex-direction: column;
          align-items: stretch;
        }

        .slip-header-year {
          justify-self: start;
          width: fit-content;
        }

        .slip-detail {
          grid-template-columns: 1fr;
          gap: 0.25rem;
        }

        .slip-rank-stats {
          gap: 0.55rem;
        }
      }

      @media (prefers-reduced-motion: reduce) {
        .result-slip {
          animation: none;
        }

        .slip-scale-fill {
          transition: none;
        }

        .slip-cta:hover {
          transform: none;
        }

        .slip-cta:hover .slip-cta-arrow {
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
