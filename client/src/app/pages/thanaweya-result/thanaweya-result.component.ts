import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../api.service';
import { StudentResult } from '../../models';
import {
  applyDigitsOnlyInput,
  digitsOnlyValidator,
} from '../../form-validators';

@Component({
  selector: 'app-thanaweya-result',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="container">
      <nav class="breadcrumb" aria-label="مسار التنقل">
        <a routerLink="/">الرئيسية</a>
        <span class="breadcrumb-sep">/</span>
        <span>نتيجة الثانوية</span>
      </nav>

      <section class="lookup-hero hero-gradient">
        <div class="lookup-hero-text">
          <h1 class="lookup-title">نتيجة الثانوية العامة برقم الجلوس</h1>
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
            <div class="error">{{ error }}</div>
          }

          <button
            class="btn btn-primary btn-lg lookup-submit"
            type="submit"
            [disabled]="loading"
          >
            @if (loading) {
              <span class="btn-spinner" aria-hidden="true"></span>
              جاري البحث...
            } @else {
              اعرض النتيجة
            }
          </button>
        </form>
      </section>

      @if (loading) {
        <section class="thanaweya-loading" aria-label="جاري تحميل النتيجة">
          <div class="skeleton-card"></div>
          <div class="skeleton-card skeleton-card--short"></div>
        </section>
      }

      @if (result && !loading) {
        <section class="thanaweya-result-panel" aria-live="polite">
          <div class="thanaweya-summary" aria-label="ملخص نتيجة الطالب">
            <div class="thanaweya-score-block">
              <div class="thanaweya-score-value">{{ result.totalDegree }}</div>
              <div class="thanaweya-score-label">المجموع الكلي</div>
            </div>
            <div class="thanaweya-summary-text">
              <span
                class="thanaweya-status"
                [class]="statusClass(result.studentCaseDesc)"
              >
                {{ result.studentCaseDesc }}
              </span>
              <h2 class="thanaweya-student-name">{{ result.arabicName }}</h2>
              <dl class="thanaweya-meta">
                <div class="thanaweya-meta-item">
                  <dt>رقم الجلوس</dt>
                  <dd>{{ result.seatingNo }}</dd>
                </div>
                <div class="thanaweya-meta-item">
                  <dt>سنة النتيجة</dt>
                  <dd>{{ result.year }}</dd>
                </div>
              </dl>
            </div>
          </div>

          <div class="thanaweya-next-step">
            <div class="thanaweya-next-step-text">
              <h3>الخطوة الجاية</h3>
              <p>اعرف الكليات اللي مجموعك يسمح بيها حسب حدود القبول الرسمية.</p>
            </div>
            <a
              [routerLink]="['/predict']"
              [queryParams]="{ score: result.totalDegree }"
              class="btn btn-primary btn-lg thanaweya-cta"
            >
              اعرف كليتك من مجموعك
              <svg
                width="18"
                height="18"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="2.5"
                aria-hidden="true"
              >
                <path d="M5 12h14M12 5l7 7-7 7" />
              </svg>
            </a>
          </div>
        </section>
      } @else if (searched && !loading && !error) {
        <section class="results-empty thanaweya-empty">
          <div class="results-empty-icon" aria-hidden="true">🔍</div>
          <h3>مفيش نتيجة لرقم الجلوس ده</h3>
          <p>تأكد إنك كتبت الرقم صح وحاول تاني.</p>
          <button
            type="button"
            class="btn btn-secondary"
            (click)="resetSearch()"
          >
            جرب رقم تاني
          </button>
        </section>
      }
    </div>
  `,
  styles: [
    `
      .lookup-hero {
        display: flex;
        flex-direction: column;
        align-items: center;
        text-align: center;
        padding: 2.5rem 1.75rem 2rem;
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
        letter-spacing: 0.02em;
        color: var(--color-accent);
        background: rgba(217, 119, 6, 0.1);
        border: 1px solid rgba(217, 119, 6, 0.25);
        border-radius: 999px;
        padding: 0.25rem 0.75rem;
        margin-bottom: 0.85rem;
      }

      .lookup-title {
        font-family: var(--font-display);
        font-size: clamp(1.4rem, 3.5vw, 2rem);
        font-weight: 800;
        color: var(--color-primary);
        line-height: 1.35;
        margin: 0 0 0.65rem;
      }

      .lookup-lead {
        margin: 0 auto 1.5rem;
        color: var(--color-text-muted);
        line-height: 1.75;
        font-size: 1.02rem;
        max-width: 46ch;
      }

      .lookup-form {
        width: 100%;
        max-width: 420px;
        padding: 1.5rem;
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

      .btn-spinner {
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

      .thanaweya-loading {
        display: grid;
        gap: 0.85rem;
        max-width: 720px;
        margin: 0 auto;
      }

      .skeleton-card--short {
        height: 120px;
      }

      .thanaweya-result-panel {
        max-width: 720px;
        margin: 0 auto;
        display: grid;
        gap: 1rem;
        animation: fadeUp 0.45s ease both;
      }

      @keyframes fadeUp {
        from {
          opacity: 0;
          transform: translateY(12px);
        }
        to {
          opacity: 1;
          transform: translateY(0);
        }
      }

      .thanaweya-summary {
        display: grid;
        gap: 1.25rem;
        padding: 1.75rem;
        background: linear-gradient(
          135deg,
          var(--color-primary) 0%,
          #1e40af 100%
        );
        border-radius: 20px;
        color: #fff;
        box-shadow: 0 12px 32px rgba(30, 58, 138, 0.25);
      }

      @media (min-width: 640px) {
        .thanaweya-summary {
          grid-template-columns: auto 1fr;
          align-items: center;
        }
      }

      .thanaweya-score-block {
        text-align: center;
        min-width: 110px;
        padding: 0.5rem 1rem;
        background: rgba(255, 255, 255, 0.08);
        border: 1px solid rgba(255, 255, 255, 0.15);
        border-radius: 16px;
      }

      .thanaweya-score-value {
        font-family: var(--font-display);
        font-size: clamp(2.5rem, 6vw, 3.25rem);
        font-weight: 800;
        line-height: 1;
        color: var(--color-accent-light);
      }

      .thanaweya-score-label {
        font-size: 0.85rem;
        opacity: 0.85;
        margin-top: 0.4rem;
      }

      .thanaweya-summary-text {
        min-width: 0;
        text-align: center;
      }

      @media (min-width: 640px) {
        .thanaweya-summary-text {
          text-align: right;
        }
      }

      .thanaweya-status {
        display: inline-block;
        padding: 0.3rem 0.75rem;
        border-radius: 999px;
        font-size: 0.82rem;
        font-weight: 700;
        margin-bottom: 0.65rem;
      }

      .status-pass {
        background: rgba(220, 252, 231, 0.95);
        color: #166534;
      }

      .status-second {
        background: rgba(254, 243, 199, 0.95);
        color: #92400e;
      }

      .status-fail {
        background: rgba(254, 226, 226, 0.95);
        color: #991b1b;
      }

      .status-absent {
        background: rgba(243, 244, 246, 0.95);
        color: #374151;
      }

      .status-default {
        background: rgba(239, 246, 255, 0.95);
        color: #1e40af;
      }

      .thanaweya-student-name {
        font-family: var(--font-display);
        font-size: clamp(1.15rem, 3vw, 1.55rem);
        font-weight: 700;
        margin: 0 0 0.5rem;
        line-height: 1.45;
      }

      .thanaweya-meta {
        display: flex;
        flex-wrap: wrap;
        gap: 0.6rem;
        margin: 0.75rem 0 0;
        justify-content: center;
      }

      @media (min-width: 640px) {
        .thanaweya-meta {
          justify-content: flex-start;
        }
      }

      .thanaweya-meta-item {
        display: flex;
        flex-direction: column;
        gap: 0.15rem;
        padding: 0.55rem 0.9rem;
        background: rgba(255, 255, 255, 0.1);
        border: 1px solid rgba(255, 255, 255, 0.18);
        border-radius: 12px;
        min-width: 7.5rem;
      }

      .thanaweya-meta-item dt {
        font-size: 0.75rem;
        font-weight: 600;
        opacity: 0.75;
        margin: 0;
      }

      .thanaweya-meta-item dd {
        margin: 0;
        font-family: var(--font-display);
        font-size: 1rem;
        font-weight: 700;
        letter-spacing: 0.02em;
      }

      .thanaweya-next-step {
        display: grid;
        gap: 1rem;
        padding: 1.35rem 1.5rem;
        background: #fff;
        border-radius: 16px;
        border: 1px solid rgba(30, 58, 138, 0.08);
        box-shadow: 0 4px 16px rgba(15, 23, 42, 0.06);
      }

      @media (min-width: 640px) {
        .thanaweya-next-step {
          grid-template-columns: 1fr auto;
          align-items: center;
        }
      }

      .thanaweya-next-step-text h3 {
        font-family: var(--font-display);
        font-size: 1.05rem;
        color: var(--color-primary);
        margin: 0 0 0.35rem;
      }

      .thanaweya-next-step-text p {
        margin: 0;
        color: var(--color-text-muted);
        font-size: 0.95rem;
        line-height: 1.65;
      }

      .thanaweya-cta {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        gap: 0.45rem;
        white-space: nowrap;
      }

      .thanaweya-cta svg {
        transform: scaleX(-1);
      }

      .thanaweya-empty {
        max-width: 520px;
        margin: 0 auto;
      }

      @media (prefers-reduced-motion: reduce) {
        .thanaweya-result-panel {
          animation: none;
        }

        .btn-spinner {
          animation: none;
          border-top-color: rgba(255, 255, 255, 0.35);
        }
      }
    `,
  ],
})
export class ThanaweyaResultComponent {
  private fb = inject(FormBuilder);
  private api = inject(ApiService);

  loading = false;
  searched = false;
  error = '';
  result: StudentResult | null = null;

  form = this.fb.group({
    seatingNo: ['', [Validators.required, digitsOnlyValidator()]],
  });

  onSeatingInput(event: Event): void {
    applyDigitsOnlyInput(event, this.form.get('seatingNo'));
  }

  resetSearch(): void {
    this.searched = false;
    this.result = null;
    this.form.get('seatingNo')?.markAsUntouched();
  }

  submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    const seatingNo = this.form.value.seatingNo!.trim();
    this.loading = true;
    this.error = '';
    this.result = null;
    this.searched = false;

    this.api.lookupThanaweyaResult(seatingNo).subscribe({
      next: (data) => {
        this.result = data;
        this.searched = true;
        this.loading = false;
      },
      error: (err) => {
        this.searched = true;
        this.loading = false;
        if (err.status === 404) {
          this.result = null;
          this.error = '';
        } else {
          this.error = err.error?.message ?? 'تعذر البحث عن النتيجة.';
        }
      },
    });
  }

  statusClass(desc: string): string {
    if (desc.includes('ناجح')) return 'thanaweya-status status-pass';
    if (desc.includes('دور ثان')) return 'thanaweya-status status-second';
    if (desc.includes('راسب')) return 'thanaweya-status status-fail';
    if (desc.includes('غياب')) return 'thanaweya-status status-absent';
    return 'thanaweya-status status-default';
  }
}
