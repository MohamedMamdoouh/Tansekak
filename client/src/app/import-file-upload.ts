import {
  ApiResponse,
  ImportJobStarted,
  ImportJobStatus,
  ImportResult,
  ImportUploadUrl,
} from './models';
import { normalizeImportResult } from './import-error.util';

export type ImportUploadPhase = 'uploading' | 'processing';

export interface ImportUploadProgress {
  phase: ImportUploadPhase;
  percent: number;
}

export interface ImportUploadError {
  status: number;
  aborted?: boolean;
  error?: ApiResponse<ImportResult> | null;
}

const DIRECT_UPLOAD_THRESHOLD_BYTES = 20 * 1024 * 1024;
const XLSX_CONTENT_TYPE =
  'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet';

export function uploadImportFile(
  url: string,
  formData: FormData,
  onProgress: (progress: ImportUploadProgress) => void,
  signal?: AbortSignal,
): Promise<ImportResult> {
  return uploadWithXhr(url, formData, onProgress, signal, (xhr) => {
    const body = parseJsonResponse<ImportResult>(xhr);
    const payload = extractImportPayload(body);
    if (!payload) {
      throw { status: xhr.status, error: body } satisfies ImportUploadError;
    }
    onProgress({ phase: 'processing', percent: 100 });
    return normalizeImportResult(payload);
  });
}

export function uploadStudentResultsImport(
  yearId: number,
  file: File,
  onProgress: (progress: ImportUploadProgress) => void,
  signal?: AbortSignal,
): Promise<ImportResult> {
  if (file.size <= DIRECT_UPLOAD_THRESHOLD_BYTES) {
    const form = new FormData();
    form.append('file', file);
    return uploadStudentResultsSingle(
      `/api/admin/admission-years/${yearId}/import-results`,
      form,
      onProgress,
      signal,
    );
  }

  return uploadStudentResultsViaR2(yearId, file, onProgress, signal);
}

function uploadStudentResultsSingle(
  url: string,
  formData: FormData,
  onProgress: (progress: ImportUploadProgress) => void,
  signal?: AbortSignal,
): Promise<ImportResult> {
  return uploadWithXhr(url, formData, onProgress, signal, async (xhr) => {
    if (xhr.status === 202) {
      return startPollingFromResponse(xhr, onProgress, signal);
    }

    const body = parseJsonResponse<ImportResult>(xhr);
    const payload = extractImportPayload(body);
    if (!payload) {
      throw { status: xhr.status, error: body } satisfies ImportUploadError;
    }
    onProgress({ phase: 'processing', percent: 100 });
    return normalizeImportResult(payload);
  });
}

async function uploadStudentResultsViaR2(
  yearId: number,
  file: File,
  onProgress: (progress: ImportUploadProgress) => void,
  signal?: AbortSignal,
): Promise<ImportResult> {
  if (signal?.aborted) {
    throw { status: 0, aborted: true } satisfies ImportUploadError;
  }

  const uploadUrlResponse = await fetch(
    `/api/admin/admission-years/${yearId}/import-results/upload-url`,
    {
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        fileName: file.name,
        totalSize: file.size,
      }),
      signal,
    },
  );

  if (!uploadUrlResponse.ok) {
    throw {
      status: uploadUrlResponse.status,
      error: ((await uploadUrlResponse.json().catch(() => null)) as ApiResponse<ImportResult> | null),
    } satisfies ImportUploadError;
  }

  const uploadUrlBody =
    (await uploadUrlResponse.json()) as ApiResponse<ImportUploadUrl>;
  const uploadTarget = extractUploadUrl(uploadUrlBody);
  if (!uploadTarget) {
    throw { status: uploadUrlResponse.status, error: null } satisfies ImportUploadError;
  }

  await uploadFileToPresignedUrl(
    uploadTarget.uploadUrl,
    file,
    signal,
    (percent) => onProgress({ phase: 'uploading', percent }),
  );

  onProgress({ phase: 'processing', percent: 90 });

  const importResponse = await fetch(
    `/api/admin/admission-years/${yearId}/import-results/from-storage`,
    {
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        objectKey: uploadTarget.objectKey,
        fileName: file.name,
      }),
      signal,
    },
  );

  if (!importResponse.ok && importResponse.status !== 202) {
    throw {
      status: importResponse.status,
      error: ((await importResponse.json().catch(() => null)) as ApiResponse<ImportResult> | null),
    } satisfies ImportUploadError;
  }

  const body = (await importResponse.json()) as ApiResponse<ImportJobStarted>;
  const jobId = extractJobId(body);
  if (!jobId) {
    throw { status: importResponse.status, error: null } satisfies ImportUploadError;
  }

  return pollImportJob(jobId, onProgress, signal);
}

