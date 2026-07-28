import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import {
  AdmissionCutoff,
  AdmissionResult,
  AdmissionYear,
  ApiResponse,
  AuthUser,
  Config,
  Dashboard,
  ImportResult,
  PagedCutoffs,
  PredictRequest,
  PredictResponse,
  StudentResult,
  UniversityFaculty,
} from './models';

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
      .pipe(map((r) => this.normalizePredictResponse(r.data)));
  }

  lookupThanaweyaResult(seatingNo: string): Observable<StudentResult> {
    return this.http
      .get<ApiResponse<StudentResult>>(
        `/api/thanaweya-results/${encodeURIComponent(seatingNo)}`,
      )
      .pipe(map((r) => this.normalizeStudentResult(r.data)));
  }

  private normalizeStudentResult(
    data: StudentResult | null | undefined,
  ): StudentResult {
    const raw = data as StudentResult & {
      SeatingNo?: string;
      ArabicName?: string;
      TotalDegree?: number;
      StudentCaseDesc?: string;
      Year?: number;
    };
    return {
      seatingNo: raw.seatingNo ?? raw.SeatingNo ?? '',
      arabicName: raw.arabicName ?? raw.ArabicName ?? '',
      totalDegree: raw.totalDegree ?? raw.TotalDegree ?? 0,
      studentCaseDesc: raw.studentCaseDesc ?? raw.StudentCaseDesc ?? '',
      year: raw.year ?? raw.Year ?? 0,
    };
  }

  private normalizePredictResponse(
    data: PredictResponse | null | undefined,
  ): PredictResponse {
    if (!data) {
      return { results: [], hasMore: false, totalCount: 0 };
    }

    const raw = data as PredictResponse & {
      Results?: AdmissionResult[];
      HasMore?: boolean;
      TotalCount?: number;
    };

    return {
      results: raw.results ?? raw.Results ?? [],
      hasMore: raw.hasMore ?? raw.HasMore ?? false,
      totalCount: raw.totalCount ?? raw.TotalCount ?? raw.results?.length ?? 0,
    };
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
      .pipe(map((r) => this.normalizePagedCutoffs(r.data, page, pageSize)));
  }

  private normalizePagedCutoffs(
    data: PagedCutoffs | AdmissionCutoff[] | null | undefined,
    page: number,
    pageSize: number,
  ): PagedCutoffs {
    if (!data) {
      return { items: [], totalCount: 0, page, pageSize };
    }

    if (Array.isArray(data)) {
      const start = (page - 1) * pageSize;
      return {
        items: data.slice(start, start + pageSize),
        totalCount: data.length,
        page,
        pageSize,
      };
    }

    const raw = data as PagedCutoffs & {
      Items?: AdmissionCutoff[];
      TotalCount?: number;
      Page?: number;
      PageSize?: number;
    };

    return {
      items: raw.items ?? raw.Items ?? [],
      totalCount: raw.totalCount ?? raw.TotalCount ?? 0,
      page: raw.page ?? raw.Page ?? page,
      pageSize: raw.pageSize ?? raw.PageSize ?? pageSize,
    };
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
    const form = new FormData();
    form.append('file', file);
    form.append('track', track);
    return this.http
      .post<
        ApiResponse<ImportResult>
      >(`/api/admin/admission-years/${yearId}/import`, form)
      .pipe(map((r) => r.data));
  }

  importStudentResults(
    yearId: number,
    file: File,
  ): Observable<ImportResult> {
    const form = new FormData();
    form.append('file', file);
    return this.http
      .post<
        ApiResponse<ImportResult>
      >(`/api/admin/admission-years/${yearId}/import-results`, form)
      .pipe(map((r) => r.data));
  }
}
