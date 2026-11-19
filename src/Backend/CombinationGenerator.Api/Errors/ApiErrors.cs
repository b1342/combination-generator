using CombinationGenerator.Application.Generators;

namespace CombinationGenerator.Api.Errors;

/// <summary>
/// שגיאה כפי שהיא נשלחת לדפדפן: קוד HTTP, קוד שגיאה קבוע שהלקוח יכול לבדוק, כותרת והודעה למשתמש.
/// </summary>
internal sealed record ApiError(int StatusCode, string Code, string Title, string Message);

/// <summary>
/// הטבלה שמתרגמת כל כישלון של ה-Service לשגיאת HTTP.
/// 400 = הקלט לא תקין, 404 = אין Session, 409 = מתנגש במצב הנוכחי, 503 = השרת מלא.
/// </summary>
internal static class ApiErrors
{
    /// <summary>
    /// שגיאה שה-Controller מגלה בעצמו, לפני שהבקשה מגיעה ל-Service: action חסר או לא מוכר.
    /// </summary>
    public static readonly ApiError InvalidAction =
        new(StatusCodes.Status400BadRequest, "INVALID_ACTION", "Invalid action", "הפעולה חייבת להיות אחת מ: First, Previous, Next, Last, Specific, Current.");

    public static ApiError For(OperationStatus status) => status switch
    {
        // ---------- 400: הקלט לא תקין ----------
        OperationStatus.InvalidN =>
            new(StatusCodes.Status400BadRequest, "INVALID_N", "Invalid n", "n חייב להיות מספר שלם בין 1 ל-20."),

        OperationStatus.InvalidPageSize =>
            new(StatusCodes.Status400BadRequest, "INVALID_PAGE_SIZE", "Invalid page size", "גודל העמוד לא בטווח המותר."),

        OperationStatus.InvalidPageNumber =>
            new(StatusCodes.Status400BadRequest, "INVALID_PAGE_NUMBER", "Invalid page number", "מספר הדף לא קיים."),

        // ---------- 404: אין Session ----------
        OperationStatus.SessionNotFound =>
            new(StatusCodes.Status404NotFound, "SESSION_NOT_FOUND", "Session not found", "ה-Session לא קיים או שפג תוקפו. יש להתחיל מחדש."),

        // ---------- 409: מתנגש במצב הנוכחי ----------
        OperationStatus.Conflict =>
            new(StatusCodes.Status409Conflict, "SESSION_CONFLICT", "Session changed", "המצב השתנה בבקשה אחרת. יש לטעון את המצב העדכני."),

        OperationStatus.InvalidMode =>
            new(StatusCodes.Status409Conflict, "INVALID_MODE", "Invalid mode", "הפעולה לא מתאימה למסך הנוכחי."),

        OperationStatus.AllEntryRequiresFirst =>
            new(StatusCodes.Status409Conflict, "ALL_ENTRY_REQUIRES_FIRST", "Must start at first page", "הכניסה ל'כל הקומבינציות' היא תמיד לדף הראשון."),

        OperationStatus.NoPreviousPage =>
            new(StatusCodes.Status409Conflict, "NO_PREVIOUS_PAGE", "No previous page", "זה כבר הדף הראשון."),

        OperationStatus.NoPreviousCombination =>
            new(StatusCodes.Status409Conflict, "NO_PREVIOUS_COMBINATION", "No previous combination", "זו כבר הקומבינציה הראשונה."),

        OperationStatus.NoNextPage =>
            new(StatusCodes.Status409Conflict, "NO_NEXT_PAGE", "No next page", "זה כבר הדף האחרון."),

        // ---------- 503: השרת מלא ----------
        OperationStatus.CapacityReached =>
            new(StatusCodes.Status503ServiceUnavailable, "CAPACITY_REACHED", "Server is busy", "השרת עמוס כרגע. נסו שוב בעוד כמה דקות."),

        // Success ו-NoMorePermutations הם לא שגיאות, ולא אמורים להגיע לכאן
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Not an error status."),
    };
}
