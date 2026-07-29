import {
  ApiResponse,
  ImportJobStarted,
  ImportJobStatus,
  ImportResult,
  ImportUploadSession,
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

const DEFAULT_CHUNK_SIZE_BYTES = 5 * 1024 * 1024;
const SIMPLE_UPLOAD_THRESHOLD_BYTES = 20 * 1024 * 1024;

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
  if (file.size <= SIMPLE_UPLOAD_THRESHOLD_BYTES) {
    const form = new FormData();
    form.append('file', file);
    return uploadStudentResultsSingle(
      `/api/admin/admission-years/${yearId}/import-results`,
      form,
      onProgress,
      signal,
    );
  }

  return uploadStudentResultsChunked(yearId, file, onProgress, signal);
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

async function uploadStudentResultsChunked(
  yearId: number,
  file: File,
  onProgress: (progress: ImportUploadProgress) => void,
  signal?: AbortSignal,
): Promise<ImportResult> {
  const initialChunkSizeBytes = DEFAULT_CHUNK_SIZE_BYTES;
  const totalChunks = Math.ceil(file.size / initialChunkSizeBytes);
  let uploadId: string | null = null;

  try {
    if (signal?.aborted) {
      throw { status: 0, aborted: true } satisfies ImportUploadError;
    }

    const sessionResponse = await fetch(
      `/api/admin/admission-years/${yearId}/import-results/sessions`,
      {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          fileName: file.name,
          totalSize: file.size,
          totalChunks,
        }),
        signal,
      },
    );

    if (!sessionResponse.ok) {
      throw {
        status: sessionResponse.status,
        error: ((await sessionResponse.json().catch(() => null)) as ApiResponse<ImportResult> | null),
      } satisfies ImportUploadError;
    }

    const sessionBody =
      (await sessionResponse.json()) as ApiResponse<ImportUploadSession>;
    uploadId = extractUploadId(sessionBody);
    const chunkSizeBytes = extractChunkSizeBytes(sessionBody) ?? initialChunkSizeBytes;
    const effectiveTotalChunks = Math.ceil(file.size / chunkSizeBytes);
    if (!uploadId || effectiveTotalChunks !== totalChunks) {
      throw { status: sessionResponse.status, error: null } satisfies ImportUploadError;
    }

    let uploadedBytes = 0;
    for (let index = 0; index < effectiveTotalChunks; index++) {
      if (signal?.aborted) {
        throw { status: 0, aborted: true } satisfies ImportUploadError;
      }

      const start = index * chunkSizeBytes;
      const end = Math.min(start + chunkSizeBytes, file.size);
      const chunk = file.slice(start, end);

      await uploadChunk(uploadId, index, chunk, signal, (loaded) => {
        const currentBytes = uploadedBytes + loaded;
        const percent = Math.min(
          85,
          Math.max(1, Math.round((currentBytes / file.size) * 85)),
        );
        onProgress({ phase: 'uploading', percent });
      });

      uploadedBytes += chunk.size;
      onProgress({
        phase: 'uploading',
        percent: Math.min(85, Math.round((uploadedBytes / file.size) * 85)),
      });
    }

    onProgress({ phase: 'processing', percent: 90 });

    const completeResponse = await fetch(
      `/api/admin/import-uploads/${uploadId}/complete`,
      {
        method: 'POST',
        credentials: 'include',
        signal,
      },
    );

    if (!completeResponse.ok && completeResponse.status !== 202) {
      throw {
        status: completeResponse.status,
        error: ((await completeResponse.json().catch(() => null)) as ApiResponse<ImportResult> | null),
      } satisfies ImportUploadError;
    }

    const body = (await completeResponse.json()) as ApiResponse<ImportJobStarted>;
    const jobId = extractJobId(body);
    if (!jobId) {
      throw { status: completeResponse.status, error: null } satisfies ImportUploadError;
    }

    uploadId = null;
    return pollImportJob(jobId, onProgress, signal);
  } catch (error) {
    if (uploadId && !(error as ImportUploadError).aborted) {
      void fetch(`/api/admin/import-uploads/${uploadId}`, {
        method: 'DELETE',
        credentials: 'include',
      });
    }
    throw error;
  }
}

function uploadChunk(
  uploadId: string,
  chunkIndex: number,
  chunk: Blob,
  signal: AbortSignal | undefined,
  onChunkProgress: (loaded: number) => void,
): Promise<void> {
  return new Promise((resolve, reject) => {
    const xhr = new XMLHttpRequest();
    xhr.open('PUT', `/api/admin/import-uploads/${uploadId}/chunks/${chunkIndex}`);
    xhr.withCredentials = true;
    xhr.timeout = 0;

    const abort = () => {
      if (xhr.readyState !== XMLHttpRequest.DONE) {
        xhr.abort();
      }
    };

    signal?.addEventListener('abort', abort);

    xhr.upload.addEventListener('progress', (event) => {
      if (!event.lengthComputable) return;
      onChunkProgress(event.loaded);
    });

    xhr.addEventListener('load', () => {
      if (xhr.status === 204 || (xhr.status >= 200 && xhr.status < 300)) {
        resolve();
        return;
      }
      reject({
        status: xhr.status,
        error: parseJsonResponse<ImportResult>(xhr),
      } satisfies ImportUploadError);
    });

    xhr.addEventListener('error', () => {
      reject({ status: 0 } satisfies ImportUploadError);
    });

    xhr.addEventListener('abort', () => {
      reject({ status: 0, aborted: true } satisfies ImportUploadError);
    });

    xhr.send(chunk);
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

function extractChunkSizeBytes(
  body: ApiResponse<ImportUploadSession> | null,
): number | null {
  if (!body) return null;
  const envelope = body as ApiResponse<ImportUploadSession> & {
    Data?: ImportUploadSession;
  };
  const payload = envelope.data ?? envelope.Data;
  if (!payload) return null;
  const raw = payload as ImportUploadSession & { ChunkSizeBytes?: number };
  return raw.chunkSizeBytes ?? raw.ChunkSizeBytes ?? null;
}

function extractUploadId(
  body: ApiResponse<ImportUploadSession> | null,
): string | null {
  if (!body) return null;
  const envelope = body as ApiResponse<ImportUploadSession> & {
    Data?: ImportUploadSession;
  };
  const payload = envelope.data ?? envelope.Data;
  if (!payload) return null;
  const raw = payload as ImportUploadSession & { UploadId?: string };
  return raw.uploadId ?? raw.UploadId ?? null;
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
