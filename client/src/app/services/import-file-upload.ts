import { ApiResponse, ImportResult } from '../models';
import { normalizeImportResult } from '../utils/import-error.util';

export type ImportUploadPhase = 'uploading' | 'processing';

export interface ImportUploadProgress {
  phase: ImportUploadPhase;
  percent: number;
}

export interface ImportUploadError {
  status: number;
  aborted?: boolean;
  error?: ApiResponse<unknown> | null;
}

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

function extractImportPayload(
  body: ApiResponse<ImportResult> | null,
): ImportResult | null {
  if (!body) return null;

  const envelope = body as ApiResponse<ImportResult> & {
    Data?: ImportResult;
  };

  return envelope.data ?? envelope.Data ?? null;
}

export function uploadToPresignedUrl(
  uploadUrl: string,
  file: File,
  onProgress: (percent: number) => void,
  signal?: AbortSignal,
): Promise<void> {
  return new Promise((resolve, reject) => {
    const xhr = new XMLHttpRequest();
    xhr.open('PUT', uploadUrl);
    xhr.setRequestHeader(
      'Content-Type',
      'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    );
    xhr.timeout = 0;

    const abort = () => {
      if (xhr.readyState !== XMLHttpRequest.DONE) {
        xhr.abort();
      }
    };

    signal?.addEventListener('abort', abort);

    xhr.upload.addEventListener('progress', (event) => {
      if (!event.lengthComputable) return;
      const percent = Math.min(85, Math.round((event.loaded / event.total) * 85));
      onProgress(Math.max(percent, 1));
    });

    xhr.addEventListener('load', () => {
      signal?.removeEventListener('abort', abort);
      if (xhr.status >= 200 && xhr.status < 300) {
        resolve();
        return;
      }
      reject({ status: xhr.status } satisfies ImportUploadError);
    });

    xhr.addEventListener('error', () => {
      signal?.removeEventListener('abort', abort);
      reject({ status: 0 } satisfies ImportUploadError);
    });

    xhr.addEventListener('timeout', () => {
      signal?.removeEventListener('abort', abort);
      reject({ status: 0 } satisfies ImportUploadError);
    });

    xhr.addEventListener('abort', () => {
      signal?.removeEventListener('abort', abort);
      reject({ status: 0, aborted: true } satisfies ImportUploadError);
    });

    xhr.send(file);
  });
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
