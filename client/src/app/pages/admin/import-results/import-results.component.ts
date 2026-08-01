import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../../api.service';
import { AdmissionYear } from '../../../models';

const IMPORT_UNAVAILABLE_MESSAGE = 'خدمة الاستيراد غير متاحة حاليا.';

@Component({
  selector: 'app-admin-import-results',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="container">
      <h1>استيراد نتائج الثانوية</h1>
      <div class="service-unavailable" role="status">
        {{ importUnavailableMessage }}
      </div>
      <div class="card">
        <form [formGroup]="form" (ngSubmit)="upload()">
          <div class="form-group">
            <label>سنة القبول</label>
            <select formControlName="yearId" [disabled]="true">
              <option [ngValue]="null">اختر السنة</option>
              @for (y of years; track y.id) {
                <option [ngValue]="y.id">{{ y.year }}</option>
              }
            </select>
          </div>

          <p class="note">
            هذا الاستيراد <strong>يستبدل</strong> كل نتائج الطلاب للسنة
            المختارة.
          </p>

          <p class="hint">
            صيغة الملف: Excel (.xlsx). الأعمدة المطلوبة:
            <code
              >seating_no | arabic_name | total_degree | student_case_desc</code
            >
          </p>

          <div class="form-group">
            <label>ملف Excel (.xlsx)</label>
            <input type="file" [disabled]="true" accept=".xlsx" />
          </div>

          <button class="btn btn-primary" type="submit" [disabled]="true">
            استيراد
          </button>
        </form>
      </div>
    </div>
  `,
  styles: [
    `
      h1 {
        margin-top: 0;
      }
      .service-unavailable {
        background: #fef3c7;
        border: 1px solid #f59e0b;
        color: #92400e;
        padding: 0.85rem 1rem;
        border-radius: 0.5rem;
        margin-bottom: 1rem;
        line-height: 1.65;
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
        word-break: break-word;
      }

      .hint code {
        display: block;
        margin-top: 0.35rem;
        font-size: 0.82rem;
        line-height: 1.6;
      }

      @media (max-width: 640px) {
        .btn-primary {
          width: 100%;
        }
      }
    `,
  ],
})
export class AdminImportResultsComponent implements OnInit {
  private api = inject(ApiService);
  private fb = inject(FormBuilder);

  readonly importUnavailableMessage = IMPORT_UNAVAILABLE_MESSAGE;
  years: AdmissionYear[] = [];

  form = this.fb.group({
    yearId: [
      { value: null as number | null, disabled: true },
      Validators.required,
    ],
  });

  ngOnInit(): void {
    this.api.getAdmissionYears().subscribe((y) => {
      this.years = y;
      const current = y.find((x) => x.isCurrent) ?? y[0];
      if (current) this.form.patchValue({ yearId: current.id });
    });
  }

  upload(): void {
    // Service disabled — upload controls are inactive.
  }
}
