import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../services/api.service';
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

  readonly fmt = formatNumber;

  loading = false;
  error = '';
  result: StudentResult | null = null;

  form = this.fb.group({
    seatingNo: ['', [Validators.required, digitsOnlyValidator()]],
  });

  get thanaweyaMaxScore(): number {
    if (!this.result?.maximumScore) return DEFAULT_MAXIMUM_SCORE;
    return this.result.maximumScore;
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
