import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../services/api.service';
import { AdmissionYearStore } from '../../services/admission-year.store';
import { DEFAULT_MAXIMUM_SCORE, StudentResult } from '../../models';
import {
  applyDigitsOnlyInput,
  digitsOnlyValidator,
} from '../../form-validators';
import { formatNumber } from '../../utils/format-number.util';
import {
  hasTrackRank,
  predictQueryParams,
} from '../../utils/student-result.util';
import { scorePercentage, scoreProgress } from '../../utils/thanaweya-score.util';
import { getTrackLabel } from '../../utils/track-label.util';
import { submitStudentLookup } from '../../utils/student-lookup.util';

@Component({
  selector: 'app-thanaweya-result',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './thanaweya-result.component.html',
  styleUrl: './thanaweya-result.component.scss',
})
export class ThanaweyaResultComponent {
  private fb = inject(FormBuilder);
  private api = inject(ApiService);
  private admissionYears = inject(AdmissionYearStore);
  private destroyRef = inject(DestroyRef);

  readonly fmt = formatNumber;

  loading = false;
  error = '';
  result: StudentResult | null = null;

  form = this.fb.group({
    seatingNo: ['', [Validators.required, digitsOnlyValidator()]],
  });

  constructor() {
    this.admissionYears
      .loadYears()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe();
  }

  get thanaweyaMaxScore(): number {
    if (!this.result) return DEFAULT_MAXIMUM_SCORE;
    return this.admissionYears.getMaximumScoreForYear(
      this.result.year,
      DEFAULT_MAXIMUM_SCORE,
    );
  }

  resultPercentage(totalDegree: number): string {
    return scorePercentage(totalDegree, this.thanaweyaMaxScore);
  }

  scoreProgress(totalDegree: number): number {
    return scoreProgress(totalDegree, this.thanaweyaMaxScore);
  }

  trackLabel(track: string): string {
    return getTrackLabel(track);
  }

  studentInitial(name: string): string {
    const trimmed = name.trim();
    return trimmed ? trimmed.charAt(0) : '؟';
  }

  onSeatingInput(event: Event): void {
    applyDigitsOnlyInput(event, this.form.get('seatingNo'));
    this.error = '';
    this.result = null;
  }

  submit(): void {
    submitStudentLookup(
      this.form,
      (seatingNo) => this.api.getThanaweyaResult(seatingNo),
      (state) => {
        if (state.loading !== undefined) this.loading = state.loading;
        if (state.error !== undefined) this.error = state.error;
        if (state.result !== undefined) this.result = state.result;
      },
    );
  }

  predictQueryParams(): { score: number; track?: string } {
    return predictQueryParams(this.result);
  }

  hasTrackRank(result: StudentResult): boolean {
    return hasTrackRank(result);
  }
}
