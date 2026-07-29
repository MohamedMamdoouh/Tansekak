import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../api.service';
import { Config, TRACK_LABELS } from '../../models';

@Component({
  selector: 'app-predict',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="container">
      <nav class="breadcrumb" aria-label="مسار التنقل">
        <a routerLink="/">الرئيسية</a>
        <span class="breadcrumb-sep">/</span>
        <span>اعرف كليتك</span>
      </nav>

      <section class="hero card predict-card">
        <h1 class="section-title">اعرف كليتك</h1>
        <p class="section-subtitle" style="margin-bottom: 1.5rem">
          اكتب مجموعك وشعبتك ونشوفلك الكليات المتاحة ليك
        </p>

        <form [formGroup]="form" (ngSubmit)="submit()">
          <div class="form-group">
            <label for="track">الشعبة</label>
            <select id="track" formControlName="track">
              <option value="">اختر الشعبة</option>
              @for (track of config?.tracks ?? []; track track) {
                <option [value]="track">{{ trackLabel(track) }}</option>
              }
            </select>
            @if (form.get('track')?.invalid && form.get('track')?.touched) {
              <small class="field-error">يرجى اختيار الشعبة</small>
            }
          </div>

          <div class="form-group">
            <label for="score">المجموع الكلي</label>
            <input
              id="score"
              type="number"
              inputmode="decimal"
              step="0.01"
              min="0"
              formControlName="score"
              [attr.max]="config?.maximumScore ?? 320"
            />
            <small class="text-muted"
              >الحد الأقصى: {{ config?.maximumScore }}</small
            >
            @if (form.get('score')?.invalid && form.get('score')?.touched) {
              @if (form.get('score')?.errors?.['required']) {
                <small class="field-error">يرجى إدخال المجموع</small>
              } @else if (form.get('score')?.errors?.['min']) {
                <small class="field-error">المجموع لا يمكن أن يكون سالبا</small>
              } @else if (form.get('score')?.errors?.['max']) {
                <small class="field-error"
                  >المجموع يتجاوز الحد الأقصى ({{
                    config?.maximumScore
                  }})</small
                >
              }
            }
          </div>

          @if (error) {
            <div class="error">{{ error }}</div>
          }

          <button
            class="btn btn-primary btn-lg"
            type="submit"
            [disabled]="loading"
          >
            {{ loading ? 'جاري التحقق...' : 'اعرض كليتي المتوقعة' }}
          </button>
        </form>
      </section>
    </div>
  `,
  styles: [
    `
      .predict-card {
        max-width: 640px;
        margin: 0 auto;
      }
      .field-error {
        color: #dc2626;
        display: block;
        margin-top: 0.25rem;
      }

      @media (max-width: 640px) {
        .predict-card .btn-primary {
          width: 100%;
        }
      }
    `,
  ],
})
export class PredictComponent implements OnInit {
  private fb = inject(FormBuilder);
  private api = inject(ApiService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  config?: Config;
  loading = false;
  error = '';

  form = this.fb.group({
    track: ['', Validators.required],
    score: [null as number | null, [Validators.required, Validators.min(0)]],
  });

  ngOnInit(): void {
    this.route.queryParams.subscribe((params) => {
      if (params['error'] === 'missing') {
        this.error = 'يرجى إدخال الشعبة والمجموع لعرض النتائج.';
      }
      if (params['score']) {
        const score = Number(params['score']);
        if (!Number.isNaN(score)) {
          this.form.patchValue({ score });
        }
      }
    });

    this.api.getConfig().subscribe({
      next: (cfg) => {
        this.config = cfg;
        this.form
          .get('score')
          ?.setValidators([
            Validators.required,
            Validators.min(0),
            Validators.max(cfg.maximumScore),
          ]);
        this.form.get('score')?.updateValueAndValidity();
      },
      error: () => (this.error = 'تعذر تحميل إعدادات التطبيق.'),
    });
  }

  trackLabel(track: string): string {
    return TRACK_LABELS[track] ?? track;
  }

  submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid || !this.config) return;

    this.loading = true;
    this.router.navigate(['/results'], {
      queryParams: {
        track: this.form.value.track,
        score: Number(this.form.value.score),
      },
    });
  }
}
