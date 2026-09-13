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
import { scorePercentage } from '../../utils/thanaweya-score.util';
import { getTrackLabel } from '../../utils/track-label.util';
import { submitStudentLookup } from '../../utils/student-lookup.util';

@Component({
  selector: 'app-track-rank',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './track-rank.component.html',
  styleUrl: './track-rank.component.scss',
})
export class TrackRankComponent {
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

  hasRank(result: StudentResult): boolean {
    return hasTrackRank(result);
  }

  trackLabel(track: string): string {
    return getTrackLabel(track);
  }

  rankAriaLabel(result: StudentResult): string {
    return `الترتيب ${result.trackRank} من ${result.trackTotalStudents} في ${this.trackLabel(result.track!)}`;
  }

  rankPositionPercent(result: StudentResult): number {
    if (!this.hasRank(result)) return 0;
    const rank = result.trackRank!;
    const total = result.trackTotalStudents!;
    if (total <= 1) return 0;
    return Math.min(Math.max(((rank - 1) / (total - 1)) * 100, 0), 100);
  }

  rankPercentile(result: StudentResult): string {
    if (!this.hasRank(result)) return '0';
    const rank = result.trackRank!;
    const total = result.trackTotalStudents!;
    const pct = ((total - rank + 1) / total) * 100;
    return pct.toFixed(1);
  }

  scorePercentage(totalDegree: number): string {
    return scorePercentage(totalDegree, this.thanaweyaMaxScore);
  }

  predictQueryParams(): { score: number; track?: string } {
    return predictQueryParams(this.result);
  }
}
