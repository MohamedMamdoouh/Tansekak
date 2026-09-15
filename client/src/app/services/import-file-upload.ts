import { HttpClient, HttpEventType } from '@angular/common/http';
import { ApiResponse, ImportResult } from '../models';
import { normalizeImportResult } from '../utils/import-error.util';

export type ImportUploadPhase = 'uploading' | 'processing';

export type ImportUploadFailureKind = 'api_unreachable' | 'aborted' | 'http';

export interface ImportUploadProgress {
  phase: ImportUploadPhase;
  percent: number;
}

export interface ImportUploadError {
  status: number;
  aborted?: boolean;
  kind?: ImportUploadFailureKind;
  error?: ApiResponse<unknown> | null;
}

export function uploadImportFile(
  http: HttpClient,
  url: string,
  formData: FormData,
  onProgress: (progress: ImportUploadProgress) => void,
  signal?: AbortSignal,
): Promise<ImportResult> {
  return uploadMultipart(http, url, formData, onProgress, signal, (body) => {
    const payload = extractImportPayload(body as ApiResponse<ImportResult> | null);
    return payload ? normalizeImportResult(payload) : null;
  });
}

function uploadMultipart<T>(
  http: HttpClient,
  url: string,
  formData: FormData,
  onProgress: (progress: ImportUploadProgress) => void,
  signal: AbortSignal | undefined,
  extract: (body: ApiResponse<unknown> | null | undefined) => T | null,
  options?: { completePercent?: number },
): Promise<T> {
  return new Promise((resolve, reject) => {
    let processingTimer: ReturnType<typeof setInterval> | null = null;
    let processingPercent = 90;
    let processingStarted = false;
    const completePercent = options?.completePercent ?? 100;

    const stopProcessingTimer = () => {
      if (processingTimer !== null) {
        clearInterval(processingTimer);
        processingTimer = null;
      }
    };

    const startProcessingTimer = () => {
      if (processingStarted) return;
      processingStarted = true;
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

    const fail = (error: ImportUploadError) => {
      stopProcessingTimer();
      reject(error);
    };

    const subscription = http
      .post<ApiResponse<unknown>>(url, formData, {
        reportProgress: true,
        observe: 'events',
        ...(signal ? { signal } : {}),
      })
      .subscribe({
        next: (event) => {
          if (event.type === HttpEventType.UploadProgress) {
            if (event.total) {
              const percent = Math.min(
                85,
                Math.round((event.loaded / event.total) * 85),
              );
              onProgress({ phase: 'uploading', percent: Math.max(percent, 1) });
              if (event.loaded >= event.total) {
                startProcessingTimer();
              }
            }
            return;
          }

          if (event.type === HttpEventType.Response) {
            stopProcessingTimer();
            if (event.status >= 200 && event.status < 300) {
              const payload = extract(event.body);
              if (payload === null) {
                fail({
                  status: event.status,
                  error: event.body,
                  kind: 'http',
                });
                return;
              }
              onProgress({ phase: 'processing', percent: completePercent });
              resolve(payload);
              return;
            }

            fail({
              status: event.status,
              error: event.body,
              kind: 'http',
            });
          }
        },
        error: (err: {
          status?: number;
          error?: ApiResponse<unknown> | null;
          name?: string;
        }) => {
          if (signal?.aborted || err.name === 'AbortError') {
            fail({ status: 0, aborted: true, kind: 'aborted' });
            return;
          }

          const status = err.status ?? 0;
          fail({
            status,
            error: err.error ?? null,
            kind: status === 0 ? 'api_unreachable' : 'http',
          });
        },
      });

    signal?.addEventListener(
      'abort',
      () => {
        subscription.unsubscribe();
        fail({ status: 0, aborted: true, kind: 'aborted' });
      },
      { once: true },
    );
  });
}

function extractImportPayload(
  body: ApiResponse<ImportResult> | null | undefined,
): ImportResult | null {
  if (!body) return null;

  const envelope = body as ApiResponse<ImportResult> & {
    Data?: ImportResult;
  };

  return envelope.data ?? envelope.Data ?? null;
}
