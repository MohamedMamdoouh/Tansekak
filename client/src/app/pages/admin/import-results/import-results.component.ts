import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService, ImportUploadError } from '../../../api.service';
import { ImportUploadService } from '../../../import-upload.service';
import { AdmissionYear, ImportResult } from '../../../models';

@Component({
  selector: 'app-admin-import-results',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="container">
      <h1>استيراد نتائج الثانوية</h1>
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

          <p class="note">
            هذا الاستيراد <strong>يستبدل</strong> كل نتائج الطلاب للسنة
            المختارة.
          </p>

          <p class="hint">
            صيغة الملف: Excel (.xlsx) بأعمدة —
            <code
              >seating_no | arabic_name | total_degree | student_case_desc</code
            >
          </p>

          <div class="form-group">
            <label>ملف Excel (.xlsx)</label>
            <input type="file" (change)="onFile($event)" accept=".xlsx" />
            @if (fileTouched && !file) {
              <small class="field-error">يرجى اختيار ملف Excel</small>
            }
          </div>

          <button class="btn btn-primary" type="submit" [disabled]="uploading">
            {{
              uploading
                ? 'جاري الاستيراد (قد يستغرق دقائق للملفات الكبيرة)...'
                : 'استيراد'
            }}
          </button>
        </form>

        @if (message) {
          <p [class]="result?.success ? 'success' : 'error'">{{ message }}</p>
        }
        @if (result?.errors?.length) {
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
    `,
  ],
})
export class AdminImportResultsComponent implements OnInit {
  private api = inject(ApiService);
  private fb = inject(FormBuilder);
  private importUpload = inject(ImportUploadService);

  years: AdmissionYear[] = [];
  file: File | null = null;
  fileTouched = false;
  uploading = false;
  message = '';
  result: ImportResult | null = null;

  form = this.fb.group({
    yearId: [null as number | null, Validators.required],
  });

  ngOnInit(): void {
    this.api.getAdmissionYears().subscribe((y) => {
      this.years = y;
      const current = y.find((x) => x.isCurrent) ?? y[0];
      if (current) this.form.patchValue({ yearId: current.id });
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
    const yearId = this.form.value.yearId;
    if (this.form.invalid || !this.file || !yearId) return;

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
        this.result = res;
        this.message = res.message;
        this.uploading = false;
        this.importUpload.finish();
      })
      .catch((err: ImportUploadError) => {
        if (err.aborted) {
          this.message = 'تم إلغاء الاستيراد.';
          this.uploading = false;
          return;
        }

        this.result = err.error?.data ?? null;
        this.message =
          err.status === 0
            ? 'انقطع الاتصال بالخادم أثناء الاستيراد. الملف قد يكون كبيراً — انتظر دقيقة ثم حاول مرة أخرى بعد إعادة تشغيل الـ API.'
            : (err.error?.message ?? 'فشل الاستيراد.');
        this.uploading = false;
        this.importUpload.finish();
      });
  }
}
