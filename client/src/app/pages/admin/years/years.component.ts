import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { ApiService } from '../../../services/api.service';
import { resolveApiError } from '../../../utils/api-error.util';
import { AdmissionYearStore } from '../../../services/admission-year.store';
import { AdmissionYear, DEFAULT_MAXIMUM_SCORE } from '../../../models';

@Component({
  selector: 'app-admin-years',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './years.component.html',
  styleUrl: './years.component.scss',
})
export class AdminYearsComponent implements OnInit {
  private api = inject(ApiService);
  private admissionYears = inject(AdmissionYearStore);
  private fb = inject(FormBuilder);
  private destroyRef = inject(DestroyRef);

  items: AdmissionYear[] = [];
  loading = false;
  loadError = '';
  saving = false;
  deleting = false;
  formError = '';
  editId: number | null = null;
  deleteTarget: AdmissionYear | null = null;
  readonly defaultMaximumScore = DEFAULT_MAXIMUM_SCORE;

  form = this.fb.group({
    year: [
      null as number | null,
      [Validators.required, Validators.min(2000), Validators.max(2100)],
    ],
    maximumScore: [
      DEFAULT_MAXIMUM_SCORE as number | null,
      [Validators.required, Validators.min(0.01), Validators.max(1000)],
    ],
  });

  ngOnInit(): void {
    this.load();
  }

  get showAddForm(): boolean {
    return this.items.length === 0 || this.editId !== null;
  }

  load(): void {
    this.loading = true;
    this.loadError = '';
    this.admissionYears
      .refreshYears()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (years) => {
          this.items = years;
          this.loading = false;
        },
        error: (err: HttpErrorResponse) => {
          this.loading = false;
          this.items = [];
          this.loadError = resolveApiError(err, 'تعذر تحميل سنوات القبول.');
        },
      });
  }

  edit(item: AdmissionYear): void {
    this.editId = item.id;
    this.formError = '';
    this.form.patchValue({
      year: item.year,
      maximumScore: item.maximumScore,
    });
  }

  cancelEdit(): void {
    this.editId = null;
    this.formError = '';
    this.form.reset({
      year: null,
      maximumScore: DEFAULT_MAXIMUM_SCORE,
    });
  }

  save(): void {
    this.form.markAllAsTouched();
    this.formError = '';
    if (this.form.invalid) return;

    const year = Number(this.form.value.year);
    const maximumScore = Number(this.form.value.maximumScore);
    this.saving = true;

    const req = this.editId
      ? this.api.updateAdmissionYear(this.editId, { year, maximumScore })
      : this.api.createAdmissionYear({ year, maximumScore });

    req.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.saving = false;
        this.cancelEdit();
        this.load();
      },
      error: (err: HttpErrorResponse) => {
        this.saving = false;
        this.formError = resolveApiError(err, 'تعذر حفظ السنة.');
      },
    });
  }

  openDeleteDialog(item: AdmissionYear): void {
    this.deleteTarget = item;
  }

  closeDeleteDialog(): void {
    if (this.deleting) return;
    this.deleteTarget = null;
  }

  confirmDelete(): void {
    if (!this.deleteTarget) return;
    this.deleting = true;
    this.api
      .deleteAdmissionYear(this.deleteTarget.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.deleting = false;
          this.deleteTarget = null;
          if (this.editId !== null) {
            this.cancelEdit();
          }
          this.load();
        },
        error: (err: HttpErrorResponse) => {
          this.deleting = false;
          this.formError = resolveApiError(err, 'تعذر حذف السنة.');
        },
      });
  }
}
