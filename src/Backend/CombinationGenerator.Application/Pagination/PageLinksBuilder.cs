namespace CombinationGenerator.Application.Pagination;

/// <summary>
/// מדיניות התצוגה של מספרי הדפים: אילו מספרים מציגים ומתי מציגים "…".
/// זו החלטת תצוגה ולא חוק של המערכת, ולכן היא כאן ולא ב-Domain.
/// החישוב נשאר בשרת כדי שהלקוח לא יחשב בעצמו מספרים שגדולים מהטווח המדויק של JavaScript.
/// </summary>
public static class PageLinksBuilder
{
    /// <summary>כמה דפים להציג מכל צד של הדף הנוכחי.</summary>
    private const int NeighborPages = 2;

    /// <summary>
    /// בונה את רשימת מספרי הדפים להצגה: תמיד הראשון והאחרון, ושני דפים מכל צד של הנוכחי.
    /// פער של דף בודד מוצג כמספר; פער של שניים ומעלה מוצג כ-"…".
    /// </summary>
    /// <example>דף 50 מתוך 100: 1 … 48 49 [50] 51 52 … 100</example>
    public static IReadOnlyList<PageToken> Build(long currentPage, long totalPages)
    {
        if (totalPages < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(totalPages), totalPages, "totalPages must be at least 1.");
        }

        if (currentPage < 1 || currentPage > totalPages)
        {
            throw new ArgumentOutOfRangeException(nameof(currentPage), currentPage, "currentPage must be between 1 and totalPages.");
        }

        // 1. אוספים את הדפים שחייבים להופיע. SortedSet ממיין ומונע כפילויות.
        var pages = new SortedSet<long> { 1, totalPages };

        // הגבולות מחושבים כך שאין חיבור שעלול לעבור את long.MaxValue
        long from = Math.Max(1, currentPage - NeighborPages);
        long to = currentPage > totalPages - NeighborPages ? totalPages : currentPage + NeighborPages;
        for (long page = from; page <= to; page++)
        {
            pages.Add(page);
            if (page == to)
            {
                break; // בלי זה, page++ אחרי long.MaxValue היה גולש למספר שלילי
            }
        }

        // 2. עוברים עליהם לפי הסדר ומשלימים את הפערים
        var tokens = new List<PageToken>();
        long? previous = null;

        foreach (long page in pages)
        {
            if (previous is long prev)
            {
                long gap = page - prev;
                if (gap == 2)
                {
                    // חסר דף אחד בלבד: עדיף להציג אותו מאשר "…"
                    tokens.Add(new PageNumberToken(prev + 1, IsCurrent: false));
                }
                else if (gap > 2)
                {
                    tokens.Add(new EllipsisToken());
                }
            }

            tokens.Add(new PageNumberToken(page, IsCurrent: page == currentPage));
            previous = page;
        }

        return tokens;
    }
}
