namespace CombinationGenerator.Domain.Sessions;

/// <summary>כפתורי הדפדוף.</summary>
public enum PageNavigation
{
    First,
    Previous,
    Next,
    Last,
    Specific,

    /// <summary>נשארים בעמוד הנוכחי. משמש בעיקר לשינוי גודל העמוד בלי לזוז.</summary>
    Current,
}

/// <summary>
/// בקשת דפדוף: איזה כפתור, גודל העמוד, ומספר דף (רק עבור Specific).
/// גודל העמוד נשלח בכל בקשה, ואפשר לשנות אותו גם בתוך "כל הקומבינציות".
/// </summary>
public readonly record struct NavigationRequest(PageNavigation Action, int PageSize, long? PageNumber = null);
