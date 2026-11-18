using CombinationGenerator.Domain.Sessions;

namespace CombinationGenerator.Api.Contracts;

/// <summary>גוף הבקשה של "התחל" (StartAPI). דוגמה: { "n": 3 }</summary>
public sealed record StartRequest(int N);

/// <summary>
/// גוף הבקשה של "כל הקומבינציות" ושל כפתורי הדפדוף (GetAllAPI).
/// דוגמה: { "action": "Next", "pageSize": 20 }
/// </summary>
/// <param name="Action">איזה כפתור: First, Previous, Next, Last, Specific או Current.</param>
/// <param name="PageSize">כמה שורות בעמוד. המטלה דורשת לשלוח אותו בכל קריאה.</param>
/// <param name="PageNumber">
/// מספר דף, רק עבור Specific. נשלח כטקסט, כי מספרי דפים יכולים לעבור את הטווח המדויק של JavaScript.
/// </param>
public sealed record ShowAllRequest(PageNavigation Action, int PageSize, string? PageNumber = null);
