import { Component, DestroyRef, NgZone, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService, ImportUploadError } from '../../../services/api.service';
import {
  extractImportResult,
  importErrorMessage,
} from '../../../utils/import-error.util';
import { ImportUploadService } from '../../../services/import-upload.service';
import { AdmissionYearStore } from '../../../services/admission-year.store';
import { AdmissionYear, ImportResult } from '../../../models';

@Component({
  selector: 'app-admin-import-results',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
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

  currentYear?: AdmissionYear;
  file: File | null = null;
  fileTouched = false;
  uploading = false;
  message = '';
  result: ImportResult | null = null;

  form = this.fb.group({});

  constructor() {
    this.admissionYears
      .loadCurrentYear()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((year) => {
        this.currentYear = year;
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
    if (!this.file || !yearId) return;

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
          this.result = res;
          this.message = res.message;
          this.uploading = false;
          this.importUpload.finish();
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
}
