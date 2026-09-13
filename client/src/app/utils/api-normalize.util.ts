import {
  AdmissionCutoff,
  AdmissionResult,
  ApiResponse,
  ImportResult,
  PagedCutoffs,
  PredictResponse,
  StudentResult,
} from '../models';

export function normalizeStudentResult(
  data: StudentResult | null | undefined,
): StudentResult {
  if (!data) {
    return {
      seatingNo: '',
      arabicName: '',
      totalDegree: 0,
      studentCaseDesc: '',
      year: 0,
    };
  }

  const raw = data as StudentResult & {
    SeatingNo?: string;
    ArabicName?: string;
    TotalDegree?: number;
    StudentCaseDesc?: string;
    Year?: number;
    Track?: string | null;
    TrackRank?: number | null;
    TrackTotalStudents?: number | null;
  };

  return {
    seatingNo: raw.seatingNo ?? raw.SeatingNo ?? '',
    arabicName: raw.arabicName ?? raw.ArabicName ?? '',
    totalDegree: raw.totalDegree ?? raw.TotalDegree ?? 0,
    studentCaseDesc: raw.studentCaseDesc ?? raw.StudentCaseDesc ?? '',
    year: raw.year ?? raw.Year ?? 0,
    track: raw.track ?? raw.Track ?? null,
    trackRank: raw.trackRank ?? raw.TrackRank ?? null,
    trackTotalStudents:
      raw.trackTotalStudents ?? raw.TrackTotalStudents ?? null,
  };
}

export function normalizePredictResponse(
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

export function normalizePagedCutoffs(
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

export function unwrapApiData<T>(
  response: ApiResponse<T> & { Data?: T },
): T | undefined {
  return response.data ?? response.Data;
}
