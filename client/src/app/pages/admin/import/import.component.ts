import { Component, DestroyRef, ElementRef, NgZone, ViewChild, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { ApiService, ImportUploadError } from '../../../services/api.service';
import {
  extractImportResult,
  importErrorMessage,
} from '../../../utils/import-error.util';
import { resolveApiError } from '../../../utils/api-error.util';
import { ImportUploadService } from '../../../services/import-upload.service';
import { AdmissionYearStore } from '../../../services/admission-year.store';
import { AdmissionYear, ImportResult, TRACK_OPTIONS } from '../../../models';
import { getTrackLabel } from '../../../utils/track-label.util';
import { ImportSuccessDialogComponent } from '../../../components/import-success-dialog/import-success-dialog.component';

@Component({
  selector: 'app-admin-import',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ImportSuccessDialogComponent],
  templateUrl: './import.component.html',
  styleUrl: './import.component.scss',
})
export class AdminImportComponent {
  private api = inject(ApiService);
  private fb = inject(FormBuilder);
  private importUpload = inject(ImportUploadService);
  private admissionYears = inject(AdmissionYearStore);
  private ngZone = inject(NgZone);
  private destroyRef = inject(DestroyRef);

  @ViewChild('fileInput') private fileInput?: ElementRef<HTMLInputElement>;

  currentYear?: AdmissionYear;
  apiAvailable: boolean | null = null;
  yearLoadError = '';
  file: File | null = null;
  fileTouched = false;
  uploading = false;
  message = '';
  result: ImportResult | null = null;
  trackOptions = TRACK_OPTIONS;
  showSuccessDialog = false;
  successMessage = '';
  importedCount?: number;
  successTrackLabel = '';

  get successDetailSubtitle(): string {
    const year = this.currentYear ? String(this.currentYear.year) : '';
    const count =
      this.importedCount != null ? `${this.importedCount} حد قبول` : '';
    return [year, count].filter(Boolean).join(' — ');
  }

  form = this.fb.group({
    track: ['Science', Validators.required],
  });

  constructor() {
    this.api
      .checkHealth()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((ok) => {
        this.apiAvailable = ok;
      });

    this.admissionYears
      .refreshCurrentYear()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (year) => {
          this.currentYear = year;
          this.yearLoadError = '';
        },
        error: (err: HttpErrorResponse) => {
          this.currentYear = undefined;
          this.yearLoadError = resolveApiError(
            err,
            'تعذر تحميل سنة القبول الحالية. انشر سنة من /admin/years ثم أعد المحاولة.',
          );
        },
      });
  }

  trackLabel(track: string): string {
    return getTrackLabel(track);
  }

  onFile(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.file = input.files?.[0] ?? null;
    this.fileTouched = true;
  }

  upload(): void {
    this.form.markAllAsTouched();
    this.fileTouched = true;
    const { track } = this.form.value;
    const yearId = this.currentYear?.id;
    if (this.form.invalid || !this.file || !yearId || !track || this.apiAvailable === false) return;

    this.uploading = true;
    this.message = '';
    this.result = null;

    const signal = this.importUpload.begin();

    void this.api
      .importCutoffsWithProgress(
        yearId,
        track,
        this.file,
        ({ percent, phase }) => this.importUpload.updateProgress(percent, phase),
        signal,
      )
      .then((res) => {
        this.ngZone.run(() => {
          this.uploading = false;
          this.importUpload.finish();
          if (res.success) {
            this.openSuccessDialog(res, track);
            return;
          }
          this.result = res;
          this.message = res.message;
        });
      })
      .catch((err: ImportUploadError) => {
        this.ngZone.run(() => {
          this.uploading = false;
          this.importUpload.finish();

          if (err.aborted) {
            this.message = 'تم إلغاء الاستيراد.';
            return;
          }

          this.result = extractImportResult(err.error);
          this.message = importErrorMessage(err.status, err.error);
        });
      });
  }

  closeSuccessDialog(): void {
    this.showSuccessDialog = false;
    this.successMessage = '';
    this.importedCount = undefined;
    this.successTrackLabel = '';
  }

  private openSuccessDialog(result: ImportResult, track: string): void {
    this.successMessage = result.message;
    this.importedCount = result.importedCount;
    this.successTrackLabel = this.trackLabel(track);
    this.showSuccessDialog = true;
    this.resetForm();
  }

  private resetForm(): void {
    this.file = null;
    this.fileTouched = false;
    this.message = '';
    this.result = null;
    if (this.fileInput) {
      this.fileInput.nativeElement.value = '';
    }
  }
}
