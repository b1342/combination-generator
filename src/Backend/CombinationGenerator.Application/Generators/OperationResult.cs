namespace CombinationGenerator.Application.Generators;

/// <summary>
/// מה קרה בפעולה. Success ו-NoMorePermutations מגיעים עם View; כל השאר הם שגיאות, בלי View.
/// </summary>
public enum OperationStatus
{
    /// <summary>הפעולה הצליחה. View הוא המצב החדש.</summary>
    Success,

    /// <summary>אין יותר קומבינציות. View הוא המצב הנוכחי, שלא השתנה.</summary>
    NoMorePermutations,

    /// <summary>n מחוץ לטווח 1..20.</summary>
    InvalidN,

    /// <summary>גודל עמוד מחוץ לטווח 1..MaxPageSize.</summary>
    InvalidPageSize,

    /// <summary>אין Session עם המזהה הזה: לא היה, נמחק או פג.</summary>
    SessionNotFound,

    /// <summary>בקשה אחרת שינתה את ה-Session בין הקריאה לשמירה. הלקוח צריך לטעון את המצב מחדש.</summary>
    Conflict,

    /// <summary>הגענו לתקרת ה-Sessions.</summary>
    CapacityReached,

    /// <summary>הפעולה שייכת למסך השני.</summary>
    InvalidMode,

    /// <summary>כניסה ל"כל הקומבינציות" מותרת רק עם "דף ראשון".</summary>
    AllEntryRequiresFirst,

    /// <summary>"דף קודם" בעמוד הראשון.</summary>
    NoPreviousPage,

    /// <summary>"דף הבא" בעמוד האחרון.</summary>
    NoNextPage,

    /// <summary>מספר דף שלא קיים.</summary>
    InvalidPageNumber,
}

/// <summary>התוצאה של כל פעולה ב-Service.</summary>
public sealed record OperationResult(OperationStatus Status, SessionView? View)
{
    internal static OperationResult Success(SessionView? view) => new(OperationStatus.Success, view);

    internal static OperationResult NoMore(SessionView view) => new(OperationStatus.NoMorePermutations, view);

    internal static OperationResult Fail(OperationStatus status) => new(status, null);
}
