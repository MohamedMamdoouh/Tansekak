import { Component, DestroyRef, NgZone, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
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
import { getTrackLabel } from '../../../utils/track-label.util';

interface CutoffImportSlot {
  track: 'Science' | 'Literature';
  label: string;
  file: File | null;
  touched: boolean;
}

interface CutoffImportFileResult {
  track: string;
  label: string;
  message: string;
  result: ImportResult | null;
}

interface ImportResultDialog {
  success: boolean;
  title: string;
  message: string;
  results: CutoffImportFileResult[];
}

@Component({
  selector: 'app-admin-import',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './import.component.html',
  styleUrl: './import.component.scss',
})
export class AdminImportComponent {
  private api = inject(ApiService);
  private importUpload = inject(ImportUploadService);
  private admissionYears = inject(AdmissionYearStore);
  private ngZone = inject(NgZone);
  private destroyRef = inject(DestroyRef);
  private router = inject(Router);
  private resultDialogTimer?: ReturnType<typeof setTimeout>;

  currentYear?: AdmissionYear;
  apiAvailable: boolean | null = null;
  yearLoadError = '';
  uploading = false;
  message = '';
  fileResults: CutoffImportFileResult[] = [];
  resultDialog: ImportResultDialog | null = null;

  slots: CutoffImportSlot[] = [
    { track: 'Science', label: 'ملف الشعبة العلمية', file: null, touched: false },
    { track: 'Literature', label: 'ملف الشعبة الأدبية', file: null, touched: false },
  ];

  constructor() {
    this.destroyRef.onDestroy(() => {
      clearTimeout(this.resultDialogTimer);
    });

    this.api
      .checkHealth()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((ok) => {
        this.apiAvailable = ok;
      });

    this.admissionYears
      .loadCurrentYear()
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

  selectedSlots(): CutoffImportSlot[] {
    return this.slots.filter((slot) => slot.file);
  }

  showMissingFileError(): boolean {
    return this.slots.every((slot) => slot.touched) && this.selectedSlots().length === 0;
  }

  onFile(event: Event, slot: CutoffImportSlot): void {
    const input = event.target as HTMLInputElement;
    slot.file = input.files?.[0] ?? null;
    slot.touched = true;
  }

  async upload(): Promise<void> {
    for (const slot of this.slots) {
      slot.touched = true;
    }

    const yearId = this.currentYear?.id;
    const selected = this.selectedSlots();
    if (!yearId || selected.length === 0 || this.uploading || this.apiAvailable === false) {
      return;
    }

    this.uploading = true;
    this.message = '';
    this.fileResults = [];

    const signal = this.importUpload.begin();

    try {
      for (const slot of selected) {
        if (signal.aborted) {
          this.ngZone.run(() => {
            this.message = 'تم إلغاء الاستيراد.';
          });
          return;
        }

        try {
          const res = await this.api.importCutoffsWithProgress(
            yearId,
            slot.track,
            slot.file!,
            ({ percent, phase }) => this.importUpload.updateProgress(percent, phase),
            signal,
          );

          const item: CutoffImportFileResult = {
            track: slot.track,
            label: this.trackLabel(slot.track),
            message: res.message,
            result: res,
          };

          this.ngZone.run(() => {
            this.fileResults = [...this.fileResults, item];
            this.message = res.message;
          });

          if (!res.success) return;
        } catch (err) {
          const uploadError = err as ImportUploadError;
          this.ngZone.run(() => {
            if (uploadError.aborted) {
              this.message = 'تم إلغاء الاستيراد.';
              return;
            }

            const result = extractImportResult(uploadError.error);
            const errorMessage = importErrorMessage(
              uploadError.status,
              uploadError.error,
              { kind: uploadError.kind },
            );
            this.fileResults = [
              ...this.fileResults,
              {
                track: slot.track,
                label: this.trackLabel(slot.track),
                message: errorMessage,
                result,
              },
            ];
            this.message = errorMessage;
          });
          return;
        }
      }
    } finally {
      this.ngZone.run(() => {
        this.uploading = false;
        this.importUpload.finish();
        if (this.fileResults.length > 0) {
          this.openResultDialog();
        }
      });
    }
  }

  private openResultDialog(): void {
    const allSuccess = this.fileResults.every((item) => item.result?.success);
    const message = this.fileResults
      .map((item) => `${item.label}: ${item.message}`)
      .join('\n');

    this.resultDialog = {
      success: allSuccess,
      title: allSuccess ? 'تم الاستيراد بنجاح' : 'فشل الاستيراد',
      message,
      results: [...this.fileResults],
    };

    clearTimeout(this.resultDialogTimer);
    this.resultDialogTimer = setTimeout(() => {
      this.closeResultDialogAndRefresh();
    }, 5000);
  }

  closeResultDialogAndRefresh(): void {
    clearTimeout(this.resultDialogTimer);
    this.resultDialog = null;
    void this.router.navigateByUrl('/admin', { skipLocationChange: true }).then(() => {
      void this.router.navigate(['/admin/import']);
    });
  }
}
