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
        <section class="result-card card" aria-label="نتيجة الطالب">
          <div class="result-header">
            <h2 class="result-name">{{ result.arabicName }}</h2>
            <span class="result-year">{{ result.year }}</span>
          </div>

          <dl class="result-details">
            <div class="result-row">
              <dt>رقم الجلوس</dt>
              <dd>{{ result.seatingNo }}</dd>
            </div>
            <div class="result-row result-row-highlight">
              <dt>المجموع الكلي</dt>
              <dd class="result-score">{{ result.totalDegree }}</dd>
            </div>
            <div class="result-row">
              <dt>حالة الطالب</dt>
              <dd>{{ result.studentCaseDesc }}</dd>
            </div>
          </dl>

          <a
            class="btn btn-secondary btn-lg result-cta"
            [routerLink]="['/predict']"
            [queryParams]="{ score: result.totalDegree }"
          >
            اعرف الكليات المتاحة لمجموعك
          </a>
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

      .lookup-notice {
        text-align: center;
        margin-bottom: 1rem;
        line-height: 1.65;
        font-size: 0.95rem;
      }

      .result-card {
        max-width: 480px;
        margin: 0 auto 2rem;
        padding: 1.75rem;
        text-align: center;
      }

      .result-header {
        margin-bottom: 1.25rem;
      }

      .result-name {
        font-family: var(--font-display);
        font-size: 1.35rem;
        font-weight: 700;
        color: var(--color-primary);
        margin: 0 0 0.35rem;
        line-height: 1.4;
      }

      .result-year {
        display: inline-block;
        font-size: 0.85rem;
        color: var(--color-text-muted);
        background: var(--color-surface-alt, #f3f4f6);
        padding: 0.2rem 0.65rem;
        border-radius: 999px;
      }

      .result-details {
        margin: 0 0 1.5rem;
        padding: 0;
      }

      .result-row {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 0.65rem 0;
        border-bottom: 1px solid var(--color-border, #e5e7eb);
        gap: 1rem;
      }

      .result-row:last-child {
        border-bottom: none;
      }

      .result-row dt {
        font-size: 0.9rem;
        color: var(--color-text-muted);
        margin: 0;
      }

      .result-row dd {
        margin: 0;
        font-weight: 600;
        text-align: left;
      }

      .result-row-highlight dd.result-score {
        font-size: 1.5rem;
        font-weight: 800;
        color: var(--color-primary);
      }

      .result-cta {
        width: 100%;
        display: inline-flex;
        align-items: center;
        justify-content: center;
      }
    `,
  ],
})
export class ThanaweyaResultComponent {
  private fb = inject(FormBuilder);
  private api = inject(ApiService);

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
}
