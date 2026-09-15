namespace Tansekak.Application.Common;

public static class ArabicErrorCatalog
{
    private static readonly Dictionary<string, string> Messages = new(StringComparer.Ordinal)
    {
        [ApiErrorCodes.InternalError] = "حدث خطأ غير متوقع. حاول مرة أخرى لاحقاً.",
        [ApiErrorCodes.NotFound] = "العنصر المطلوب غير موجود.",
        [ApiErrorCodes.ValidationFailed] = "يرجى التحقق من البيانات المدخلة.",
        [ApiErrorCodes.InvalidCredentials] = "بيانات الدخول غير صحيحة.",
        [ApiErrorCodes.NotAuthenticated] = "يجب تسجيل الدخول للمتابعة.",
        [ApiErrorCodes.Forbidden] = "ليس لديك صلاحية لتنفيذ هذا الإجراء.",
        [ApiErrorCodes.ServiceUnavailable] = "الخدمة غير متاحة حالياً. حاول مرة أخرى لاحقاً.",
        [ApiErrorCodes.NoCurrentYear] = "لم يتم تعيين سنة قبول حالية.",
        [ApiErrorCodes.InvalidTrack] = "الشعبة غير صحيحة.",
        [ApiErrorCodes.InvalidUniversityType] = "نوع الجامعة غير صحيح.",
        [ApiErrorCodes.ScoreExceedsMax] = "المجموع يتجاوز الحد الأقصى المسموح ({0}).",
        [ApiErrorCodes.CutoffNegative] = "يجب أن يكون الحد الأدنى صفراً أو أكثر.",
        [ApiErrorCodes.CutoffDuplicate] = "يوجد بالفعل حد قبول لهذه الكلية والشعبة في السنة المحددة.",
        [ApiErrorCodes.AdmissionYearNotFound] = "سنة القبول غير موجودة.",
        [ApiErrorCodes.AdmissionYearDuplicate] = "هذه السنة موجودة بالفعل.",
        [ApiErrorCodes.AdmissionYearLimitReached] = "يوجد سنة قبول بالفعل. احذفها أولاً لإضافة سنة جديدة.",
        [ApiErrorCodes.UniversityFacultyNotFound] = "كلية الجامعة غير موجودة.",
        [ApiErrorCodes.StudentResultNotFound] = "لم يتم العثور على نتيجة لهذا الرقم.",
        [ApiErrorCodes.FileRequired] = "الملف مطلوب.",
        [ApiErrorCodes.TrackRequired] = "الشعبة مطلوبة.",
        [ApiErrorCodes.FileNameRequired] = "اسم الملف مطلوب.",
        [ApiErrorCodes.ObjectKeyRequired] = "مفتاح الملف مطلوب.",
        [ApiErrorCodes.OnlyMdFiles] = "يُسمح فقط بملفات .md.",
        [ApiErrorCodes.OnlyXlsxFiles] = "يُسمح فقط بملفات .xlsx.",
        [ApiErrorCodes.FileTooLarge] = "حجم الملف يتجاوز حد الرفع المباشر (20 ميجابايت). استخدم رفع R2.",
        [ApiErrorCodes.R2NotConfigured] = "رفع الملفات الكبيرة غير متاح. يرجى ضبط Cloudflare R2.",
        [ApiErrorCodes.InvalidObjectKey] = "مفتاح الملف غير صالح.",
        [ApiErrorCodes.ImportJobMissingKey] = "مهمة الاستيراد لا تحتوي على مفتاح ملف.",
        [ApiErrorCodes.ImportJobSuperseded] = "تم إلغاء مهمة الاستيراد لأنها استُبدلت باستيراد أحدث.",
        [ApiErrorCodes.ImportJobCancelled] = "تم إلغاء الاستيراد.",
        [ApiErrorCodes.ImportJobNotCancellable] = "لا يمكن إلغاء مهمة الاستيراد في حالتها الحالية.",
        [ApiErrorCodes.AllowedTracksRequired] = "يجب تحديد شعبة واحدة على الأقل.",
        [ApiErrorCodes.FacultyTrackNotAllowed] = "الكلية غير متاحة للشعبة المحددة.",
        [ApiErrorCodes.Required] = "هذا الحقل مطلوب.",
        [ApiErrorCodes.Invalid] = "قيمة غير صالحة.",
        [ApiErrorCodes.Empty] = "الملف فارغ أو لا يحتوي على بيانات.",
        [ApiErrorCodes.Duplicate] = "قيمة مكررة.",
        [ApiErrorCodes.MissingColumn] = "عمود مطلوب مفقود ({0}).",
        [ApiErrorCodes.InvalidRow] = "تنسيق الصف غير صحيح.",
        [ApiErrorCodes.Malformed] = "تنسيق البيانات غير صحيح.",
        [ApiErrorCodes.TrackNotAllowed] = "الكلية غير متاحة للشعبة المحددة.",
        [ApiErrorCodes.ScoreMustBePositive] = "يجب أن يكون الحد الأدنى أكبر من صفر.",
        [ApiErrorCodes.DuplicateRow] = "صف مكرر في الملف.",
        [ApiErrorCodes.CollegeContainsScore] = "عمود الكلية يحتوي على قيمة درجة.",
        [ApiErrorCodes.CutoffMustBeNumber] = "يجب أن يكون الحد الأدنى رقماً.",
        [ApiErrorCodes.TotalDegreeInvalid] = "يجب أن يكون المجموع رقماً صالحاً.",
        [ApiErrorCodes.TotalDegreeNegative] = "يجب أن يكون المجموع صفراً أو أكثر.",
        [ApiErrorCodes.WorkbookEmpty] = "ملف Excel لا يحتوي على أوراق عمل.",
        [ApiErrorCodes.WorksheetEmpty] = "ورقة العمل لا تحتوي على بيانات.",
        [ApiErrorCodes.FileNoDataRows] = "الملف لا يحتوي على صفوف بيانات.",
        [ApiErrorCodes.UnresolvedCollege] = "تعذر مطابقة \"{0}\" مع جامعة/كلية في النظام.",
        [ApiErrorCodes.FieldTooLong] = "القيمة في {0} أطول من الحد المسموح ({1} حرف).",
        [ApiErrorCodes.ImportFileInvalid] = "الملف تالف أو بصيغة غير مدعومة. تأكد أنه ملف Excel (.xlsx) صالح.",
        [ApiErrorCodes.ImportJobMemoryFailed] = "نفدت ذاكرة الخادم أثناء معالجة الملف. جرّب تقسيم الملف إلى أجزاء أصغر.",
        [ApiErrorCodes.ImportJobTimeout] = "انتهت مهلة معالجة الاستيراد. حاول مرة أخرى أو قسّم الملف.",
    };

    public static string GetMessage(string errorCode) =>
        Messages.TryGetValue(errorCode, out var message)
            ? message
            : Messages[ApiErrorCodes.InternalError];

    public static string GetMessage(string errorCode, params object[] args)
    {
        var template = GetMessage(errorCode);
        return args.Length == 0 ? template : string.Format(template, args);
    }

    public static string GetImportFieldMessage(string errorCode, params object[] args) =>
        GetMessage(errorCode, args);
}
