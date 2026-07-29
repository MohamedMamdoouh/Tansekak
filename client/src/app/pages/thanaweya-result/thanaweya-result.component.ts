import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ApiService } from '../../api.service';
import { StudentResult } from '../../models';
import {
  applyDigitsOnlyInput,
  digitsOnlyValidator,
} from '../../form-validators';

const NOT_FOUND_MESSAGE = 'لم يتم العثور على نتيجة لهذا الرقم.';
const GENERIC_ERROR_MESSAGE = 'حدث خطأ أثناء البحث. حاول مرة أخرى لاحقًا.';
const THANAWEYA_MAX_SCORE = 320;
const SCORE_ARC_LENGTH = 339.292;

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
          <span class="lookup-eyebrow">الثانوية العامة</span>
          <h1 class="lookup-title">نتيجة الثانوية برقم الجلوس</h1>
          @if (!result) {
            <p class="lookup-lead">
              اكتب رقم جلوسك وشوف نتيجتك — وبعدها اعرف الكليات المتاحة
              لمجموعك.
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
        <section class="result-certificate" aria-label="نتيجة الطالب">
          <header class="cert-header">
            <div class="cert-header-inner">
              <span class="cert-year">نتيجة {{ result.year }}</span>
              <h2 class="cert-name">{{ result.arabicName }}</h2>
              <p class="cert-seating">
                <span class="cert-seating-label">رقم الجلوس</span>
                <span class="cert-seating-no">{{ result.seatingNo }}</span>
              </p>
            </div>
          </header>

          <div class="cert-perforation" aria-hidden="true"></div>

          <div class="cert-body">
            <div
              class="score-gauge"
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
              <svg
                class="score-gauge-svg"
                viewBox="0 0 120 120"
                aria-hidden="true"
              >
                <circle class="score-gauge-track" cx="60" cy="60" r="54" />
                <circle
                  class="score-gauge-fill"
                  cx="60"
                  cy="60"
                  r="54"
                  [style.stroke-dashoffset]="
                    scoreArcOffset(result.totalDegree)
                  "
                />
              </svg>
              <div class="score-gauge-content">
                <span class="score-fraction">
                  <span class="score-value">{{ result.totalDegree }}</span>
                  <span class="score-max">/{{ thanaweyaMaxScore }}</span>
                </span>
                <span class="score-percentage"
                  >{{ resultPercentage(result.totalDegree) }}%</span
                >
                <span class="score-label">المجموع الكلي</span>
              </div>
            </div>

            <div class="cert-meta">
              <article class="cert-meta-item">
                <span class="cert-meta-icon" aria-hidden="true">
                  <svg
                    width="18"
                    height="18"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    stroke-width="2"
                  >
                    <path d="M20 21v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2" />
                    <circle cx="12" cy="7" r="4" />
                  </svg>
                </span>
                <div class="cert-meta-text">
                  <span class="cert-meta-label">حالة الطالب</span>
                  <span class="cert-meta-value">{{
                    result.studentCaseDesc
                  }}</span>
                </div>
              </article>
              <article class="cert-meta-item">
                <span class="cert-meta-icon" aria-hidden="true">
                  <svg
                    width="18"
                    height="18"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    stroke-width="2"
                  >
                    <rect x="3" y="4" width="18" height="18" rx="2" />
                    <path d="M16 2v4M8 2v4M3 10h18" />
                  </svg>
                </span>
                <div class="cert-meta-text">
                  <span class="cert-meta-label">سنة النتيجة</span>
                  <span class="cert-meta-value">{{ result.year }}</span>
                </div>
              </article>
            </div>

            <div class="cert-next">
              <p class="cert-next-hint">الخطوة التالية</p>
              <a
                class="cert-cta"
                [routerLink]="['/predict']"
                [queryParams]="{ score: result.totalDegree }"
              >
                <span>اعرف الكليات المتاحة لمجموعك</span>
                <span class="cert-cta-arrow" aria-hidden="true">←</span>
              </a>
            </div>
          </div>
        </section>
      }
    </div>
  `,
  styles: [
    `
      .thanaweya-page {
        padding-bottom: 2.5rem;
      }

      /* ── Lookup stage ── */
      .lookup-stage {
        position: relative;
        display: grid;
        gap: 1.5rem;
        justify-items: center;
        text-align: center;
        padding: 2.25rem 1.75rem 2rem;
        margin-bottom: 1.75rem;
        border-radius: 22px;
        background:
          radial-gradient(
            circle at 12% 18%,
            rgba(245, 158, 11, 0.09) 0%,
            transparent 42%
          ),
          radial-gradient(
            circle at 88% 82%,
            rgba(37, 99, 235, 0.07) 0%,
            transparent 38%
          ),
          linear-gradient(165deg, #f8faff 0%, #eef2ff 52%, #fff 100%);
        border: 1px solid rgba(30, 58, 138, 0.08);
        box-shadow: 0 14px 40px rgba(15, 23, 42, 0.06);
        transition:
          padding 0.35s ease,
          margin 0.35s ease;
      }

      .lookup-stage--compact {
        padding: 1.35rem 1.25rem 1.25rem;
        margin-bottom: 1.25rem;
        gap: 1rem;
      }

      .lookup-stage--compact .lookup-title {
        font-size: clamp(1.1rem, 3.5vw, 1.35rem);
        margin-bottom: 0;
      }

      .lookup-stage-copy {
        max-width: 480px;
      }

      .lookup-eyebrow {
        display: inline-block;
        font-size: 0.78rem;
        font-weight: 700;
        letter-spacing: 0.06em;
        text-transform: uppercase;
        color: #92400e;
        background: rgba(245, 158, 11, 0.14);
        border: 1px solid rgba(217, 119, 6, 0.28);
        padding: 0.28rem 0.85rem;
        border-radius: 999px;
        margin-bottom: 0.85rem;
      }

      .lookup-title {
        font-family: var(--font-display);
        font-size: clamp(1.45rem, 4.5vw, 2.05rem);
        font-weight: 800;
        color: var(--color-primary);
        line-height: 1.32;
        margin: 0 0 0.7rem;
      }

      .lookup-lead {
        margin: 0;
        color: var(--color-text-muted);
        line-height: 1.75;
        font-size: clamp(0.94rem, 2.5vw, 1.02rem);
        max-width: 42ch;
        margin-inline: auto;
      }

      .lookup-form {
        width: 100%;
        max-width: 380px;
        display: grid;
        gap: 0.85rem;
      }

      .lookup-label {
        font-size: 0.88rem;
        font-weight: 700;
        color: var(--color-primary);
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
        font-size: 1.1rem;
        font-weight: 800;
        color: rgba(30, 58, 138, 0.28);
        pointer-events: none;
      }

      .lookup-input-wrap input {
        width: 100%;
        text-align: center;
        font-family: var(--font-display);
        font-size: 1.35rem;
        letter-spacing: 0.14em;
        font-weight: 700;
        padding: 0.95rem 2.5rem;
        border: 2px solid rgba(30, 58, 138, 0.14);
        border-radius: 14px;
        background: #fff;
        box-shadow: inset 0 2px 6px rgba(15, 23, 42, 0.04);
        transition:
          border-color 0.2s ease,
          box-shadow 0.2s ease;
      }

      .lookup-input-wrap input:focus {
        outline: none;
        border-color: var(--color-primary-light);
        box-shadow:
          inset 0 2px 6px rgba(15, 23, 42, 0.04),
          0 0 0 4px rgba(37, 99, 235, 0.12);
      }

      .lookup-submit {
        width: 100%;
        display: inline-flex;
        align-items: center;
        justify-content: center;
        gap: 0.55rem;
        margin-top: 0.15rem;
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
        font-size: 0.88rem;
        text-align: center;
        margin: -0.25rem 0 0;
      }

      .lookup-notice {
        padding: 0.75rem 1rem;
        border-radius: 12px;
        background: #fffbeb;
        border: 1px solid #fde68a;
        color: #92400e;
        font-size: 0.92rem;
        line-height: 1.6;
        text-align: center;
      }

      /* ── Certificate result ── */
      .result-certificate {
        max-width: 480px;
        margin: 0 auto;
        border-radius: 22px;
        overflow: hidden;
        background: #fefdfb;
        border: 1px solid rgba(30, 58, 138, 0.1);
        box-shadow:
          0 24px 60px rgba(15, 23, 42, 0.14),
          0 0 0 1px rgba(255, 255, 255, 0.6) inset;
        animation: cert-enter 0.55s cubic-bezier(0.22, 1, 0.36, 1);
      }

      .cert-header {
        position: relative;
        padding: 1.85rem 1.5rem 1.65rem;
        background: linear-gradient(
          148deg,
          #0b1533 0%,
          #152a5c 45%,
          #1e3a8a 100%
        );
        color: #fff;
        text-align: center;
        overflow: hidden;
      }

      .cert-header::before {
        content: '';
        position: absolute;
        inset: 0;
        background-image: radial-gradient(
          circle,
          rgba(255, 255, 255, 0.06) 1px,
          transparent 1px
        );
        background-size: 18px 18px;
        opacity: 0.45;
        pointer-events: none;
      }

      .cert-header::after {
        content: '';
        position: absolute;
        inset: auto -20% -60% -20%;
        height: 180px;
        border-radius: 50%;
        background: radial-gradient(
          circle,
          rgba(245, 158, 11, 0.22) 0%,
          transparent 68%
        );
        pointer-events: none;
      }

      .cert-header-inner {
        position: relative;
        z-index: 1;
      }

      .cert-year {
        display: inline-block;
        font-size: 0.76rem;
        font-weight: 700;
        letter-spacing: 0.05em;
        padding: 0.28rem 0.8rem;
        border-radius: 999px;
        background: rgba(245, 158, 11, 0.18);
        border: 1px solid rgba(245, 158, 11, 0.35);
        color: #fde68a;
        margin-bottom: 0.85rem;
      }

      .cert-name {
        font-family: var(--font-display);
        font-size: clamp(1.25rem, 4.5vw, 1.65rem);
        font-weight: 800;
        line-height: 1.4;
        margin: 0 0 0.85rem;
      }

      .cert-seating {
        display: inline-flex;
        flex-direction: column;
        align-items: center;
        gap: 0.2rem;
        margin: 0;
        padding: 0.55rem 1.1rem;
        border-radius: 12px;
        background: rgba(255, 255, 255, 0.08);
        border: 1px solid rgba(255, 255, 255, 0.12);
      }

      .cert-seating-label {
        font-size: 0.72rem;
        opacity: 0.72;
        letter-spacing: 0.04em;
      }

      .cert-seating-no {
        font-family: var(--font-display);
        font-size: 1.15rem;
        font-weight: 800;
        letter-spacing: 0.12em;
        color: var(--color-accent-light);
      }

      .cert-perforation {
        height: 14px;
        background:
          radial-gradient(circle at 7px 7px, #fefdfb 6px, transparent 6.5px),
          linear-gradient(180deg, #152a5c 0%, #fefdfb 100%);
        background-size:
          14px 14px,
          100% 100%;
        background-repeat: repeat-x, no-repeat;
        background-position:
          center top,
          center;
      }

      .cert-body {
        padding: 1.65rem 1.4rem 1.75rem;
        text-align: center;
        animation: cert-body-enter 0.6s cubic-bezier(0.22, 1, 0.36, 1) 0.12s
          both;
      }

      .score-gauge {
        position: relative;
        width: clamp(168px, 44vw, 196px);
        height: clamp(168px, 44vw, 196px);
        margin: 0 auto 1.5rem;
      }

      .score-gauge-svg {
        width: 100%;
        height: 100%;
        transform: rotate(-90deg);
      }

      .score-gauge-track,
      .score-gauge-fill {
        fill: none;
        stroke-width: 7;
        stroke-linecap: round;
      }

      .score-gauge-track {
        stroke: rgba(30, 58, 138, 0.1);
      }

      .score-gauge-fill {
        stroke: var(--color-accent);
        stroke-dasharray: 339.292;
        transition: stroke-dashoffset 1s cubic-bezier(0.22, 1, 0.36, 1);
      }

      .score-gauge-content {
        position: absolute;
        inset: 0;
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: 0.15rem;
      }

      .score-fraction {
        display: flex;
        align-items: baseline;
        gap: 0.1rem;
        line-height: 1;
      }

      .score-value {
        font-family: var(--font-display);
        font-size: clamp(2.1rem, 7vw, 2.75rem);
        font-weight: 800;
        color: var(--color-primary);
      }

      .score-max {
        font-family: var(--font-display);
        font-size: clamp(0.95rem, 3vw, 1.1rem);
        font-weight: 700;
        color: var(--color-text-muted);
      }

      .score-percentage {
        font-family: var(--font-display);
        font-size: clamp(1.05rem, 3.5vw, 1.2rem);
        font-weight: 800;
        color: var(--color-accent);
        line-height: 1;
      }

      .score-label {
        margin-top: 0.15rem;
        font-size: 0.76rem;
        font-weight: 600;
        color: var(--color-text-muted);
        letter-spacing: 0.02em;
      }

      .cert-meta {
        display: grid;
        grid-template-columns: repeat(2, minmax(0, 1fr));
        gap: 0.75rem;
        margin-bottom: 1.5rem;
      }

      .cert-meta-item {
        display: flex;
        align-items: flex-start;
        gap: 0.65rem;
        padding: 0.85rem 0.75rem;
        text-align: right;
        background: var(--color-surface);
        border: 1px solid rgba(30, 58, 138, 0.08);
        border-radius: 14px;
      }

      .cert-meta-icon {
        flex-shrink: 0;
        width: 34px;
        height: 34px;
        display: grid;
        place-items: center;
        border-radius: 10px;
        background: rgba(37, 99, 235, 0.08);
        color: var(--color-primary);
      }

      .cert-meta-text {
        display: grid;
        gap: 0.15rem;
        min-width: 0;
      }

      .cert-meta-label {
        font-size: 0.72rem;
        color: var(--color-text-muted);
      }

      .cert-meta-value {
        font-weight: 700;
        font-size: 0.88rem;
        color: var(--color-primary);
        line-height: 1.4;
        overflow-wrap: anywhere;
      }

      .cert-next {
        padding-top: 0.25rem;
        border-top: 1px dashed rgba(30, 58, 138, 0.14);
      }

      .cert-next-hint {
        margin: 0 0 0.75rem;
        font-size: 0.78rem;
        font-weight: 700;
        letter-spacing: 0.04em;
        color: var(--color-text-muted);
      }

      .cert-cta {
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 0.65rem;
        width: 100%;
        padding: 0.95rem 1.25rem;
        border-radius: 14px;
        font-weight: 700;
        font-size: 1rem;
        color: #fff;
        background: linear-gradient(
          135deg,
          var(--color-primary-light) 0%,
          var(--color-primary) 100%
        );
        box-shadow: 0 8px 24px rgba(37, 99, 235, 0.32);
        transition:
          transform 0.2s ease,
          box-shadow 0.2s ease;
      }

      .cert-cta:hover {
        transform: translateY(-2px);
        box-shadow: 0 12px 32px rgba(37, 99, 235, 0.38);
      }

      .cert-cta-arrow {
        font-size: 1.2rem;
        font-weight: 800;
        color: var(--color-accent-light);
        transition: transform 0.2s ease;
      }

      .cert-cta:hover .cert-cta-arrow {
        transform: translateX(-4px);
      }

      @keyframes cert-enter {
        from {
          opacity: 0;
          transform: translateY(20px) scale(0.98);
        }
        to {
          opacity: 1;
          transform: translateY(0) scale(1);
        }
      }

      @keyframes cert-body-enter {
        from {
          opacity: 0;
          transform: translateY(10px);
        }
        to {
          opacity: 1;
          transform: translateY(0);
        }
      }

      @media (max-width: 480px) {
        .lookup-stage {
          padding: 1.5rem 1rem 1.35rem;
          border-radius: 18px;
        }

        .lookup-stage--compact {
          padding: 1.1rem 0.95rem 1rem;
        }

        .lookup-input-wrap input {
          font-size: 1.2rem;
          padding: 0.85rem 2.25rem;
        }

        .cert-header {
          padding: 1.45rem 1rem 1.35rem;
        }

        .cert-body {
          padding: 1.35rem 1rem 1.5rem;
        }

        .cert-meta {
          grid-template-columns: 1fr;
        }

        .cert-meta-item {
          padding: 0.75rem;
        }
      }

      @media (prefers-reduced-motion: reduce) {
        .result-certificate,
        .cert-body {
          animation: none;
        }

        .score-gauge-fill {
          transition: none;
        }

        .lookup-spinner {
          animation: none;
        }

        .cert-cta:hover {
          transform: none;
        }

        .cert-cta:hover .cert-cta-arrow {
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

  loading = false;
  error = '';
  result: StudentResult | null = null;

  form = this.fb.group({
    seatingNo: ['', [Validators.required, digitsOnlyValidator()]],
  });

  resultPercentage(totalDegree: number): string {
    return ((totalDegree / THANAWEYA_MAX_SCORE) * 100).toFixed(2);
  }

  scoreArcOffset(totalDegree: number): number {
    const progress = Math.min(Math.max(totalDegree / THANAWEYA_MAX_SCORE, 0), 1);
    return SCORE_ARC_LENGTH * (1 - progress);
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
}
