using CombinationGenerator.Application.Pagination;

namespace CombinationGenerator.Application.Generators;

/// <summary>באיזה מסך המשתמשת נמצאת.</summary>
public enum SessionMode
{
    /// <summary>מסך א': קומבינציה אחת וכפתור "הבא".</summary>
    Next,

    /// <summary>מסך ב': טבלה של "כל הקומבינציות".</summary>
    All,
}

/// <summary>
/// מה שהמסך צריך להציג. נבנה מחדש מהסימנייה בכל תשובה, ולא נשמר אף פעם.
/// במסך א' יש Current (או null לפני ה"הבא" הראשון); במסך ב' יש Page.
/// </summary>
public sealed record SessionView(
    Guid SessionId,
    int N,
    long TotalCount,
    SessionMode Mode,
    CurrentCombination? Current,
    PageView? Page);

/// <summary>הקומבינציה שמוצגת במסך א'. Number מתחיל מ-1, כמו שהמשתמשת רואה.</summary>
public sealed record CurrentCombination(long Number, IReadOnlyList<int> Permutation);

/// <summary>העמוד שמוצג במסך ב': איזה עמוד, השורות, ושורת הכפתורים.</summary>
public sealed record PageView(
    long PageNumber,
    long TotalPages,
    int PageSize,
    IReadOnlyList<PageRow> Rows,
    IReadOnlyList<PageToken> Links);

/// <summary>שורה אחת בטבלה. Number מתחיל מ-1.</summary>
public sealed record PageRow(long Number, IReadOnlyList<int> Permutation);
