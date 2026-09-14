import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../services/api.service';
import { resolveApiError } from '../../../utils/api-error.util';
import { HttpErrorResponse } from '@angular/common/http';
import { AdmissionYearStore } from '../../../services/admission-year.store';
import {
  AdmissionCutoff,
  AdmissionYear,
  TRACK_OPTIONS,
  UniversityFaculty,
} from '../../../models';
import {
  ADMIN_CUTOFFS_PAGE_SIZE,
  SEARCH_DEBOUNCE_MS,
} from '../../../constants/pagination.constants';
import { getTrackLabel } from '../../../utils/track-label.util';

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
  templateUrl: './cutoffs.component.html',
  styleUrl: './cutoffs.component.scss',
})
export class AdminCutoffsComponent implements OnInit {
  private api = inject(ApiService);
  private admissionYears = inject(AdmissionYearStore);
  private fb = inject(FormBuilder);
  private destroyRef = inject(DestroyRef);

  items: AdmissionCutoff[] = [];
  universityFaculties: UniversityFaculty[] = [];
  currentYear?: AdmissionYear;
  currentYearId = 0;
  search = '';
  page = 1;
  pageSize = ADMIN_CUTOFFS_PAGE_SIZE;
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
    this.admissionYears
      .refreshCurrentYear()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((year) => {
        this.currentYear = year;
        this.currentYearId = year.id;
        this.updateCutoffScoreValidators(year.maximumScore);
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
        error: (err: HttpErrorResponse) => {
          this.loading = false;
          this.items = [];
          this.totalCount = 0;
          this.loadError = resolveApiError(err, 'تعذر تحميل السجلات.');
        },
      });
  }

  onSearchChange(): void {
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => {
      this.page = 1;
      this.load();
    }, SEARCH_DEBOUNCE_MS);
  }

  goToPage(nextPage: number): void {
    if (nextPage < 1 || nextPage > this.totalPages) return;
    this.page = nextPage;
    this.load();
  }

  trackLabel(track: string): string {
    return getTrackLabel(track);
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
      error: (err: HttpErrorResponse) => {
        this.saving = false;
        this.formError = resolveApiError(err, 'تعذر حفظ السجل.');
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
      error: (err: HttpErrorResponse) => {
        this.deleting = false;
        this.formError = resolveApiError(err, 'تعذر حذف السجل.');
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
