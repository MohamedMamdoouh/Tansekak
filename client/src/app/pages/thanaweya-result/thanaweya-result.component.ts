import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import {
  applyDigitsOnlyInput,
  digitsOnlyValidator,
} from '../../form-validators';

const SERVICE_UNAVAILABLE_MESSAGE =
  'السيرفر غير متاح حالياً. حاول مرة أخرى لاحقاً.';

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

          <button class="btn btn-primary btn-lg lookup-submit" type="submit">
            اعرض النتيجة
          </button>
        </form>
      </section>
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
    `,
  ],
})
export class ThanaweyaResultComponent {
  private fb = inject(FormBuilder);

  error = '';

  form = this.fb.group({
    seatingNo: ['', [Validators.required, digitsOnlyValidator()]],
  });

  onSeatingInput(event: Event): void {
    applyDigitsOnlyInput(event, this.form.get('seatingNo'));
  }

  submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    this.error = SERVICE_UNAVAILABLE_MESSAGE;
  }
}
