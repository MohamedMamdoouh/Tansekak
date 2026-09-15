import { ApiResponse, ImportResult } from '../models';
import { unwrapApiData } from './api-normalize.util';
import { resolveApiError } from './api-error.util';
import { ImportUploadFailureKind } from '../services/import-file-upload';

export function normalizeImportResult(
  data: ImportResult | null | undefined,
): ImportResult {
  if (!data) {
    return {
      success: false,
      message: 'استجابة غير صالحة من الخادم.',
    };
  }

  const raw = data as ImportResult & {
    Success?: boolean;
    Message?: string;
    ImportedCount?: number;
    Errors?: ImportResult['errors'];
  };

  return {
    success: raw.success ?? raw.Success ?? false,
    message: raw.message ?? raw.Message ?? '',
    importedCount: raw.importedCount ?? raw.ImportedCount,
    errors: normalizeImportErrors(raw.errors ?? raw.Errors),
  };
}

function normalizeImportErrors(
  errors: ImportResult['errors'] | undefined,
): ImportResult['errors'] {
  if (!errors?.length) return errors;

  return errors.map((error) => {
    const item = error as typeof error & {
      RowNumber?: number;
      Column?: string;
      Field?: string;
      ErrorCode?: string;
      Message?: string;
    };
    return {
      rowNumber: item.rowNumber ?? item.RowNumber ?? 0,
      column: item.column ?? item.Column ?? item.Field ?? '',
      errorCode: item.errorCode ?? item.ErrorCode ?? '',
      message: item.message ?? item.Message ?? '',
    };
  });
}

export function extractImportResult(
  response: ApiResponse<unknown> | null | undefined,
): ImportResult | null {
  const data = response ? unwrapApiData(response) : undefined;
  if (data && typeof data === 'object') {
    return normalizeImportResult(data as ImportResult);
  }

  if (response?.errors?.length) {
    return {
      success: false,
      message: response.message ?? '',
      errors: response.errors.map((error) => ({
        rowNumber: error.rowNumber ?? 0,
        column: error.field ?? '',
        errorCode: error.errorCode ?? '',
        message: error.message ?? '',
      })),
    };
  }

  return null;
}

export function importErrorMessage(
  status: number,
  response: ApiResponse<unknown> | null | undefined,
  options?: { kind?: ImportUploadFailureKind },
): string {
  if (options?.kind === 'aborted') {
    return 'تم إلغاء الاستيراد.';
  }

  if (status === 0 || options?.kind === 'api_unreachable') {
    return 'تعذر الاتصال بالخادم. شغّل الـ API على localhost:5080 (أو npm start مع dotnet run)، أو تحقق من أن نشر Render يعمل عبر /health.';
  }

  if (status === 502 || status === 504) {
    return 'انتهت مهلة الخادم (5 دقائق). قد يكون الاستيراد لا يزال جاريا — انتظر دقيقة ثم تحقق من عدد النتائج في لوحة التحكم.';
  }

  const importData = extractImportResult(response);
  if (importData?.errors?.length) {
    return `فشل التحقق من الملف (${importData.errors.length} خطأ). راجع الجدول بالأسفل.`;
  }

  if (response?.errors?.length) {
    return `فشل التحقق من الملف (${response.errors.length} خطأ). راجع الجدول بالأسفل.`;
  }

  return resolveApiError({ status, error: response }, 'فشل الاستيراد.');
}
