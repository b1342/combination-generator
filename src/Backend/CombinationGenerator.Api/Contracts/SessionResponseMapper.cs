using System.Globalization;
using CombinationGenerator.Application.Generators;
using CombinationGenerator.Application.Pagination;

namespace CombinationGenerator.Api.Contracts;

/// <summary>
/// מתרגם את התשובה של ה-Service (SessionView) ל-JSON שנשלח לדפדפן (SessionResponse).
/// כאן, ורק כאן, המספרים הגדולים הופכים לטקסט.
/// </summary>
internal static class SessionResponseMapper
{
    /// <summary>ההודעה שהמטלה דורשת כשאין יותר קומבינציות.</summary>
    private const string NoMorePermutationsMessage = "אין יותר קומבינציות";

    public static SessionResponse ToResponse(SessionView view, bool noMorePermutations) => new(
        SessionId: view.SessionId,
        N: view.N,
        TotalCount: AsText(view.TotalCount),
        Mode: view.Mode,
        Current: view.Current is null ? null : ToCombination(view.Current.Number, view.Current.Permutation),
        Page: view.Page is null ? null : ToPage(view.Page),
        NoMorePermutations: noMorePermutations,
        Message: noMorePermutations ? NoMorePermutationsMessage : null);

    private static PageResponse ToPage(PageView page) => new(
        PageNumber: AsText(page.PageNumber),
        TotalPages: AsText(page.TotalPages),
        PageSize: page.PageSize,
        Rows: page.Rows.Select(row => ToCombination(row.Number, row.Permutation)).ToList(),
        Links: page.Links.Select(ToLink).ToList());

    private static CombinationResponse ToCombination(long number, IReadOnlyList<int> permutation) =>
        new(AsText(number), permutation);

    private static PageLinkResponse ToLink(PageToken token) => token switch
    {
        PageNumberToken page => new PageLinkResponse("page", AsText(page.Number), page.IsCurrent),
        EllipsisToken => new PageLinkResponse("ellipsis", null, false),
        _ => throw new ArgumentOutOfRangeException(nameof(token), token, "Unknown page token."),
    };

    // InvariantCulture: בלי מפרידי אלפים ובלי תלות בהגדרות האזוריות של השרת
    private static string AsText(long value) => value.ToString(CultureInfo.InvariantCulture);
}
