import { ApiResponse, ImportResult } from './models';

export function parseImportErrorResponse(
  response: ApiResponse<ImportResult> | null | undefined,
): ImportResult | null {
  if (!response) return null;

  if (response.data) {
    return normalizeImportResult(response.data);
  }

  if (!response.errors?.length) return null;

  return {
    success: false,
    message: response.message,
    errors: response.errors.map((error) => ({
      rowNumber: error.rowNumber ?? 0,
      column: error.field,
      errorCode: '',
      message: error.message,
    })),
  };
}

export function normalizeImportResult(data: ImportResult): ImportResult {
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
    errors: raw.errors ?? raw.Errors,
  };
}

export function importErrorMessage(
  status: number,
  response: ApiResponse<ImportResult> | null | undefined,
): string {
  if (status === 0) {
    return 'انقطع الاتصال بالخادم اثناء الاستيراد. الملف قد يكون كبيرا — انتظر دقيقة ثم حاول مرة اخرى.';
  }

  if (response?.data?.errors?.length) {
    return `فشل التحقق من الملف (${response.data.errors.length} خطأ). راجع الجدول بالاسفل.`;
  }

  if (response?.errors?.length) {
    return `فشل التحقق من الملف (${response.errors.length} خطأ). راجع الجدول بالاسفل.`;
  }

  return response?.message ?? response?.data?.message ?? 'فشل الاستيراد.';
}
