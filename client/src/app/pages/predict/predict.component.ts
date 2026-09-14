import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../services/api.service';
import { Config, DEFAULT_MAXIMUM_SCORE } from '../../models';
import { canonicalizeTrack, getTrackLabel } from '../../utils/track-label.util';

@Component({
  selector: 'app-predict',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './predict.component.html',
  styleUrl: './predict.component.scss',
})
export class PredictComponent {
  private fb = inject(FormBuilder);
  private api = inject(ApiService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private destroyRef = inject(DestroyRef);

  config?: Config;
  readonly defaultMaximumScore = DEFAULT_MAXIMUM_SCORE;
  loading = false;
  error = '';

  form = this.fb.group({
    track: ['', Validators.required],
    score: [null as number | null, [Validators.required, Validators.min(0)]],
  });

  constructor() {
    this.route.queryParams
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((params) => {
        if (params['error'] === 'missing') {
          this.error = 'يرجى إدخال الشعبة والمجموع لعرض النتائج.';
        }
        if (params['score'] !== undefined && params['score'] !== '') {
          const score = Number(params['score']);
          if (!Number.isNaN(score)) {
            this.form.patchValue({ score });
          }
        }
        if (params['track']) {
          this.form.patchValue({ track: canonicalizeTrack(params['track']) });
        }
      });

    this.api
      .getConfig()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
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
    return getTrackLabel(track);
  }

  submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid || !this.config) return;

    this.loading = true;
    this.router
      .navigate(['/results'], {
        queryParams: {
          track: this.form.value.track,
          score: Number(this.form.value.score),
        },
      })
      .then((navigated) => {
        if (!navigated) this.loading = false;
      })
      .catch(() => {
        this.loading = false;
      });
  }
}
