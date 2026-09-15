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
        [ApiErrorCodes.OnlyMdFiles] = "يُسمح فقط بملفات .md.",
        [ApiErrorCodes.AllowedTracksRequired] = "يجب تحديد شعبة واحدة على الأقل.",
        [ApiErrorCodes.FacultyTrackNotAllowed] = "الكلية غير متاحة للشعبة المحددة.",
        [ApiErrorCodes.Required] = "هذا الحقل مطلوب.",
        [ApiErrorCodes.Invalid] = "قيمة غير صالحة.",
        [ApiErrorCodes.Empty] = "الملف فارغ أو لا يحتوي على بيانات.",
        [ApiErrorCodes.Duplicate] = "قيمة مكررة.",
        [ApiErrorCodes.InvalidRow] = "تنسيق الصف غير صحيح.",
        [ApiErrorCodes.Malformed] = "تنسيق البيانات غير صحيح.",
        [ApiErrorCodes.TrackNotAllowed] = "الكلية غير متاحة للشعبة المحددة.",
        [ApiErrorCodes.ScoreMustBePositive] = "يجب أن يكون الحد الأدنى أكبر من صفر.",
        [ApiErrorCodes.DuplicateRow] = "صف مكرر في الملف.",
        [ApiErrorCodes.CollegeContainsScore] = "عمود الكلية يحتوي على قيمة درجة.",
        [ApiErrorCodes.CutoffMustBeNumber] = "يجب أن يكون الحد الأدنى رقماً.",
        [ApiErrorCodes.FileNoDataRows] = "الملف لا يحتوي على صفوف بيانات.",
        [ApiErrorCodes.UnresolvedCollege] = "تعذر مطابقة \"{0}\" مع جامعة/كلية في النظام.",
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
