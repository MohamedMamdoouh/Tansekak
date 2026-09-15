import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import {
  normalizePagedCutoffs,
  normalizePredictResponse,
  normalizeStudentResult,
  unwrapApiData,
} from '../utils/api-normalize.util';
import { firstValueFrom } from 'rxjs';
import {
  ImportUploadError,
  ImportUploadProgress,
  uploadImportFile,
  uploadStagedImportFile,
} from './import-file-upload';
import {
  AdmissionCutoff,
  AdmissionYear,
  ApiResponse,
  AuthUser,
  Config,
  Dashboard,
  DIRECT_STUDENT_IMPORT_LIMIT_BYTES,
  ImportJob,
  ImportResult,
  PagedCutoffs,
  PredictRequest,
  PredictResponse,
  STAGED_STUDENT_IMPORT_LIMIT_BYTES,
  StudentResult,
  UniversityFaculty,
} from '../models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  constructor(private http: HttpClient) {}

  getConfig(): Observable<Config> {
    return this.http
      .get<ApiResponse<Config>>('/api/config')
      .pipe(map((r) => r.data));
  }

  predict(body: PredictRequest): Observable<PredictResponse> {
    return this.http
      .post<ApiResponse<PredictResponse>>('/api/admission/predict', body)
      .pipe(map((r) => normalizePredictResponse(r.data)));
  }

  getThanaweyaResult(seatingNo: string): Observable<StudentResult> {
    return this.http
      .get<ApiResponse<StudentResult>>(
        `/api/thanaweya-results/${encodeURIComponent(seatingNo)}`,
      )
      .pipe(
        map((r) =>
          normalizeStudentResult(
            unwrapApiData(r as ApiResponse<StudentResult> & { Data?: StudentResult }),
          ),
        ),
      );
  }

  login(email: string, password: string): Observable<AuthUser> {
    return this.http
      .post<ApiResponse<AuthUser>>('/api/admin/auth/login', { email, password })
      .pipe(map((r) => r.data));
  }

  logout(): Observable<void> {
    return this.http
      .post<ApiResponse<unknown>>('/api/admin/auth/logout', {})
      .pipe(map(() => undefined));
  }

  me(): Observable<AuthUser | null> {
    return this.http
      .get<ApiResponse<AuthUser>>('/api/admin/auth/me')
      .pipe(map((r) => (r.success ? r.data : null)));
  }

  getDashboard(): Observable<Dashboard> {
    return this.http
      .get<ApiResponse<Dashboard>>('/api/admin/dashboard')
      .pipe(map((r) => r.data));
  }

  checkHealth(): Observable<boolean> {
    return this.http.get<{ status: string }>('/health').pipe(
      map((response) => response.status === 'healthy'),
      catchError(() => of(false)),
    );
  }

  getUniversityFaculties(search = ''): Observable<UniversityFaculty[]> {
    const q = search ? `?search=${encodeURIComponent(search)}` : '';
    return this.http
      .get<
        ApiResponse<UniversityFaculty[]>
      >(`/api/admin/university-faculties${q}`)
      .pipe(map((r) => r.data));
  }

  getAdmissionYears(): Observable<AdmissionYear[]> {
    return this.http
      .get<ApiResponse<AdmissionYear[]>>('/api/admin/admission-years')
      .pipe(map((r) => r.data));
  }

  getCurrentAdmissionYear(): Observable<AdmissionYear> {
    return this.http
      .get<ApiResponse<AdmissionYear>>('/api/admin/admission-years/current')
      .pipe(map((r) => r.data));
  }

  createAdmissionYear(payload: {
    year: number;
    maximumScore: number;
  }): Observable<AdmissionYear> {
    return this.http
      .post<ApiResponse<AdmissionYear>>('/api/admin/admission-years', payload)
      .pipe(map((r) => r.data));
  }

  updateAdmissionYear(
    id: number,
    payload: { year: number; maximumScore: number },
  ): Observable<AdmissionYear> {
    return this.http
      .put<ApiResponse<AdmissionYear>>(
        `/api/admin/admission-years/${id}`,
        payload,
      )
      .pipe(map((r) => r.data));
  }

  deleteAdmissionYear(id: number): Observable<void> {
    return this.http
      .delete<ApiResponse<unknown>>(`/api/admin/admission-years/${id}`)
      .pipe(map(() => undefined));
  }

  getCutoffs(
    yearId?: number,
    search = '',
    page = 1,
    pageSize = 10,
  ): Observable<PagedCutoffs> {
    const params = new URLSearchParams();
    if (yearId) params.set('yearId', String(yearId));
    if (search) params.set('search', search);
    params.set('page', String(page));
    params.set('pageSize', String(pageSize));
    const q = params.toString();
    return this.http
      .get<
        ApiResponse<PagedCutoffs | AdmissionCutoff[]>
      >(`/api/admin/admission-cutoffs?${q}`)
      .pipe(map((r) => normalizePagedCutoffs(r.data, page, pageSize)));
  }

  createCutoff(payload: Partial<AdmissionCutoff>): Observable<AdmissionCutoff> {
    return this.http
      .post<
        ApiResponse<AdmissionCutoff>
      >('/api/admin/admission-cutoffs', payload)
      .pipe(map((r) => r.data));
  }

  updateCutoff(
    id: number,
    payload: Partial<AdmissionCutoff>,
  ): Observable<AdmissionCutoff> {
    return this.http
      .put<
        ApiResponse<AdmissionCutoff>
      >(`/api/admin/admission-cutoffs/${id}`, payload)
      .pipe(map((r) => r.data));
  }

  deleteCutoff(id: number): Observable<void> {
    return this.http
      .delete<ApiResponse<unknown>>(`/api/admin/admission-cutoffs/${id}`)
      .pipe(map(() => undefined));
  }

  importCutoffs(
    yearId: number,
    track: string,
    file: File,
  ): Observable<ImportResult> {
    const form = this.buildCutoffImportFormData(file, track);
    return this.http
      .post<
        ApiResponse<ImportResult>
      >(`/api/admin/admission-years/${yearId}/import`, form)
      .pipe(map((r) => r.data));
  }

  importCutoffsWithProgress(
    yearId: number,
    track: string,
    file: File,
    onProgress: (progress: ImportUploadProgress) => void,
    signal?: AbortSignal,
  ): Promise<ImportResult> {
    const form = this.buildCutoffImportFormData(file, track);
    return uploadImportFile(
      this.http,
      `/api/admin/admission-years/${yearId}/import`,
      form,
      onProgress,
      signal,
    );
  }

  importStudentResultsWithProgress(
    yearId: number,
    file: File,
    onProgress: (progress: ImportUploadProgress) => void,
    signal?: AbortSignal,
  ): Promise<ImportResult> {
    if (file.size <= DIRECT_STUDENT_IMPORT_LIMIT_BYTES) {
      const form = new FormData();
      form.append('file', file);
      return uploadImportFile(
        this.http,
        `/api/admin/admission-years/${yearId}/import-results`,
        form,
        onProgress,
        signal,
      );
    }

    if (file.size > STAGED_STUDENT_IMPORT_LIMIT_BYTES) {
      return Promise.reject({
        status: 413,
        kind: 'http',
        error: {
          success: false,
          message: 'حجم الملف يتجاوز 100 ميجابايت.',
          data: null,
        },
      } satisfies ImportUploadError);
    }

    return this.importLargeStudentResults(yearId, file, onProgress, signal);
  }

  getImportJob(jobId: string): Observable<ImportJob> {
    return this.http
      .get<ApiResponse<ImportJob>>(`/api/admin/import-jobs/${jobId}`)
      .pipe(map((r) => r.data));
  }

  cancelImportJob(jobId: string): Observable<ImportJob> {
    return this.http
      .post<ApiResponse<ImportJob>>(`/api/admin/import-jobs/${jobId}/cancel`, {})
      .pipe(map((r) => r.data));
  }

  private async importLargeStudentResults(
    yearId: number,
    file: File,
    onProgress: (progress: ImportUploadProgress) => void,
    signal?: AbortSignal,
  ): Promise<ImportResult> {
    const form = new FormData();
    form.append('file', file);

    let jobId: string | null = null;
    try {
      jobId = await uploadStagedImportFile(
        this.http,
        `/api/admin/admission-years/${yearId}/import-results/from-upload`,
        form,
        onProgress,
        signal,
      );

      return await this.pollImportJob(jobId, onProgress, signal);
    } catch (err) {
      if (jobId && isAbortedImportError(err)) {
        try {
          await firstValueFrom(this.cancelImportJob(jobId));
        } catch {
          // Best-effort: UI already treats the op as cancelled.
        }
      }
      throw err;
    }
  }

  private async pollImportJob(
    jobId: string,
    onProgress: (progress: ImportUploadProgress) => void,
    signal?: AbortSignal,
  ): Promise<ImportResult> {
    let processingPercent = 90;

    while (true) {
      if (signal?.aborted) {
        throw { status: 0, aborted: true } satisfies ImportUploadError;
      }

      const job = await firstValueFrom(this.getImportJob(jobId));

      if (job.status === 'completed') {
        onProgress({ phase: 'processing', percent: 100 });
        return {
          success: true,
          message: job.message ?? 'Import completed.',
          importedCount: job.importedCount ?? undefined,
        };
      }

      if (job.status === 'failed') {
        onProgress({ phase: 'processing', percent: 100 });
        return {
          success: false,
          message: job.message ?? 'Import failed.',
        };
      }

      if (job.status === 'cancelled') {
        throw { status: 0, aborted: true } satisfies ImportUploadError;
      }

      processingPercent = Math.min(98, processingPercent + 1);
      onProgress({ phase: 'processing', percent: processingPercent });
      await new Promise((resolve) => setTimeout(resolve, 2500));
    }
  }

  private buildCutoffImportFormData(file: File, track: string): FormData {
    const form = new FormData();
    form.append('file', file);
    form.append('track', track);
    return form;
  }
}

function isAbortedImportError(err: unknown): err is ImportUploadError {
  return (
    typeof err === 'object' &&
    err !== null &&
    'aborted' in err &&
    (err as ImportUploadError).aborted === true
  );
}

export type { ImportUploadError, ImportUploadProgress };