function uploadFileToPresignedUrl(
  uploadUrl: string,
  file: File,
  signal: AbortSignal | undefined,
  onProgress: (percent: number) => void,
): Promise<void> {
  return new Promise((resolve, reject) => {
    const xhr = new XMLHttpRequest();
    xhr.open('PUT', uploadUrl);
    xhr.timeout = 0;
    xhr.setRequestHeader('Content-Type', XLSX_CONTENT_TYPE);

    const abort = () => {
      if (xhr.readyState !== XMLHttpRequest.DONE) {
        xhr.abort();
      }
    };

    signal?.addEventListener('abort', abort);

    xhr.upload.addEventListener('progress', (event) => {
      if (!event.lengthComputable) return;
      const percent = Math.min(
        85,
        Math.max(1, Math.round((event.loaded / event.total) * 85)),
      );
      onProgress(percent);
    });

    xhr.addEventListener('load', () => {
      if (xhr.status >= 200 && xhr.status < 300) {
        resolve();
        return;
      }
      reject({ status: xhr.status } satisfies ImportUploadError);
    });

    xhr.addEventListener('error', () => {
      reject({ status: 0 } satisfies ImportUploadError);
    });

    xhr.addEventListener('abort', () => {
      reject({ status: 0, aborted: true } satisfies ImportUploadError);
    });

    xhr.send(file);
  });
}

async function startPollingFromResponse(
  xhr: XMLHttpRequest,
  onProgress: (progress: ImportUploadProgress) => void,
  signal?: AbortSignal,
): Promise<ImportResult> {
  const body = parseJsonResponse<ImportJobStarted>(xhr);
  const jobId = extractJobId(body);
  if (!jobId) {
    throw { status: 202, error: null } satisfies ImportUploadError;
  }
  return pollImportJob(jobId, onProgress, signal);
}

function uploadWithXhr(
  url: string,
  formData: FormData,
  onProgress: (progress: ImportUploadProgress) => void,
  signal: AbortSignal | undefined,
  onSuccess: (xhr: XMLHttpRequest) => ImportResult | Promise<ImportResult>,
): Promise<ImportResult> {
  return new Promise((resolve, reject) => {
    const xhr = new XMLHttpRequest();
    xhr.open('POST', url);
    xhr.withCredentials = true;
    xhr.responseType = 'json';
    xhr.timeout = 0;

    let processingTimer: ReturnType<typeof setInterval> | null = null;
    let processingPercent = 90;

    const stopProcessingTimer = () => {
      if (processingTimer !== null) {
        clearInterval(processingTimer);
        processingTimer = null;
      }
    };

    const startProcessingTimer = () => {
      stopProcessingTimer();
      processingPercent = 90;
      onProgress({ phase: 'processing', percent: processingPercent });
      processingTimer = setInterval(() => {
        if (processingPercent < 98) {
          processingPercent += 1;
          onProgress({ phase: 'processing', percent: processingPercent });
        }
      }, 2500);
    };

    const settle = (handler: () => void | Promise<void>) => {
      stopProcessingTimer();
      Promise.resolve()
        .then(handler)
        .catch(() => {
          reject({ status: xhr.status || 0, error: null } satisfies ImportUploadError);
        });
    };

    const abort = () => {
      stopProcessingTimer();
      if (xhr.readyState !== XMLHttpRequest.DONE) {
        xhr.abort();
      }
    };

    signal?.addEventListener('abort', abort);

    xhr.upload.addEventListener('progress', (event) => {
      if (!event.lengthComputable) return;
      const percent = Math.min(85, Math.round((event.loaded / event.total) * 85));
      onProgress({ phase: 'uploading', percent: Math.max(percent, 1) });
    });

    xhr.upload.addEventListener('loadend', startProcessingTimer);

    xhr.addEventListener('load', () => {
      if (xhr.status >= 200 && xhr.status < 300) {
        settle(async () => {
          try {
            resolve(await onSuccess(xhr));
          } catch (error) {
            reject(error);
          }
        });
        return;
      }

      settle(() => {
        reject({
          status: xhr.status,
          error: parseJsonResponse<ImportResult>(xhr),
        } satisfies ImportUploadError);
      });
    });

    xhr.addEventListener('error', () => {
      settle(() => {
        reject({ status: 0 } satisfies ImportUploadError);
      });
    });

    xhr.addEventListener('timeout', () => {
      settle(() => {
        reject({ status: 0 } satisfies ImportUploadError);
      });
    });

    xhr.addEventListener('abort', () => {
      settle(() => {
        reject({ status: 0, aborted: true } satisfies ImportUploadError);
      });
    });

    xhr.send(formData);
  });
}

