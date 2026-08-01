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
            <header class="slip-header">
              <div class="slip-header-brand">
                <span class="slip-header-eyebrow">بيان نتيجة</span>
                <h2 class="slip-header-title">الثانوية العامة</h2>
              </div>
              <div class="slip-header-year">
                <span class="slip-header-year-label">عام</span>
                <span class="slip-header-year-value">{{ result.year }}</span>
              </div>
            </header>

            <div class="slip-divider" aria-hidden="true"></div>

            <div class="slip-identity">
              <p class="slip-name">{{ result.arabicName }}</p>
              <div class="slip-seating">
                <span class="slip-seating-label">رقم الجلوس</span>
                <span class="slip-seating-no">{{ result.seatingNo }}</span>
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
                <span class="slip-score-pct"
                  >{{ resultPercentage(result.totalDegree) }}%</span
                >
              </div>
              <div class="slip-score-display">
                <span class="slip-score-value">{{
                  result.totalDegree
                }}</span>
                <span class="slip-score-max">/ {{ thanaweyaMaxScore }}</span>
              </div>
              <div class="slip-scale" aria-hidden="true">
                <div class="slip-scale-track">
                  <div
                    class="slip-scale-fill"
                    [style.width.%]="scoreProgress(result.totalDegree)"
                  ></div>
                </div>
                <div class="slip-scale-labels">
                  <span>0</span>
                  <span>{{ thanaweyaMaxScore }}</span>
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
                <div class="slip-rank-copy">
                  <span class="slip-rank-label">الترتيب على الشعبة</span>
                  <span class="slip-rank-value">{{
                    trackRankLabel(result)
                  }}</span>
                </div>
                <a class="slip-rank-link" routerLink="/track-rank"
                  >تفاصيل الترتيب</a
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
        max-width: 560px;
        margin-inline: auto;
        padding-bottom: 3rem;
      }

      .thanaweya-page .breadcrumb {
        margin-bottom: 1.25rem;
      }

      /* ── Lookup ── */
      .lookup-stage {
        display: grid;
        gap: 1.35rem;
        justify-items: center;
        text-align: center;
        padding: 2rem 1.5rem;
        margin-bottom: 1.5rem;
        border-radius: 20px;
        background: #fff;
        border: 1px solid rgba(30, 58, 138, 0.07);
        box-shadow:
          0 1px 2px rgba(15, 23, 42, 0.04),
          0 12px 36px rgba(15, 23, 42, 0.06);
        transition:
          padding 0.3s ease,
          margin 0.3s ease,
          box-shadow 0.3s ease;
      }

      .lookup-stage--compact {
        padding: 1.25rem 1.15rem;
        margin-bottom: 1rem;
        gap: 0.85rem;
        box-shadow: 0 6px 20px rgba(15, 23, 42, 0.05);
      }

      .lookup-stage--compact .lookup-title {
        font-size: clamp(1.05rem, 3.5vw, 1.25rem);
        margin-bottom: 0;
      }

      .lookup-stage-copy {
        max-width: 440px;
      }

      .lookup-title {
        font-family: var(--font-display);
        font-size: clamp(1.35rem, 4vw, 1.85rem);
        font-weight: 800;
        color: var(--color-primary);
        line-height: 1.35;
        margin: 0 0 0.6rem;
      }

      .lookup-lead {
        margin: 0;
        color: var(--color-text-muted);
        line-height: 1.7;
        font-size: 0.96rem;
        max-width: 38ch;
        margin-inline: auto;
      }

      .lookup-form {
        width: 100%;
        max-width: 340px;
        display: grid;
        gap: 0.75rem;
        padding-top: 0.25rem;
      }

      .lookup-label {
        font-size: 0.84rem;
        font-weight: 700;
        color: var(--color-text-muted);
        margin: 0;
      }

      .lookup-input-wrap {
        position: relative;
      }

      .lookup-input-wrap::before {
        content: '#';
        position: absolute;
        right: 1rem;
        top: 50%;
        transform: translateY(-50%);
        font-family: var(--font-display);
        font-size: 1rem;
        font-weight: 700;
        color: rgba(30, 58, 138, 0.22);
        pointer-events: none;
      }

      .lookup-input-wrap input {
        width: 100%;
        text-align: center;
        font-family: var(--font-display);
        font-size: 1.25rem;
        letter-spacing: 0.12em;
        font-weight: 700;
        padding: 0.9rem 2.4rem;
        border: 1.5px solid #e5e7eb;
        border-radius: 12px;
        background: var(--color-surface);
        transition:
          border-color 0.2s ease,
          background 0.2s ease,
          box-shadow 0.2s ease;
      }

      .lookup-input-wrap input::placeholder {
        color: #cbd5e1;
        letter-spacing: 0.08em;
        font-weight: 600;
      }

      .lookup-input-wrap input:focus {
        outline: none;
        border-color: var(--color-primary-light);
        background: #fff;
        box-shadow: 0 0 0 3px rgba(37, 99, 235, 0.1);
      }

      .lookup-submit {
        width: 100%;
        display: inline-flex;
        align-items: center;
        justify-content: center;
        gap: 0.5rem;
        margin-top: 0.25rem;
        border-radius: 12px;
      }

      .lookup-spinner {
        width: 1rem;
        height: 1rem;
        border: 2px solid rgba(255, 255, 255, 0.35);
        border-top-color: #fff;
        border-radius: 50%;
        animation: spin 0.7s linear infinite;
      }

      @keyframes spin {
        to {
          transform: rotate(360deg);
        }
      }

      .field-error {
        color: #dc2626;
        font-size: 0.84rem;
        text-align: center;
        margin: -0.15rem 0 0;
      }

      .lookup-notice {
        padding: 0.7rem 0.9rem;
        border-radius: 10px;
        background: #fffbeb;
        border: 1px solid #fde68a;
        color: #92400e;
        font-size: 0.88rem;
        line-height: 1.55;
        text-align: center;
      }

      /* ── Result slip ── */
      .result-slip {
        animation: slip-enter 0.55s cubic-bezier(0.22, 1, 0.36, 1);
      }

      .slip-sheet {
        position: relative;
        padding: 1.35rem 1.25rem 1.5rem;
        border-radius: 4px;
        background: #faf8f4;
        border: 1px solid #d8d2c8;
        box-shadow:
          0 24px 48px rgba(15, 23, 42, 0.1),
          0 4px 12px rgba(15, 23, 42, 0.05);
      }

      .slip-header {
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
        font-size: clamp(1.05rem, 3.5vw, 1.25rem);
        font-weight: 800;
        color: var(--color-primary);
        line-height: 1.35;
      }

      .slip-header-year {
        flex-shrink: 0;
        display: grid;
        justify-items: center;
        gap: 0.1rem;
        padding: 0.45rem 0.75rem;
        border: 1.5px solid var(--color-primary);
        border-radius: 2px;
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
        font-size: 1.15rem;
        line-height: 1;
      }

      .slip-divider {
        height: 2px;
        margin-bottom: 1.15rem;
        background: linear-gradient(
          90deg,
          var(--color-primary) 0%,
          var(--color-accent) 45%,
          var(--color-primary) 100%
        );
      }

      .slip-identity {
        text-align: center;
        margin-bottom: 1.35rem;
      }

      .slip-name {
        margin: 0 0 0.85rem;
        font-family: var(--font-display);
        font-size: clamp(1.25rem, 4.5vw, 1.55rem);
        font-weight: 800;
        line-height: 1.45;
        color: #0f172a;
      }

      .slip-seating {
        display: inline-grid;
        gap: 0.15rem;
        padding: 0.45rem 1.1rem;
        border: 1px dashed #c4bfb4;
        border-radius: 2px;
        background: rgba(255, 255, 255, 0.65);
      }

      .slip-seating-label {
        font-size: 0.65rem;
        font-weight: 700;
        color: var(--color-text-muted);
      }

      .slip-seating-no {
        font-family: var(--font-display);
        font-size: 1.05rem;
        font-weight: 800;
        letter-spacing: 0.14em;
        color: var(--color-primary);
      }

      .slip-score {
        margin-bottom: 1.25rem;
        padding: 1.15rem 1rem 1rem;
        border-radius: 2px;
        background: linear-gradient(165deg, #0f172a 0%, #1e3a8a 100%);
        color: #fff;
      }

      .slip-score-head {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 0.75rem;
        margin-bottom: 0.55rem;
      }

      .slip-score-label {
        font-size: 0.72rem;
        font-weight: 700;
        opacity: 0.82;
      }

      .slip-score-value,
      .slip-score-max,
      .slip-score-pct {
        font-family: var(--font-display);
        font-weight: 800;
      }

      .slip-score-pct {
        font-size: 0.88rem;
        color: var(--color-accent-light);
      }

      .slip-score-display {
        display: flex;
        align-items: baseline;
        justify-content: center;
        gap: 0.35rem;
        margin-bottom: 1rem;
        line-height: 1;
      }

      .slip-score-value {
        font-size: clamp(3rem, 12vw, 3.75rem);
        letter-spacing: -0.02em;
      }

      .slip-score-max {
        font-size: clamp(1rem, 3.5vw, 1.2rem);
        font-weight: 600;
        opacity: 0.55;
      }

      .slip-scale-track {
        height: 6px;
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
        margin-top: 0.35rem;
        font-size: 0.62rem;
        font-weight: 600;
        opacity: 0.5;
      }

      .slip-details {
        margin: 0 0 1rem;
        padding: 0;
        display: grid;
        gap: 0;
        border: 1px solid #e8e4dc;
        border-radius: 2px;
        overflow: hidden;
        background: #fff;
      }

      .slip-detail {
        display: grid;
        grid-template-columns: minmax(7rem, 38%) 1fr;
        gap: 0.75rem;
        padding: 0.75rem 0.9rem;
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
        font-size: 0.88rem;
        font-weight: 700;
        color: var(--color-primary);
        overflow-wrap: anywhere;
      }

      .slip-rank {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 0.75rem;
        flex-wrap: wrap;
        margin-bottom: 1.15rem;
        padding: 0.85rem 0.95rem;
        border-radius: 2px;
        background: #fffbeb;
        border: 1px solid #fde68a;
      }

      .slip-rank-copy {
        display: grid;
        gap: 0.15rem;
        min-width: 0;
      }

      .slip-rank-label,
      .slip-rank-value {
        font-weight: 700;
      }

      .slip-rank-label {
        font-size: 0.68rem;
        color: #92400e;
      }

      .slip-rank-value {
        font-family: var(--font-display);
        font-size: 0.92rem;
        font-weight: 800;
        color: #78350f;
        line-height: 1.45;
      }

      .slip-rank-link {
        flex-shrink: 0;
        font-size: 0.78rem;
        font-weight: 700;
        color: var(--color-primary-light);
      }

      .slip-footer {
        padding-top: 1.1rem;
        border-top: 1px dashed #d8d2c8;
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
        border-radius: 2px;
        font-weight: 700;
        font-size: 0.95rem;
        color: #fff;
        background: var(--color-primary-light);
        box-shadow: 0 4px 16px rgba(37, 99, 235, 0.22);
        transition: background 0.2s ease, transform 0.2s ease;
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

        .lookup-stage {
          padding: 1.35rem 1rem;
          border-radius: 16px;
        }

        .lookup-stage--compact {
          padding: 1rem 0.9rem;
        }

        .lookup-input-wrap input {
          font-size: 1.15rem;
          padding: 0.8rem 2.2rem;
        }

        .slip-sheet {
          padding: 1.1rem 0.95rem 1.25rem;
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

        .slip-rank {
          flex-direction: column;
          align-items: stretch;
        }
      }

      @media (prefers-reduced-motion: reduce) {
        .result-slip {
          animation: none;
        }

        .slip-scale-fill {
          transition: none;
        }

        .lookup-spinner {
          animation: none;
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

  trackRankLabel(result: StudentResult): string {
    const trackName = result.track
      ? (this.trackLabels[result.track] ?? result.track)
      : '';
    const base = `${result.trackRank} من ${result.trackTotalStudents}`;
    return trackName ? `${base} (${trackName})` : base;
  }
}
