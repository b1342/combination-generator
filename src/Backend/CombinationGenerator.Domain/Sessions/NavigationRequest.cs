namespace CombinationGenerator.Domain.Sessions;

/// <summary>כפתורי הדפדוף.</summary>
public enum PageNavigation
{
    First,
    Previous,
    Next,
    Last,
    Specific,
}

/// <summary>
/// בקשת דפדוף: איזה כפתור, גודל העמוד, ומספר דף (רק עבור Specific).
/// </summary>
public readonly record struct NavigationRequest(PageNavigation Action, int PageSize, long? PageNumber = null);
