namespace CombinationGenerator.Api.Contracts;

// השדות כאן nullable בכוונה: כך אפשר להבדיל בין "שדה חסר" לבין "0".
// בלי זה, { } היה מתפרש בשקט כ-n = 0, והמשתמשת הייתה מקבלת "n לא בטווח" במקום "הבקשה לא תקינה".

/// <summary>גוף הבקשה של "התחל" (StartAPI). דוגמה: { "n": 3 }</summary>
public sealed record StartRequest(int? N);

/// <summary>
/// גוף הבקשה של "כל הקומבינציות" ושל כפתורי הדפדוף (GetAllAPI).
/// דוגמה: { "action": "Next", "pageSize": 20 }
/// </summary>
/// <param name="Action">איזה כפתור. חובה.</param>
/// <param name="PageSize">כמה שורות בעמוד. חובה: המטלה דורשת לשלוח אותו בכל קריאה.</param>
/// <param name="PageNumber">
/// מספר דף. רק עבור Specific, ובו הוא חובה. נשלח כטקסט, כי מספרי דפים יכולים לעבור את הטווח המדויק של JavaScript.
/// </param>
public sealed record ShowAllRequest(PageAction? Action, int? PageSize, string? PageNumber = null);

/// <summary>
/// הכפתורים של "כל הקומבינציות", כפי שהם נראים מבחוץ (ב-JSON).
/// enum נפרד מזה של ה-Domain בכוונה: שינוי שם בתוך המערכת לא ישבור את הלקוחות, וההמרה ביניהם נעשית ב-Controller.
/// </summary>
public enum PageAction
{
    /// <summary>הדף הראשון. גם הכניסה ל"כל הקומבינציות".</summary>
    First,

    /// <summary>הדף הקודם.</summary>
    Previous,

    /// <summary>הדף הבא.</summary>
    Next,

    /// <summary>הדף האחרון.</summary>
    Last,

    /// <summary>דף לפי מספר (pageNumber).</summary>
    Specific,

    /// <summary>אותו דף, למשל אחרי שינוי גודל עמוד.</summary>
    Current,
}
