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
const GENERIC_ERROR_MESSAGE = 'حدث خطأ أثناء البحث. حاول مرة أخرى لاحقًا.';
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
            <h2 class="rank-student-name">{{ result.arabicName }}</h2>
            <p class="rank-seating">
              <span class="rank-seating-label">رقم الجلوس</span>
              <span class="rank-seating-no">{{ result.seatingNo }}</span>
            </p>
          </header>

          <div class="rank-body">
            @if (hasRank(result)) {
              <div class="rank-badge-wrap">
                <div class="rank-badge" [attr.aria-label]="rankAriaLabel(result)">
                  <span class="rank-badge-label">الترتيب</span>
                  <span class="rank-badge-value">{{ result.trackRank }}</span>
                </div>
                <p class="rank-subtitle">
                  من {{ result.trackTotalStudents }} طالب في
                  {{ trackLabel(result.track!) }}
                </p>
              </div>
            } @else {
              <div class="rank-unavailable">
                <p class="rank-unavailable-title">تعذّر تحديد الترتيب</p>
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
                  >{{ result.totalDegree }}/{{ thanaweyaMaxScore }}</span
                >
              </article>
              <article class="rank-meta-item">
                <span class="rank-meta-label">سنة النتيجة</span>
                <span class="rank-meta-value">{{ result.year }}</span>
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
        max-width: 520px;
        margin-inline: auto;
        padding-bottom: 3rem;
      }

      .track-rank-page .breadcrumb {
        margin-bottom: 1.25rem;
      }

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

      .rank-card {
        border-radius: 20px;
        overflow: hidden;
        background: #fff;
        border: 1px solid rgba(30, 58, 138, 0.08);
        box-shadow:
          0 1px 2px rgba(15, 23, 42, 0.04),
          0 20px 50px rgba(15, 23, 42, 0.1);
        animation: rank-enter 0.5s cubic-bezier(0.22, 1, 0.36, 1);
      }

      .rank-header {
        padding: 1.5rem 1.25rem 1.25rem;
        background: linear-gradient(160deg, #0f172a 0%, #1e3a8a 100%);
        color: #fff;
        text-align: center;
      }

      .rank-student-name {
        font-family: var(--font-display);
        font-size: clamp(1.15rem, 4vw, 1.45rem);
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
        padding: 0.5rem 1rem;
        border-radius: 10px;
        background: rgba(255, 255, 255, 0.07);
        border: 1px solid rgba(255, 255, 255, 0.1);
      }

      .rank-seating-label {
        font-size: 0.68rem;
        opacity: 0.7;
      }

      .rank-seating-no {
        font-family: var(--font-display);
        font-size: 1.05rem;
        font-weight: 800;
        letter-spacing: 0.1em;
        color: var(--color-accent-light);
      }

      .rank-body {
        padding: 1.75rem 1.25rem 1.5rem;
        text-align: center;
      }

      .rank-badge-wrap {
        margin-bottom: 1.5rem;
      }

      .rank-badge {
        display: inline-flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        width: clamp(140px, 36vw, 168px);
        height: clamp(140px, 36vw, 168px);
        border-radius: 50%;
        background: linear-gradient(145deg, #fffbeb 0%, #fef3c7 100%);
        border: 3px solid #fbbf24;
        box-shadow: 0 8px 28px rgba(217, 119, 6, 0.18);
        margin-bottom: 0.85rem;
      }

      .rank-badge-label {
        font-size: 0.78rem;
        font-weight: 700;
        color: #92400e;
        margin-bottom: 0.15rem;
      }

      .rank-badge-value {
        font-family: var(--font-display);
        font-size: clamp(2.4rem, 8vw, 3rem);
        font-weight: 800;
        line-height: 1;
        color: var(--color-primary);
      }

      .rank-subtitle {
        margin: 0;
        font-size: 0.95rem;
        color: var(--color-text-muted);
        line-height: 1.6;
      }

      .rank-unavailable {
        margin-bottom: 1.5rem;
        padding: 1rem;
        border-radius: 12px;
        background: #fffbeb;
        border: 1px solid #fde68a;
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
        padding: 0.8rem 0.7rem;
        background: var(--color-surface);
        border: 1px solid rgba(30, 58, 138, 0.06);
        border-radius: 12px;
      }

      .rank-meta-label {
        display: block;
        font-size: 0.68rem;
        color: var(--color-text-muted);
        margin-bottom: 0.15rem;
      }

      .rank-meta-value {
        font-weight: 700;
        font-size: 0.9rem;
        color: var(--color-primary);
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

        .lookup-stage {
          padding: 1.35rem 1rem;
          border-radius: 16px;
        }

        .rank-meta {
          grid-template-columns: 1fr;
        }
      }

      @media (prefers-reduced-motion: reduce) {
        .rank-card {
          animation: none;
        }

        .lookup-spinner {
          animation: none;
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
    return (
      result.trackRank != null &&
      result.trackTotalStudents != null &&
      !!result.track
    );
  }

  trackLabel(track: string): string {
    return this.trackLabels[track] ?? track;
  }

  rankAriaLabel(result: StudentResult): string {
    return `الترتيب ${result.trackRank} من ${result.trackTotalStudents} في ${this.trackLabel(result.track!)}`;
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
