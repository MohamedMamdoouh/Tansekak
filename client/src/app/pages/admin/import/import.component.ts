import { Component, DestroyRef, NgZone, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService, ImportUploadError } from '../../../services/api.service';
import {
  extractImportResult,
  importErrorMessage,
} from '../../../utils/import-error.util';
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

  currentYear?: AdmissionYear;
  uploading = false;
  message = '';
  fileResults: CutoffImportFileResult[] = [];

  slots: CutoffImportSlot[] = [
    { track: 'Science', label: 'ملف الشعبة العلمية', file: null, touched: false },
    { track: 'Literature', label: 'ملف الشعبة الأدبية', file: null, touched: false },
  ];

  constructor() {
    this.admissionYears
      .loadCurrentYear()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((year) => {
        this.currentYear = year;
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
    if (!yearId || selected.length === 0 || this.uploading) return;

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
            this.fileResults = [
              ...this.fileResults,
              {
                track: slot.track,
                label: this.trackLabel(slot.track),
                message: importErrorMessage(uploadError.status, uploadError.error),
                result,
              },
            ];
            this.message = importErrorMessage(uploadError.status, uploadError.error);
          });
          return;
        }
      }
    } finally {
      this.ngZone.run(() => {
        this.uploading = false;
        this.importUpload.finish();
      });
    }
  }
}
