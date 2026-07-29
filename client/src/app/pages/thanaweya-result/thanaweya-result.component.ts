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
        <section class="result-certificate" aria-label="نتيجة الطالب">
          <header class="cert-header">
            <div class="cert-header-inner">
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
                <defs>
                  <linearGradient
                    id="scoreArcGradient"
                    x1="0%"
                    y1="0%"
                    x2="100%"
                    y2="100%"
                  >
                    <stop offset="0%" stop-color="#fbbf24" />
                    <stop offset="100%" stop-color="#d97706" />
                  </linearGradient>
                </defs>
                <circle class="score-gauge-track" cx="60" cy="60" r="54" />
                <circle
                  class="score-gauge-fill"
                  cx="60"
                  cy="60"
                  r="54"
                  [style.stroke-dashoffset]="scoreArcOffset(result.totalDegree)"
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
                [queryParams]="predictQueryParams()"
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
        max-width: 520px;
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

      .lookup-stage--compact .lookup-eyebrow {
        margin-bottom: 0.5rem;
      }

      .lookup-stage--compact .lookup-title {
        font-size: clamp(1.05rem, 3.5vw, 1.25rem);
        margin-bottom: 0;
      }

      .lookup-stage-copy {
        max-width: 440px;
      }

      .lookup-eyebrow {
        display: inline-block;
        font-size: 0.75rem;
        font-weight: 700;
        color: #92400e;
        background: #fffbeb;
        border: 1px solid #fde68a;
        padding: 0.25rem 0.75rem;
        border-radius: 999px;
        margin-bottom: 0.75rem;
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

      /* ── Result certificate ── */
      .result-certificate {
        border-radius: 20px;
        overflow: hidden;
        background: #fff;
        border: 1px solid rgba(30, 58, 138, 0.08);
        box-shadow:
          0 1px 2px rgba(15, 23, 42, 0.04),
          0 20px 50px rgba(15, 23, 42, 0.1);
        animation: cert-enter 0.5s cubic-bezier(0.22, 1, 0.36, 1);
      }

      .cert-header {
        position: relative;
        padding: 1.65rem 1.35rem 1.5rem;
        background: linear-gradient(160deg, #0f172a 0%, #1e3a8a 100%);
        color: #fff;
        text-align: center;
      }

      .cert-header::after {
        content: '';
        position: absolute;
        inset: auto 0 0;
        height: 3px;
        background: linear-gradient(
          90deg,
          transparent,
          var(--color-accent-light),
          transparent
        );
      }

      .cert-header-inner {
        position: relative;
        z-index: 1;
      }

      .cert-year {
        display: inline-block;
        font-size: 0.72rem;
        font-weight: 700;
        padding: 0.22rem 0.7rem;
        border-radius: 999px;
        background: rgba(245, 158, 11, 0.15);
        border: 1px solid rgba(245, 158, 11, 0.3);
        color: #fde68a;
        margin-bottom: 0.75rem;
      }

      .cert-name {
        font-family: var(--font-display);
        font-size: clamp(1.2rem, 4vw, 1.5rem);
        font-weight: 800;
        line-height: 1.45;
        margin: 0 0 0.85rem;
      }

      .cert-seating {
        display: inline-flex;
        flex-direction: column;
        align-items: center;
        gap: 0.15rem;
        margin: 0;
        padding: 0.5rem 1rem;
        border-radius: 10px;
        background: rgba(255, 255, 255, 0.07);
        border: 1px solid rgba(255, 255, 255, 0.1);
      }

      .cert-seating-label {
        font-size: 0.68rem;
        opacity: 0.7;
      }

      .cert-seating-no {
        font-family: var(--font-display);
        font-size: 1.1rem;
        font-weight: 800;
        letter-spacing: 0.1em;
        color: var(--color-accent-light);
      }

      .cert-perforation {
        height: 10px;
        background:
          radial-gradient(circle at 5px 5px, #fff 4px, transparent 4.5px),
          #1e3a8a;
        background-size: 10px 10px;
        background-repeat: repeat-x;
        background-position: center;
        opacity: 0.85;
      }

      .cert-body {
        padding: 1.5rem 1.25rem 1.65rem;
        text-align: center;
        animation: cert-body-enter 0.55s cubic-bezier(0.22, 1, 0.36, 1) 0.1s
          both;
      }

      .score-gauge {
        position: relative;
        width: clamp(156px, 40vw, 180px);
        height: clamp(156px, 40vw, 180px);
        margin: 0 auto 1.35rem;
      }

      .score-gauge::before {
        content: '';
        position: absolute;
        inset: 12%;
        border-radius: 50%;
        background: radial-gradient(
          circle,
          rgba(245, 158, 11, 0.06) 0%,
          transparent 70%
        );
      }

      .score-gauge-svg {
        width: 100%;
        height: 100%;
        transform: rotate(-90deg);
        filter: drop-shadow(0 2px 6px rgba(217, 119, 6, 0.15));
      }

      .score-gauge-track,
      .score-gauge-fill {
        fill: none;
        stroke-width: 6;
        stroke-linecap: round;
      }

      .score-gauge-track {
        stroke: #eef2ff;
      }

      .score-gauge-fill {
        stroke: url(#scoreArcGradient);
        stroke-dasharray: 339.292;
        transition: stroke-dashoffset 0.9s cubic-bezier(0.22, 1, 0.36, 1);
      }

      .score-gauge-content {
        position: absolute;
        inset: 0;
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: 0.1rem;
      }

      .score-fraction {
        display: flex;
        align-items: baseline;
        gap: 0.08rem;
        line-height: 1;
      }

      .score-value {
        font-family: var(--font-display);
        font-size: clamp(1.95rem, 6.5vw, 2.5rem);
        font-weight: 800;
        color: var(--color-primary);
      }

      .score-max {
        font-family: var(--font-display);
        font-size: clamp(0.88rem, 2.8vw, 1rem);
        font-weight: 600;
        color: var(--color-text-muted);
      }

      .score-percentage {
        font-family: var(--font-display);
        font-size: clamp(0.95rem, 3vw, 1.1rem);
        font-weight: 800;
        color: var(--color-accent);
        line-height: 1;
        margin-top: 0.1rem;
      }

      .score-label {
        margin-top: 0.2rem;
        font-size: 0.72rem;
        font-weight: 600;
        color: var(--color-text-muted);
      }

      .cert-meta {
        display: grid;
        grid-template-columns: repeat(2, minmax(0, 1fr));
        gap: 0.65rem;
        margin-bottom: 1.35rem;
      }

      .cert-meta-item {
        display: flex;
        align-items: flex-start;
        gap: 0.6rem;
        padding: 0.8rem 0.7rem;
        text-align: right;
        background: var(--color-surface);
        border: 1px solid rgba(30, 58, 138, 0.06);
        border-radius: 12px;
      }

      .cert-meta-icon {
        flex-shrink: 0;
        width: 32px;
        height: 32px;
        display: grid;
        place-items: center;
        border-radius: 8px;
        background: #fff;
        color: var(--color-primary);
        box-shadow: 0 1px 4px rgba(15, 23, 42, 0.06);
      }

      .cert-meta-text {
        display: grid;
        gap: 0.12rem;
        min-width: 0;
      }

      .cert-meta-label {
        font-size: 0.68rem;
        color: var(--color-text-muted);
      }

      .cert-meta-value {
        font-weight: 700;
        font-size: 0.84rem;
        color: var(--color-primary);
        line-height: 1.4;
        overflow-wrap: anywhere;
      }

      .cert-next {
        padding-top: 1.15rem;
        border-top: 1px solid rgba(30, 58, 138, 0.08);
      }

      .cert-next-hint {
        margin: 0 0 0.65rem;
        font-size: 0.72rem;
        font-weight: 700;
        color: var(--color-text-muted);
      }

      .cert-cta {
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 0.55rem;
        width: 100%;
        padding: 0.9rem 1.15rem;
        border-radius: 12px;
        font-weight: 700;
        font-size: 0.95rem;
        color: #fff;
        background: var(--color-primary-light);
        box-shadow: 0 4px 16px rgba(37, 99, 235, 0.25);
        transition:
          background 0.2s ease,
          box-shadow 0.2s ease,
          transform 0.2s ease;
      }

      .cert-cta:hover {
        background: var(--color-primary);
        transform: translateY(-1px);
        box-shadow: 0 8px 24px rgba(37, 99, 235, 0.3);
      }

      .cert-cta-arrow {
        font-size: 1.1rem;
        color: var(--color-accent-light);
        transition: transform 0.2s ease;
      }

      .cert-cta:hover .cert-cta-arrow {
        transform: translateX(-3px);
      }

      @keyframes cert-enter {
        from {
          opacity: 0;
          transform: translateY(16px);
        }
        to {
          opacity: 1;
          transform: translateY(0);
        }
      }

      @keyframes cert-body-enter {
        from {
          opacity: 0;
          transform: translateY(8px);
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

        .cert-header {
          padding: 1.35rem 1rem 1.25rem;
        }

        .cert-body {
          padding: 1.25rem 1rem 1.4rem;
        }

        .cert-meta {
          grid-template-columns: 1fr;
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
    const progress = Math.min(
      Math.max(totalDegree / THANAWEYA_MAX_SCORE, 0),
      1,
    );
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
