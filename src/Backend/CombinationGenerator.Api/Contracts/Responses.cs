using CombinationGenerator.Application.Generators;

namespace CombinationGenerator.Api.Contracts;

// כל המספרים שיכולים להיות גדולים (עד 20!) נשלחים כטקסט (string):
// JavaScript מדייק במספרים רק עד 2^53, ומעבר לזה מעגל בשקט.
// מספרים קטנים (n, גודל עמוד, הספרות בתמורה) נשארים מספרים רגילים.

/// <summary>
/// התשובה של כל הכפתורים: מה המסך צריך להציג.
/// במסך "הבא" (Mode = Next) יש Current; במסך הטבלה (Mode = All) יש Page.
/// </summary>
public sealed record SessionResponse(
    Guid SessionId,
    int N,
    string TotalCount,
    SessionMode Mode,
    CombinationResponse? Current,
    PageResponse? Page,
    bool NoMorePermutations,
    string? Message);

/// <summary>קומבינציה אחת: המספר שלה (מ-1) והתמורה.</summary>
public sealed record CombinationResponse(string Number, IReadOnlyList<int> Permutation);

/// <summary>עמוד בטבלה: איזה עמוד, השורות, ושורת הכפתורים.</summary>
public sealed record PageResponse(
    string PageNumber,
    string TotalPages,
    int PageSize,
    IReadOnlyList<CombinationResponse> Rows,
    IReadOnlyList<PageLinkResponse> Links);

/// <summary>
/// כפתור אחד בשורת מספרי הדפים. Type הוא "page" (מספר דף לחיץ) או "ellipsis" ("…").
/// </summary>
public sealed record PageLinkResponse(string Type, string? Number, bool IsCurrent);
