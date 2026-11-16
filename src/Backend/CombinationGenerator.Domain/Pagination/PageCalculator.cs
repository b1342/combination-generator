namespace CombinationGenerator.Domain.Pagination;

/// <summary>
/// חוקי הדפדוף: כמה עמודים יש ומאיפה כל עמוד מתחיל.
/// לא יוצר תמורות ולא יודע מה זה HTTP או קונפיגורציה; מקבל מספרים ומחזיר מספרים.
/// </summary>
public static class PageCalculator
{
    /// <summary>
    /// כמה עמודים יש מהעוגן ועד הסוף.
    /// </summary>
    /// <param name="totalCount">סך כל התמורות (n!).</param>
    /// <param name="anchorIndex">האינדקס שממנו מתחיל העמוד הראשון (מ-0).</param>
    /// <param name="pageSize">כמה שורות בעמוד.</param>
    public static long CountPages(long totalCount, long anchorIndex, int pageSize)
    {
        ValidateRange(totalCount, anchorIndex, pageSize);

        long remaining = totalCount - anchorIndex;

        // חילוק שלם עם עיגול למעלה, בלי double
        return remaining / pageSize + (remaining % pageSize == 0 ? 0 : 1);
    }

    /// <summary>
    /// מחשב את העמוד המבוקש. מחזיר false אם אין עמוד כזה (קטן מ-1 או גדול ממספר העמודים).
    /// </summary>
    public static bool TryGetWindow(long totalCount, long anchorIndex, int pageSize, long pageNumber, out PageWindow window)
    {
        long totalPages = CountPages(totalCount, anchorIndex, pageSize);

        // בודקים את הטווח לפני ההכפלה, כדי שמספר עמוד ענק לא יגרום לגלישה
        if (pageNumber < 1 || pageNumber > totalPages)
        {
            window = default;
            return false;
        }

        // checked: הגנה נוספת בלבד. אחרי הבדיקה למעלה זה תמיד קטן מ-totalCount
        long startIndex = checked(anchorIndex + (pageNumber - 1) * pageSize);

        // העמוד האחרון יכול להיות קצר מ-pageSize
        int count = (int)Math.Min(pageSize, totalCount - startIndex);

        window = new PageWindow(pageNumber, totalPages, startIndex, count);
        return true;
    }

    private static void ValidateRange(long totalCount, long anchorIndex, int pageSize)
    {
        if (totalCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(totalCount), totalCount, "totalCount must be at least 1.");
        }

        // העוגן חייב להיות בתוך הרצף, כלומר תמיד נשארה לפחות תמורה אחת
        if (anchorIndex < 0 || anchorIndex >= totalCount)
        {
            throw new ArgumentOutOfRangeException(nameof(anchorIndex), anchorIndex, "anchorIndex must be between 0 and totalCount - 1.");
        }

        if (pageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "pageSize must be at least 1.");
        }
    }
}
