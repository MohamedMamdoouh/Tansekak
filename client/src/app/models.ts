export interface AuthUser {
  email: string;
  role: string;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors?: { field: string; message: string; rowNumber?: number }[];
}

export interface Config {
  appName: string;
  currentYear: number;
  maximumScore: number;
  tracks: string[];
}

export interface PredictRequest {
  track: string;
  score: number;
  page: number;
  pageSize: number;
}

export interface NamedEntity {
  nameAr: string;
}

export interface AdmissionResult {
  university: NamedEntity;
  faculty: NamedEntity;
}

export interface PredictResponse {
  results: AdmissionResult[];
  hasMore: boolean;
  totalCount: number;
}

export interface Dashboard {
  governoratesCount: number;
  universitiesCount: number;
  facultiesCount: number;
  universityFacultiesCount: number;
  cutoffsCount: number;
  studentResultsCount: number;
  currentYear: number | null;
}

export interface UniversityFaculty {
  id: number;
  universityId: number;
  facultyId: number;
  universityName?: string;
  facultyName?: string;
}
export interface AdmissionYear {
  id: number;
  year: number;
  maximumScore: number;
  isCurrent: boolean;
}
export interface AdmissionCutoff {
  id: number;
  admissionYearId: number;
  universityFacultyId: number;
  track: string;
  cutoffScore: number;
  universityName?: string;
  facultyName?: string;
}

export interface PagedCutoffs {
  items: AdmissionCutoff[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface ImportResult {
  success: boolean;
  message: string;
  importedCount?: number;
  errors?: {
    rowNumber: number;
    column: string;
    errorCode: string;
    message: string;
  }[];
}

export interface ImportJobStarted {
  jobId: string;
  status: string;
}

export interface ImportUploadSession {
  uploadId: string;
  chunkSizeBytes: number;
}

export interface ImportJobStatus {
  jobId: string;
  status: string;
  result?: ImportResult;
  message?: string;
}

export interface StudentResult {
  seatingNo: string;
  arabicName: string;
  totalDegree: number;
  studentCaseDesc: string;
  year: number;
}

export const TRACK_LABELS: Record<string, string> = {
  Science: 'علمي علوم',
  Mathematics: 'علمي رياضة',
  Literature: 'أدبي',
};

export const TRACK_OPTIONS = [
  { value: 'Science', label: 'علمي علوم' },
  { value: 'Mathematics', label: 'علمي رياضة' },
  { value: 'Literature', label: 'أدبي' },
];
