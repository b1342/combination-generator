namespace CombinationGenerator.Domain.Sessions;

/// <summary>
/// התוצאה של כל פעולה על ה-Session. תמיד אחת משלוש:
/// Changed — יש מצב חדש; Completed — אין יותר קומבינציות, המצב לא משתנה; Rejected — הפעולה לא חוקית.
/// </summary>
public abstract record DomainTransition
{
    // בנאי פרטי: רק שלושת הסוגים שבתוך המחלקה יכולים לרשת ממנה
    private DomainTransition()
    {
    }

    /// <summary>הפעולה הצליחה. Session הוא המצב החדש; הישן לא השתנה.</summary>
    public sealed record Changed(GeneratorSession Session) : DomainTransition;

    /// <summary>אין יותר קומבינציות. זו תוצאה תקינה, לא שגיאה.</summary>
    public sealed record Completed : DomainTransition;

    /// <summary>הפעולה לא חוקית במצב הנוכחי.</summary>
    public sealed record Rejected(DomainError Error) : DomainTransition;
}

/// <summary>הסיבות שבגללן פעולה יכולה להידחות.</summary>
public enum DomainError
{
    /// <summary>הפעולה לא מתאימה למצב: "הבא" בזמן "כל הקומבינציות", או "חזור" בזמן "הבא".</summary>
    InvalidMode,

    /// <summary>כניסה ל"כל הקומבינציות" מותרת רק דרך "דף ראשון".</summary>
    AllEntryRequiresFirst,

    /// <summary>גודל עמוד קטן מ-1.</summary>
    InvalidPageSize,

    /// <summary>"דף קודם" כשכבר בעמוד הראשון.</summary>
    NoPreviousPage,

    /// <summary>"דף הבא" כשכבר בעמוד האחרון.</summary>
    NoNextPage,

    /// <summary>מספר דף שלא קיים.</summary>
    InvalidPageNumber,

    /// <summary>"הקודם" על הקומבינציה הראשונה, או לפני שהוצגה אף אחת.</summary>
    NoPreviousCombination,
}
