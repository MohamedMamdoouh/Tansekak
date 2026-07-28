import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../api.service';
import {
  AdmissionCutoff,
  AdmissionYear,
  TRACK_LABELS,
  TRACK_OPTIONS,
  UniversityFaculty,
} from '../../../models';

interface DeleteTarget {
  id: number;
  universityName: string;
  facultyName: string;
  track: string;
  cutoffScore: number;
}

@Component({
  selector: 'app-admin-cutoffs',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  template: `
    <div class="container">
      <h1>حدود القبول</h1>
      @if (currentYear) {
        <p class="year-label">
          سنة القبول الحالية: <strong>{{ currentYear.year }}</strong>
        </p>
      }

      <div class="card form-card">
        <h2 class="form-title">{{ editId ? 'تعديل سجل' : 'إضافة سجل' }}</h2>
        <form [formGroup]="form" (ngSubmit)="save()">
          <div class="form-grid">
            <div class="form-group">
              <label>الجامعة / المعهد</label>
              <input
                type="text"
                formControlName="universityName"
                placeholder="مثال: جامعة القاهرة"
              />
              @if (
                form.get('universityName')?.invalid &&
                form.get('universityName')?.touched
              ) {
                <small class="field-error">يرجى إدخال اسم الجامعة/المعهد</small>
              }
            </div>

            <div class="form-group">
              <label>الكلية</label>
              <input
                type="text"
                formControlName="facultyName"
                placeholder="مثال: التجارة"
              />
              @if (
                form.get('facultyName')?.invalid &&
                form.get('facultyName')?.touched
              ) {
                <small class="field-error">يرجى إدخال اسم الكلية</small>
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

            <div class="form-group">
              <label>الحد الأدنى للقبول</label>
              <input
                type="number"
                inputmode="decimal"
                step="0.01"
                min="0"
                formControlName="cutoffScore"
                placeholder="مثال: 286"
                [attr.max]="currentYear?.maximumScore ?? 320"
              />
              @if (currentYear) {
                <small>الحد الأقصى: {{ currentYear.maximumScore }}</small>
              }
              @if (
                form.get('cutoffScore')?.invalid &&
                form.get('cutoffScore')?.touched
              ) {
                @if (form.get('cutoffScore')?.errors?.['required']) {
                  <small class="field-error"
                    >يرجى إدخال الحد الأدنى للقبول</small
                  >
                } @else if (form.get('cutoffScore')?.errors?.['min']) {
                  <small class="field-error"
                    >الحد الأدنى يجب أن يكون أكبر من صفر</small
                  >
                } @else if (form.get('cutoffScore')?.errors?.['max']) {
                  <small class="field-error"
                    >الحد الأدنى لا يجب أن يتجاوز ({{
                      currentYear?.maximumScore
                    }})</small
                  >
                }
              }
            </div>
          </div>

          @if (formError) {
            <p class="form-error">{{ formError }}</p>
          }

          <div class="actions">
            @if (editId) {
              <button
                type="button"
                class="btn btn-secondary"
                (click)="cancelEdit()"
              >
                إلغاء
              </button>
            }
            <button class="btn btn-primary" type="submit" [disabled]="saving">
              {{ saving ? 'جاري الحفظ...' : editId ? 'تحديث' : 'إضافة' }}
            </button>
          </div>
        </form>
      </div>

      <div class="card table-card">
        <input
          class="search"
          [(ngModel)]="search"
          (ngModelChange)="onSearchChange()"
          placeholder="ابحث باسم الجامعة أو الكلية"
        />
        <table>
          <thead>
            <tr>
              <th>الجامعة / المعهد</th>
              <th>الكلية</th>
              <th>الشعبة</th>
              <th>الحد الأدنى للقبول</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (item of items; track item.id) {
              <tr>
                <td>{{ item.universityName }}</td>
                <td>{{ item.facultyName }}</td>
                <td>{{ trackLabel(item.track) }}</td>
                <td>{{ item.cutoffScore }}</td>
                <td class="row-actions">
                  <button
                    type="button"
                    class="btn btn-secondary btn-sm"
                    (click)="edit(item)"
                  >
                    تعديل
                  </button>
                  <button
                    type="button"
                    class="btn btn-danger btn-sm"
                    (click)="openDeleteDialog(item)"
                  >
                    حذف
                  </button>
                </td>
              </tr>
            }
          </tbody>
        </table>

        @if (loadError) {
          <p class="form-error">{{ loadError }}</p>
        }
        @if (items.length === 0 && !loading && !loadError) {
          <p class="empty">لا توجد سجلات.</p>
        }
        @if (loading) {
          <p class="empty">جاري التحميل...</p>
        }

        <div class="pagination">
          <button
            type="button"
            class="btn btn-secondary btn-sm"
            [disabled]="page <= 1"
            (click)="goToPage(page - 1)"
          >
            السابق
          </button>
          <span class="page-info">صفحة {{ page }} من {{ totalPages }}</span>
          <button
            type="button"
            class="btn btn-secondary btn-sm"
            [disabled]="page >= totalPages"
            (click)="goToPage(page + 1)"
          >
            التالي
          </button>
        </div>
      </div>
    </div>

    @if (deleteTarget) {
      <div class="dialog-backdrop" (click)="closeDeleteDialog()">
        <div
          class="dialog"
          (click)="$event.stopPropagation()"
          role="dialog"
          aria-modal="true"
        >
          <div class="dialog-icon">🗑️</div>
          <h3>تأكيد الحذف</h3>
          <p class="dialog-lead">
            هل أنت متأكد من حذف هذا السجل؟ لا يمكن التراجع عن هذا الإجراء.
          </p>
          <div class="dialog-card">
            <div>
              <strong>{{ deleteTarget.facultyName }}</strong>
            </div>
            <div class="dialog-muted">{{ deleteTarget.universityName }}</div>
            <div class="dialog-tags">
              <span>{{ trackLabel(deleteTarget.track) }}</span>
              <span>الحد الأدنى للقبول: {{ deleteTarget.cutoffScore }}</span>
            </div>
          </div>
          <div class="dialog-actions">
            <button
              type="button"
              class="btn btn-secondary"
              (click)="closeDeleteDialog()"
            >
              إلغاء
            </button>
            <button
              type="button"
              class="btn btn-danger"
              [disabled]="deleting"
              (click)="confirmDelete()"
            >
              {{ deleting ? 'جاري الحذف...' : 'نعم، احذف' }}
            </button>
          </div>
        </div>
      </div>
    }
  `,
  styles: [
    `
      h1 {
        margin-top: 0;
      }
      .form-title {
        margin: 0 0 1rem;
        font-size: 1.1rem;
      }
      .year-label {
        color: #6b7280;
      }
      .form-card {
        margin-bottom: 1.5rem;
      }
      .form-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
        gap: 1rem;
      }
      .field-error,
      .form-error {
        color: #dc2626;
        display: block;
        margin-top: 0.25rem;
      }
      .form-error {
        margin-top: 0.75rem;
      }
      .actions {
        display: flex;
        gap: 0.5rem;
        margin-top: 1rem;
      }
      .search {
        width: 100%;
        margin-bottom: 1rem;
      }
      table {
        width: 100%;
        border-collapse: collapse;
      }
      th,
      td {
        padding: 0.75rem;
        text-align: right;
        border-bottom: 1px solid #eef2f7;
      }
      th {
        background: #f9fafb;
        font-weight: 600;
      }
      .row-actions {
        display: flex;
        gap: 0.5rem;
        white-space: nowrap;
      }
      .btn-sm {
        padding: 0.35rem 0.75rem;
        font-size: 0.875rem;
      }
      .empty {
        color: #6b7280;
        text-align: center;
        padding: 1rem;
      }
      .pagination {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 1rem;
        margin-top: 1.25rem;
        flex-wrap: wrap;
      }
      .page-info {
        color: #6b7280;
        font-size: 0.95rem;
      }

      .dialog-backdrop {
        position: fixed;
        inset: 0;
        background: rgba(15, 23, 42, 0.55);
        backdrop-filter: blur(4px);
        display: grid;
        place-items: center;
        z-index: 1000;
        padding: 1rem;
        animation: fadeIn 0.2s ease;
      }
      .dialog {
        width: min(420px, 100%);
        background: #fff;
        border-radius: 20px;
        padding: 1.75rem;
        box-shadow: 0 24px 60px rgba(15, 23, 42, 0.25);
        text-align: center;
        animation: slideUp 0.25s ease;
      }
      .dialog-icon {
        width: 64px;
        height: 64px;
        margin: 0 auto 1rem;
        border-radius: 50%;
        background: #fee2e2;
        display: grid;
        place-items: center;
        font-size: 1.75rem;
      }
      .dialog h3 {
        margin: 0 0 0.5rem;
        font-size: 1.35rem;
      }
      .dialog-lead {
        color: #6b7280;
        margin: 0 0 1.25rem;
        line-height: 1.6;
      }
      .dialog-card {
        background: #f8fafc;
        border: 1px solid #e2e8f0;
        border-radius: 14px;
        padding: 1rem;
        margin-bottom: 1.25rem;
        text-align: right;
      }
      .dialog-muted {
        color: #64748b;
        margin-top: 0.25rem;
      }
      .dialog-tags {
        display: flex;
        gap: 0.5rem;
        flex-wrap: wrap;
        margin-top: 0.75rem;
      }
      .dialog-tags span {
        background: #e0e7ff;
        color: #3730a3;
        padding: 0.25rem 0.65rem;
        border-radius: 999px;
        font-size: 0.85rem;
      }
      .dialog-actions {
        display: flex;
        gap: 0.75rem;
        justify-content: center;
      }
      .dialog-actions .btn {
        min-width: 120px;
      }

      @keyframes fadeIn {
        from {
          opacity: 0;
        }
        to {
          opacity: 1;
        }
      }
      @keyframes slideUp {
        from {
          opacity: 0;
          transform: translateY(16px) scale(0.98);
        }
        to {
          opacity: 1;
          transform: translateY(0) scale(1);
        }
      }
    `,
  ],
})
export class AdminCutoffsComponent implements OnInit {
  private api = inject(ApiService);
  private fb = inject(FormBuilder);

