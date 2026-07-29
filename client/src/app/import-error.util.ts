import { ApiResponse, ImportResult } from './models';

export function parseImportErrorResponse(
  response: ApiResponse<ImportResult> | null | undefined,
): ImportResult | null {
  if (!response) return null;

  const envelope = response as ApiResponse<ImportResult> & {
    Data?: ImportResult;
    Message?: string;
    Errors?: ApiResponse<ImportResult>['errors'];
  };

  const payload = envelope.data ?? envelope.Data;
  if (payload) {
    return normalizeImportResult(payload);
  }

  const errors = envelope.errors ?? envelope.Errors;
  if (!errors?.length) return null;

  return {
    success: false,
    message: envelope.message ?? envelope.Message ?? '',
    errors: errors.map((error) => ({
      rowNumber: error.rowNumber ?? 0,
      column: error.field,
      errorCode: '',
      message: error.message,
    })),
  };
}

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
      ErrorCode?: string;
      Message?: string;
    };
    return {
      rowNumber: item.rowNumber ?? item.RowNumber ?? 0,
      column: item.column ?? item.Column ?? '',
      errorCode: item.errorCode ?? item.ErrorCode ?? '',
      message: item.message ?? item.Message ?? '',
    };
  });
}

export function importErrorMessage(
  status: number,
  response: ApiResponse<ImportResult> | null | undefined,
): string {
  if (status === 0) {
    return 'انقطع الاتصال بالخادم اثناء الاستيراد. تحقق من اتصال الانترنت ثم حاول مرة اخرى.';
  }

  if (status === 403) {
    return 'ليس لديك صلاحية لتنفيذ هذا الاستيراد.';
  }

  if (status === 400 && response?.message) {
    return response.message;
  }

  if (status === 503) {
    return response?.message ?? 'الخدمة غير متاحة حالياً. حاول مرة أخرى لاحقاً.';
  }

  if (status === 502 || status === 504) {
    return 'انتهت مهلة الخادم (5 دقائق). قد يكون الاستيراد لا يزال جاريا — انتظر دقيقة ثم تحقق من عدد النتائج في لوحة التحكم.';
  }

  if (status === 401 || status === 403) {
    return 'انتهت جلسة تسجيل الدخول. سجّل الدخول مرة اخرى ثم أعد المحاولة.';
  }

  if (status >= 500) {
    return (
      response?.message ??
      'حدث خطأ في الخادم اثناء الاستيراد. راجع سجل Railway ثم حاول مرة اخرى.'
    );
  }

  if (response?.data?.errors?.length) {
    return `فشل التحقق من الملف (${response.data.errors.length} خطأ). راجع الجدول بالاسفل.`;
  }

  if (response?.errors?.length) {
    return `فشل التحقق من الملف (${response.errors.length} خطأ). راجع الجدول بالاسفل.`;
  }

  return response?.message ?? response?.data?.message ?? 'فشل الاستيراد.';
}
