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
const GENERIC_ERROR_MESSAGE =
  'حدث خطأ أثناء البحث. حاول مرة أخرى لاحقًا.';
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

      <section class="lookup-hero hero-gradient">
        <div class="lookup-hero-text">
          <span class="lookup-eyebrow">الثانوية العامة</span>
          <h1 class="lookup-title">نتيجة الثانوية برقم الجلوس</h1>
          <p class="lookup-lead">
            اكتب رقم جلوسك وشوف نتيجتك — وبعدها اعرف الكليات المتاحة لمجموعك.
          </p>
        </div>

        <form class="lookup-form card" [formGroup]="form" (ngSubmit)="submit()">
          <div class="form-group lookup-field">
            <label for="seatingNo" class="lookup-label">رقم الجلوس</label>
            <input
              id="seatingNo"
              type="text"
              inputmode="numeric"
              pattern="[0-9]*"
              formControlName="seatingNo"
              placeholder="مثال: 1234567"
              autocomplete="off"
              (input)="onSeatingInput($event)"
            />
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
          </div>

          @if (error) {
            <div class="disclaimer-box lookup-notice" role="status">
              {{ error }}
            </div>
          }

          <button
            class="btn btn-primary btn-lg lookup-submit"
            type="submit"
            [disabled]="loading"
          >
            {{ loading ? 'جاري البحث...' : 'اعرض النتيجة' }}
          </button>
        </form>
      </section>

      @if (result) {
        <section class="student-result" aria-label="نتيجة الطالب">
          <div class="student-result-banner">
            <div class="student-result-banner-inner">
              <span class="student-result-badge">نتيجة {{ result.year }}</span>
              <h2 class="student-result-name">{{ result.arabicName }}</h2>
              <p class="student-result-seating">
                رقم الجلوس: <strong>{{ result.seatingNo }}</strong>
              </p>
            </div>
          </div>

          <div class="student-result-body card">
            <div
              class="student-result-score-ring"
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
              <div class="score-ring-inner">
                <span class="score-value">{{ result.totalDegree }}</span>
                <span class="score-percentage"
                  >{{ resultPercentage(result.totalDegree) }}%</span
                >
                <span class="score-label"
                  >المجموع الكلي من {{ thanaweyaMaxScore }}</span
                >
              </div>
            </div>

            <div class="student-result-meta">
              <div class="meta-chip">
                <span class="meta-chip-label">حالة الطالب</span>
                <span class="meta-chip-value">{{ result.studentCaseDesc }}</span>
              </div>
              <div class="meta-chip">
                <span class="meta-chip-label">سنة النتيجة</span>
                <span class="meta-chip-value">{{ result.year }}</span>
              </div>
            </div>

            <a
              class="btn btn-primary btn-lg student-result-cta"
              [routerLink]="['/predict']"
              [queryParams]="{ score: result.totalDegree }"
            >
              اعرف الكليات المتاحة لمجموعك
            </a>
          </div>
        </section>
      }
    </div>
  `,
  styles: [
    `
      .thanaweya-page {
        padding-bottom: 1rem;
      }

      .lookup-hero {
        display: flex;
        flex-direction: column;
        align-items: center;
        text-align: center;
        padding: 2rem 1.5rem 1.75rem;
        margin-bottom: 1.5rem;
      }

      .lookup-hero-text {
        max-width: 560px;
        margin-bottom: 0.25rem;
      }

      .lookup-eyebrow {
        display: inline-block;
        font-size: 0.82rem;
        font-weight: 700;
        letter-spacing: 0.04em;
        color: var(--color-accent);
        background: rgba(217, 119, 6, 0.1);
        border: 1px solid rgba(217, 119, 6, 0.22);
        padding: 0.25rem 0.75rem;
        border-radius: 999px;
        margin-bottom: 0.75rem;
      }

      .lookup-title {
        font-family: var(--font-display);
        font-size: clamp(1.35rem, 4vw, 2rem);
        font-weight: 800;
        color: var(--color-primary);
        line-height: 1.35;
        margin: 0 0 0.65rem;
      }

      .lookup-lead {
        margin: 0 auto 1.25rem;
        color: var(--color-text-muted);
        line-height: 1.75;
        font-size: clamp(0.95rem, 2.5vw, 1.02rem);
        max-width: 46ch;
      }

      .lookup-form {
        width: 100%;
        max-width: 420px;
        padding: 1.35rem;
        margin: 0 auto;
      }

      .lookup-label {
        text-align: center;
        margin-bottom: 0.5rem;
      }

      .lookup-field {
        margin-bottom: 1.15rem;
      }

      .lookup-field input {
        text-align: center;
        font-size: 1.15rem;
        letter-spacing: 0.06em;
        font-weight: 600;
      }

      .lookup-submit {
        width: 100%;
        display: inline-flex;
        align-items: center;
        justify-content: center;
        gap: 0.5rem;
      }

      .field-error {
        color: #dc2626;
        display: block;
        margin-top: 0.35rem;
        font-size: 0.9rem;
        text-align: center;
      }

      .lookup-notice {
        text-align: center;
        margin-bottom: 1rem;
        line-height: 1.65;
        font-size: 0.95rem;
      }

      /* ── Student result card ── */
      .student-result {
        max-width: 520px;
        margin: 0 auto 2rem;
        animation: result-enter 0.45s ease;
      }

      .student-result-banner {
        background: linear-gradient(
          145deg,
          #0f172a 0%,
          #1e3a8a 55%,
          #1d4ed8 100%
        );
        border-radius: 20px 20px 0 0;
        padding: 1.75rem 1.5rem 2.5rem;
        position: relative;
        overflow: hidden;
        color: #fff;
        text-align: center;
      }

      .student-result-banner::before {
        content: '';
        position: absolute;
        inset: -30% auto auto 50%;
        width: 280px;
        height: 280px;
        transform: translateX(-50%);
        border-radius: 50%;
        background: radial-gradient(
          circle,
          rgba(245, 158, 11, 0.18) 0%,
          transparent 70%
        );
        pointer-events: none;
      }

      .student-result-banner-inner {
        position: relative;
        z-index: 1;
      }

      .student-result-badge {
        display: inline-block;
        font-size: 0.78rem;
        font-weight: 700;
        letter-spacing: 0.03em;
        background: rgba(255, 255, 255, 0.12);
        border: 1px solid rgba(255, 255, 255, 0.18);
        padding: 0.25rem 0.75rem;
        border-radius: 999px;
        margin-bottom: 0.75rem;
      }

      .student-result-name {
        font-family: var(--font-display);
        font-size: clamp(1.2rem, 4vw, 1.55rem);
        font-weight: 800;
        margin: 0 0 0.5rem;
        line-height: 1.45;
      }

      .student-result-seating {
        margin: 0;
        font-size: 0.92rem;
        opacity: 0.85;
      }

      .student-result-seating strong {
        font-weight: 700;
        color: var(--color-accent-light);
        letter-spacing: 0.04em;
      }

      .student-result-body {
        margin-top: -1.75rem;
        position: relative;
        z-index: 2;
        padding: 1.5rem 1.35rem 1.75rem;
        border-radius: 20px;
        text-align: center;
        box-shadow: 0 16px 48px rgba(15, 23, 42, 0.14);
      }

      .student-result-score-ring {
        display: flex;
        justify-content: center;
        margin-bottom: 1.35rem;
      }

      .score-ring-inner {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        width: clamp(140px, 38vw, 168px);
        height: clamp(140px, 38vw, 168px);
        border-radius: 50%;
        background: linear-gradient(
          145deg,
          #fff 0%,
          var(--color-surface-alt) 100%
        );
        border: 4px solid var(--color-accent-light);
        box-shadow:
          0 0 0 6px rgba(245, 158, 11, 0.12),
          0 12px 32px rgba(30, 58, 138, 0.12);
      }

      .score-value {
        font-family: var(--font-display);
        font-size: clamp(2rem, 7vw, 2.65rem);
        font-weight: 800;
        line-height: 1;
        color: var(--color-primary);
      }

      .score-percentage {
        margin-top: 0.2rem;
        font-family: var(--font-display);
        font-size: clamp(1rem, 3.5vw, 1.15rem);
        font-weight: 700;
        line-height: 1;
        color: var(--color-accent);
      }

      .score-label {
        margin-top: 0.35rem;
        font-size: 0.78rem;
        font-weight: 600;
        color: var(--color-text-muted);
        line-height: 1.35;
      }

      .student-result-meta {
        display: grid;
        gap: 0.65rem;
        margin-bottom: 1.35rem;
      }

      .meta-chip {
        display: flex;
        justify-content: space-between;
        align-items: center;
        gap: 0.75rem;
        padding: 0.75rem 1rem;
        background: var(--color-surface);
        border: 1px solid rgba(30, 58, 138, 0.08);
        border-radius: 12px;
      }

      .meta-chip-label {
        font-size: 0.88rem;
        color: var(--color-text-muted);
        flex-shrink: 0;
      }

      .meta-chip-value {
        font-weight: 700;
        font-size: 0.92rem;
        color: var(--color-primary);
        text-align: left;
      }

      .student-result-cta {
        width: 100%;
        display: inline-flex;
        align-items: center;
        justify-content: center;
      }

      @keyframes result-enter {
        from {
          opacity: 0;
          transform: translateY(12px);
        }
        to {
          opacity: 1;
          transform: translateY(0);
        }
      }

      @media (max-width: 480px) {
        .lookup-hero {
          padding: 1.35rem 1rem 1.25rem;
        }

        .lookup-form {
          padding: 1.15rem;
        }

        .student-result-banner {
          padding: 1.35rem 1rem 2.25rem;
          border-radius: 16px 16px 0 0;
        }

        .student-result-body {
          padding: 1.25rem 1rem 1.5rem;
          border-radius: 16px;
        }

        .meta-chip {
          flex-direction: column;
          align-items: stretch;
          text-align: center;
          gap: 0.25rem;
        }

        .meta-chip-value {
          text-align: center;
        }
      }

      @media (prefers-reduced-motion: reduce) {
        .student-result {
          animation: none;
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