  items: AdmissionCutoff[] = [];
  universityFaculties: UniversityFaculty[] = [];
  currentYear?: AdmissionYear;
  currentYearId = 0;
  search = '';
  page = 1;
  pageSize = 10;
  totalCount = 0;
  loading = false;
  loadError = '';
  saving = false;
  deleting = false;
  formError = '';
  editId: number | null = null;
  deleteTarget: DeleteTarget | null = null;
  trackOptions = TRACK_OPTIONS;
  private searchTimer?: ReturnType<typeof setTimeout>;

  form = this.fb.group({
    universityName: ['', Validators.required],
    facultyName: ['', Validators.required],
    track: ['Science', Validators.required],
    cutoffScore: [
      null as number | null,
      [Validators.required, Validators.min(0.01)],
    ],
  });

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount / this.pageSize));
  }

  ngOnInit(): void {
    this.api
      .getUniversityFaculties()
      .subscribe((uf) => (this.universityFaculties = uf));
    this.api.getAdmissionYears().subscribe((years) => {
      this.currentYear = years.find((y) => y.isCurrent) ?? years[0];
      this.currentYearId = this.currentYear?.id ?? 0;
      if (this.currentYear) {
        this.updateCutoffScoreValidators(this.currentYear.maximumScore);
      }
      this.load();
    });
  }

  load(): void {
    if (!this.currentYearId) return;
    this.loading = true;
    this.loadError = '';
    this.api
      .getCutoffs(this.currentYearId, this.search, this.page, this.pageSize)
      .subscribe({
        next: (res) => {
          this.items = res.items ?? [];
          this.totalCount = res.totalCount ?? 0;
          this.page = res.page ?? this.page;
          this.loading = false;
        },
        error: (err) => {
          this.loading = false;
          this.items = [];
          this.totalCount = 0;
          this.loadError =
            err.status === 401
              ? 'انتهت الجلسة. سجّل الدخول مرة أخرى.'
              : (err.error?.message ?? 'تعذر تحميل السجلات.');
        },
      });
  }

  onSearchChange(): void {
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => {
      this.page = 1;
      this.load();
    }, 300);
  }

  goToPage(nextPage: number): void {
    if (nextPage < 1 || nextPage > this.totalPages) return;
    this.page = nextPage;
    this.load();
  }

  trackLabel(track: string): string {
    return TRACK_LABELS[track] ?? track;
  }

  edit(item: AdmissionCutoff): void {
    this.editId = item.id;
    this.formError = '';
    this.form.patchValue({
      universityName: item.universityName ?? '',
      facultyName: item.facultyName ?? '',
      track: item.track,
      cutoffScore: item.cutoffScore,
    });
  }

  cancelEdit(): void {
    this.editId = null;
    this.formError = '';
    this.form.reset({
      universityName: '',
      facultyName: '',
      track: 'Science',
      cutoffScore: null,
    });
  }

  save(): void {
    this.form.markAllAsTouched();
    this.formError = '';
    if (this.form.invalid || !this.currentYearId) return;

    const { universityName, facultyName, track, cutoffScore } = this.form.value;
    const uf = this.findUniversityFaculty(
      universityName!.trim(),
      facultyName!.trim(),
    );
    if (!uf) {
      this.formError =
        'الجامعة أو الكلية غير موجودة. تأكد من كتابة الاسم كما هو في النظام.';
      return;
    }

    const payload = {
      admissionYearId: this.currentYearId,
      universityFacultyId: uf.id,
      track: track!,
      cutoffScore: Number(cutoffScore),
    };

    this.saving = true;
    const req = this.editId
      ? this.api.updateCutoff(this.editId, payload)
      : this.api.createCutoff(payload);

    req.subscribe({
      next: () => {
        this.saving = false;
        this.cancelEdit();
        this.load();
      },
      error: (err) => {
        this.saving = false;
        this.formError = err.error?.message ?? 'تعذر حفظ السجل.';
      },
    });
  }

  openDeleteDialog(item: AdmissionCutoff): void {
    this.deleteTarget = {
      id: item.id,
      universityName: item.universityName ?? '',
      facultyName: item.facultyName ?? '',
      track: item.track,
      cutoffScore: item.cutoffScore,
    };
  }

  closeDeleteDialog(): void {
    if (this.deleting) return;
    this.deleteTarget = null;
  }

  confirmDelete(): void {
    if (!this.deleteTarget) return;
    this.deleting = true;
    this.api.deleteCutoff(this.deleteTarget.id).subscribe({
      next: () => {
        this.deleting = false;
        this.deleteTarget = null;
        if (this.items.length === 1 && this.page > 1) {
          this.page -= 1;
        }
        this.load();
      },
      error: () => {
        this.deleting = false;
      },
    });
  }

  private updateCutoffScoreValidators(maxScore: number): void {
    this.form
      .get('cutoffScore')
      ?.setValidators([
        Validators.required,
        Validators.min(0.01),
        Validators.max(maxScore),
      ]);
    this.form.get('cutoffScore')?.updateValueAndValidity();
  }

  private findUniversityFaculty(
    universityName: string,
    facultyName: string,
  ): UniversityFaculty | undefined {
    const normalize = (value: string) => value.trim().toLowerCase();
    const u = normalize(universityName);
    const f = normalize(facultyName);
    return this.universityFaculties.find(
      (x) =>
        normalize(x.universityName ?? '') === u &&
        normalize(x.facultyName ?? '') === f,
    );
  }
}
