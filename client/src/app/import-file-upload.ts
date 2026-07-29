import { ApiResponse, ImportResult } from './models';
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

export function uploadImportFile(
  url: string,
  formData: FormData,
  onProgress: (progress: ImportUploadProgress) => void,
  signal?: AbortSignal,
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

    const settle = (handler: () => void) => {
      stopProcessingTimer();
      try {
        handler();
      } catch {
        reject({ status: xhr.status || 0, error: null } satisfies ImportUploadError);
      }
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
        settle(() => {
          const body = parseJsonResponse(xhr);
          const payload = extractImportPayload(body);
          if (!payload) {
            reject({
              status: xhr.status,
              error: body,
            } satisfies ImportUploadError);
            return;
          }
          onProgress({ phase: 'processing', percent: 100 });
          resolve(normalizeImportResult(payload));
        });
        return;
      }

      settle(() => {
        reject({
          status: xhr.status,
          error: parseJsonResponse(xhr),
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

function parseJsonResponse(xhr: XMLHttpRequest): ApiResponse<ImportResult> | null {
  if (xhr.response && typeof xhr.response === 'object') {
    return xhr.response as ApiResponse<ImportResult>;
  }

  if (!xhr.responseText) return null;

  try {
    return JSON.parse(xhr.responseText) as ApiResponse<ImportResult>;
  } catch {
    return null;
  }
}
