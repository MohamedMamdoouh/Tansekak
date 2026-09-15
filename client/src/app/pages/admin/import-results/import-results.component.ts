import { Component, DestroyRef, ElementRef, NgZone, ViewChild, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { ApiService, ImportUploadError } from '../../../services/api.service';
import {
  extractImportResult,
  importErrorMessage,
} from '../../../utils/import-error.util';
import { resolveApiError } from '../../../utils/api-error.util';
import { ImportUploadService } from '../../../services/import-upload.service';
import { AdmissionYearStore } from '../../../services/admission-year.store';
import { AdmissionYear, ImportResult } from '../../../models';
import { ImportSuccessDialogComponent } from '../../../components/import-success-dialog/import-success-dialog.component';

@Component({
  selector: 'app-admin-import-results',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ImportSuccessDialogComponent],
  templateUrl: './import-results.component.html',
  styleUrl: './import-results.component.scss',
})
export class AdminImportResultsComponent {
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
  showSuccessDialog = false;
  successMessage = '';
  importedCount?: number;

  get successDetailTitle(): string {
    return this.currentYear ? String(this.currentYear.year) : '';
  }

  get successDetailSubtitle(): string {
    return this.importedCount != null ? `${this.importedCount} نتيجة طالب` : '';
  }

  form = this.fb.group({});

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

  onFile(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.file = input.files?.[0] ?? null;
    this.fileTouched = true;
  }

  upload(): void {
    this.form.markAllAsTouched();
    this.fileTouched = true;
    const yearId = this.currentYear?.id;
    if (!this.file || !yearId || this.apiAvailable === false) return;

    this.uploading = true;
    this.message = '';
    this.result = null;

    const signal = this.importUpload.begin();

    void this.api
      .importStudentResultsWithProgress(
        yearId,
        this.file,
        ({ percent, phase }) => this.importUpload.updateProgress(percent, phase),
        signal,
      )
      .then((res) => {
        this.ngZone.run(() => {
          this.uploading = false;
          this.importUpload.finish();
          if (res.success) {
            this.openSuccessDialog(res);
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
          this.message = importErrorMessage(err.status, err.error, {
            kind: err.kind,
          });
        });
      });
  }

  closeSuccessDialog(): void {
    this.showSuccessDialog = false;
    this.successMessage = '';
    this.importedCount = undefined;
  }

  private openSuccessDialog(result: ImportResult): void {
    this.successMessage = result.message;
    this.importedCount = result.importedCount;
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
