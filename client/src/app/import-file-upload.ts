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

    const abort = () => {
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

    xhr.upload.addEventListener('loadend', () => {
      onProgress({ phase: 'processing', percent: 90 });
    });

    xhr.addEventListener('load', () => {
      onProgress({ phase: 'processing', percent: 100 });

      if (xhr.status >= 200 && xhr.status < 300) {
        const body = xhr.response as ApiResponse<ImportResult>;
        resolve(normalizeImportResult(body.data));
        return;
      }

      reject({
        status: xhr.status,
        error: parseJsonResponse(xhr),
      } satisfies ImportUploadError);
    });

    xhr.addEventListener('error', () => {
      reject({ status: 0 } satisfies ImportUploadError);
    });

    xhr.addEventListener('abort', () => {
      reject({ status: 0, aborted: true } satisfies ImportUploadError);
    });

    xhr.send(formData);
  });
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
