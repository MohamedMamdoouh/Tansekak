import { Component, NgZone, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService, ImportUploadError } from '../../../api.service';
import { ImportUploadService } from '../../../import-upload.service';
import { AdmissionYear, ImportResult, TRACK_OPTIONS } from '../../../models';

@Component({
  selector: 'app-admin-import',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="container">
      <h1>استيراد حدود القبول</h1>
      <div class="card">
        <form [formGroup]="form" (ngSubmit)="upload()">
          <div class="form-group">
            <label>سنة القبول</label>
            <select formControlName="yearId">
              <option [ngValue]="null">اختر السنة</option>
              @for (y of years; track y.id) {
                <option [ngValue]="y.id">{{ y.year }}</option>
              }
            </select>
            @if (form.get('yearId')?.invalid && form.get('yearId')?.touched) {
              <small class="field-error">يرجى اختيار سنة القبول</small>
            }
          </div>

          <div class="form-group">
            <label>الشعبة</label>
            <select formControlName="track">
              @for (t of trackOptions; track t.value) {
                <option [value]="t.value">{{ t.label }}</option>
              }
            </select>
            @if (form.get('track')?.invalid && form.get('track')?.touched) {
              <small class="field-error">يرجى اختيار الشعبة</small>
            }
          </div>

          @if (form.value.track) {
            <p class="note">
              هذا الاستيراد خاص بشعبة
              <strong>{{ trackLabel(form.value.track!) }}</strong> فقط، و<strong
                >يستبدل</strong
              >
              كل حدود القبول الحالية لهذه السنة والشعبة.
            </p>
          }

          <p class="hint">
            صيغة الملف: جدول Markdown بعمودين —
            <code>الكلية | الحد الأدنى</code>
          </p>

          <div class="form-group">
            <label>ملف Markdown (.md)</label>
            <input type="file" (change)="onFile($event)" accept=".md" />
            @if (fileTouched && !file) {
              <small class="field-error">يرجى اختيار ملف Markdown</small>
            }
          </div>

          <button class="btn btn-primary" type="submit" [disabled]="uploading">
            {{ uploading ? 'جاري الاستيراد...' : 'استيراد' }}
          </button>
        </form>

        @if (message) {
          <p [class]="result?.success ? 'success' : 'error'">{{ message }}</p>
        }
        @if (result?.errors?.length) {
          <div class="table-scroll">
            <table>
              <thead>
                <tr>
                  <th>الصف</th>
                  <th>العمود</th>
                  <th>الرسالة</th>
                </tr>
              </thead>
              <tbody>
                @for (e of result!.errors!; track $index) {
                  <tr>
                    <td>{{ e.rowNumber }}</td>
                    <td>{{ e.column }}</td>
                    <td>{{ e.message }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      </div>
    </div>
  `,
  styles: [
    `
      h1 {
        margin-top: 0;
      }
      .field-error {
        color: #dc2626;
        display: block;
        margin-top: 0.25rem;
      }
      .note {
        background: #eff6ff;
        padding: 0.75rem 1rem;
        border-radius: 0.5rem;
        margin: 1rem 0;
      }
      .hint {
        color: #6b7280;
        font-size: 0.9rem;
      }
      .success {
        color: #059669;
      }
      table {
        width: 100%;
        margin-top: 1rem;
        border-collapse: collapse;
      }
      th,
      td {
        padding: 0.5rem;
        text-align: right;
        border-bottom: 1px solid #eef2f7;
      }

      @media (max-width: 640px) {
        .btn-primary {
          width: 100%;
        }
      }
    `,
  ],
})
export class AdminImportComponent implements OnInit {
  private api = inject(ApiService);
  private fb = inject(FormBuilder);
  private importUpload = inject(ImportUploadService);
  private ngZone = inject(NgZone);

  years: AdmissionYear[] = [];
  file: File | null = null;
  fileTouched = false;
  uploading = false;
  message = '';
  result: ImportResult | null = null;
  trackOptions = TRACK_OPTIONS;

  form = this.fb.group({
    yearId: [null as number | null, Validators.required],
    track: ['Science', Validators.required],
  });

  ngOnInit(): void {
    this.api.getAdmissionYears().subscribe((y) => {
      this.years = y;
      const current = y.find((x) => x.isCurrent) ?? y[0];
      if (current) this.form.patchValue({ yearId: current.id });
    });
  }

  trackLabel(track: string): string {
    return this.trackOptions.find((t) => t.value === track)?.label ?? track;
  }

  onFile(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.file = input.files?.[0] ?? null;
    this.fileTouched = true;
  }

  upload(): void {
    this.form.markAllAsTouched();
    this.fileTouched = true;
    const { yearId, track } = this.form.value;
    if (this.form.invalid || !this.file || !yearId || !track) return;

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

          this.result = err.error?.data ?? null;
          this.message = err.error?.message ?? 'فشل الاستيراد.';
        });
      });
  }
}