async function pollImportJob(
  jobId: string,
  onProgress: (progress: ImportUploadProgress) => void,
  signal?: AbortSignal,
): Promise<ImportResult> {
  let processingPercent = 90;
  onProgress({ phase: 'processing', percent: processingPercent });

  const timer = setInterval(() => {
    if (processingPercent < 98) {
      processingPercent += 1;
      onProgress({ phase: 'processing', percent: processingPercent });
    }
  }, 3000);

  try {
    while (true) {
      if (signal?.aborted) {
        throw { status: 0, aborted: true } satisfies ImportUploadError;
      }

      const response = await fetch(`/api/admin/import-jobs/${jobId}`, {
        credentials: 'include',
        signal,
      });

      if (!response.ok) {
        throw {
          status: response.status,
          error: ((await response.json().catch(() => null)) as ApiResponse<ImportResult> | null),
        } satisfies ImportUploadError;
      }

      const body = (await response.json()) as ApiResponse<ImportJobStatus>;
      const status = extractJobStatus(body);
      if (!status) {
        throw { status: response.status, error: null } satisfies ImportUploadError;
      }

      if (status.status === 'completed') {
        onProgress({ phase: 'processing', percent: 100 });
        if (!status.result) {
          throw { status: 500, error: null } satisfies ImportUploadError;
        }
        const result = normalizeImportResult(status.result);
        if (!result.success) {
          throw {
            status: 400,
            error: {
              success: false,
              message: result.message,
              data: result,
            },
          } satisfies ImportUploadError;
        }
        return result;
      }

      if (status.status === 'failed') {
        onProgress({ phase: 'processing', percent: 100 });
        const result = status.result
          ? normalizeImportResult(status.result)
          : {
              success: false,
              message: status.message ?? 'فشل الاستيراد.',
            };
        throw {
          status: 400,
          error: {
            success: false,
            message: result.message,
            data: result,
          },
        } satisfies ImportUploadError;
      }

      await sleep(2000);
    }
  } finally {
    clearInterval(timer);
  }
}

function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

function extractImportPayload(
  body: ApiResponse<ImportResult> | null,
): ImportResult | null {
  if (!body) return null;

  const envelope = body as ApiResponse<ImportResult> & {
    Data?: ImportResult;
  };

  return envelope.data ?? envelope.Data ?? null;
}

function extractUploadUrl(
  body: ApiResponse<ImportUploadUrl> | null,
): ImportUploadUrl | null {
  if (!body) return null;
  const envelope = body as ApiResponse<ImportUploadUrl> & {
    Data?: ImportUploadUrl;
  };
  const payload = envelope.data ?? envelope.Data;
  if (!payload) return null;

  const raw = payload as ImportUploadUrl & {
    UploadUrl?: string;
    ObjectKey?: string;
    ExpiresInSeconds?: number;
  };

  const uploadUrl = raw.uploadUrl ?? raw.UploadUrl;
  const objectKey = raw.objectKey ?? raw.ObjectKey;
  if (!uploadUrl || !objectKey) return null;

  return {
    uploadUrl,
    objectKey,
    expiresInSeconds: raw.expiresInSeconds ?? raw.ExpiresInSeconds ?? 0,
  };
}

function extractJobId(body: ApiResponse<ImportJobStarted> | null): string | null {
  if (!body) return null;
  const envelope = body as ApiResponse<ImportJobStarted> & {
    Data?: ImportJobStarted;
  };
  const payload = envelope.data ?? envelope.Data;
  if (!payload) return null;
  const raw = payload as ImportJobStarted & { JobId?: string };
  return raw.jobId ?? raw.JobId ?? null;
}

function extractJobStatus(
  body: ApiResponse<ImportJobStatus> | null,
): ImportJobStatus | null {
  if (!body) return null;
  const envelope = body as ApiResponse<ImportJobStatus> & {
    Data?: ImportJobStatus;
  };
  const payload = envelope.data ?? envelope.Data;
  if (!payload) return null;

  const raw = payload as ImportJobStatus & {
    JobId?: string;
    Status?: string;
    Result?: ImportResult;
    Message?: string;
  };

  return {
    jobId: raw.jobId ?? raw.JobId ?? '',
    status: raw.status ?? raw.Status ?? '',
    result: raw.result ?? raw.Result,
    message: raw.message ?? raw.Message,
  };
}

function parseJsonResponse<T>(xhr: XMLHttpRequest): ApiResponse<T> | null {
  if (xhr.response && typeof xhr.response === 'object') {
    return xhr.response as ApiResponse<T>;
  }

  if (!xhr.responseText) return null;

  try {
    return JSON.parse(xhr.responseText) as ApiResponse<T>;
  } catch {
    return null;
  }
}
