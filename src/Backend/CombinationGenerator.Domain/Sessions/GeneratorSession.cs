using CombinationGenerator.Domain.Pagination;
using CombinationGenerator.Domain.Permutations;

namespace CombinationGenerator.Domain.Sessions;

/// <summary>
/// המצב של משתמש אחד וחוקי המעבר בין המצבים.
/// אובייקט שלא משתנה: כל פעולה מחזירה Session חדש (בתוך DomainTransition.Changed), והישן נשאר כמו שהיה.
/// לא יודע מה זה HTTP, אחסון, זמן או מזהה Session; את אלה מוסיפות השכבות שמעליו.
/// </summary>
public sealed class GeneratorSession
{
    private GeneratorSession(int n, Progress progress)
    {
        N = n;
        Progress = progress;
    }

    /// <summary>כמה מספרים מסדרים. לא משתנה לאורך חיי ה-Session.</summary>
    public int N { get; }

    /// <summary>המצב הנוכחי: NextProgress או AllProgress.</summary>
    public Progress Progress { get; }

    /// <summary>סך כל הקומבינציות (n!). מחושב מ-N ולא נשמר.</summary>
    public long TotalCount => FactorialCalculator.Of(N);

    /// <summary>
    /// העמוד הנוכחי במצב "כל הקומבינציות", או null במצב "הבא".
    /// </summary>
    public PageWindow? CurrentPage
    {
        get
        {
            if (Progress is not AllProgress all)
            {
                return null;
            }

            if (!PageCalculator.TryGetWindow(TotalCount, all.StartIndex, all.PageSize, all.PageNumber, out var window))
            {
                // לא אמור לקרות: AllProgress נוצר רק דרך המעברים, שבודקים את הטווח
                throw new InvalidOperationException("The stored page is outside the valid range.");
            }

            return window;
        }
    }

    // ---------- Start ----------

    /// <summary>Start: Session חדש במצב "הבא", לפני הקומבינציה הראשונה.</summary>
    public static GeneratorSession Create(int n)
    {
        if (n < 1 || n > FactorialCalculator.MaxN)
        {
            throw new ArgumentOutOfRangeException(nameof(n), n, $"n must be between 1 and {FactorialCalculator.MaxN}.");
        }

        return new GeneratorSession(n, new NextProgress(Cursor: null));
    }

    // ---------- "הבא" ----------

    /// <summary>
    /// "הבא": בפעם הראשונה [1..N]; אחר כך NextPermutation; בסוף Completed.
    /// </summary>
    public DomainTransition AdvanceNext()
    {
        if (Progress is not NextProgress next)
        {
            return Reject(DomainError.InvalidMode);
        }

        if (next.Cursor is null)
        {
            int[] first = Enumerable.Range(1, N).ToArray();
            return ChangeTo(new NextProgress(Cursor.Create(0, first)));
        }

        // עותק עבודה: NextPermutation משנה את המערך, והמצב השמור חייב להישאר כמו שהוא
        int[] working = next.Cursor.Permutation.ToArray();

        if (!NextPermutation.TryAdvance(working))
        {
            return new DomainTransition.Completed();
        }

        return ChangeTo(new NextProgress(Cursor.Create(next.Cursor.Index + 1, working)));
    }

    // ---------- "כל הקומבינציות" ----------

    /// <summary>
    /// כפתור "כל הקומבינציות" וכל כפתורי הדפדוף.
    /// ממצב "הבא": כניסה (רק עם First). ממצב "כל הקומבינציות": מעבר עמוד.
    /// </summary>
    public DomainTransition ShowAll(NavigationRequest request)
    {
        if (request.PageSize < 1)
        {
            return Reject(DomainError.InvalidPageSize);
        }

        return Progress switch
        {
            NextProgress next => EnterAll(next, request),
            AllProgress all => NavigateAll(all, request),
            _ => throw new InvalidOperationException("Unknown progress type."),
        };
    }

    private DomainTransition EnterAll(NextProgress next, NavigationRequest request)
    {
        if (request.Action != PageNavigation.First)
        {
            return Reject(DomainError.AllEntryRequiresFirst);
        }

        // מתחילים מהקומבינציה שאחרי האחרונה שהוצגה, או מההתחלה אם עוד לא הוצגה אף אחת
        long startIndex = next.Cursor is null ? 0 : next.Cursor.Index + 1;

        if (startIndex >= TotalCount)
        {
            // לא נשאר כלום להציג: נשארים ב"הבא" ולא יוצרים עמוד ריק
            return new DomainTransition.Completed();
        }

        return ChangeTo(new AllProgress(startIndex, request.PageSize, PageNumber: 1));
    }

    private DomainTransition NavigateAll(AllProgress all, NavigationRequest request)
    {
        if (request.PageSize != all.PageSize)
        {
            return Reject(DomainError.PageSizeLocked);
        }

        long totalPages = PageCalculator.CountPages(TotalCount, all.StartIndex, all.PageSize);
        long current = all.PageNumber;
        long target;

        switch (request.Action)
        {
            case PageNavigation.First:
                target = 1;
                break;

            case PageNavigation.Last:
                target = totalPages;
                break;

            case PageNavigation.Previous:
                if (current <= 1)
                {
                    return Reject(DomainError.NoPreviousPage);
                }

                target = current - 1;
                break;

            case PageNavigation.Next:
                if (current >= totalPages)
                {
                    return Reject(DomainError.NoNextPage);
                }

                target = current + 1;
                break;

            case PageNavigation.Specific:
                if (request.PageNumber is not long requested || requested < 1 || requested > totalPages)
                {
                    return Reject(DomainError.InvalidPageNumber);
                }

                target = requested;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(request), request.Action, "Unknown navigation action.");
        }

        // העוגן וגודל העמוד לא משתנים; רק מספר העמוד
        return ChangeTo(all with { PageNumber = target });
    }

    // ---------- "חזור" ----------

    /// <summary>
    /// "חזור": חוזרים ל"הבא" עם השורה האחרונה של העמוד הנוכחי.
    /// </summary>
    public DomainTransition BackToNext()
    {
        if (CurrentPage is not PageWindow page)
        {
            return Reject(DomainError.InvalidMode);
        }

        long lastIndex = page.LastIndex;
        int[] permutation = PermutationUnranker.Unrank(N, lastIndex);

        return ChangeTo(new NextProgress(Cursor.Create(lastIndex, permutation)));
    }

    // ---------- עזרים ----------

    private DomainTransition ChangeTo(Progress progress) =>
        new DomainTransition.Changed(new GeneratorSession(N, progress));

    private static DomainTransition Reject(DomainError error) =>
        new DomainTransition.Rejected(error);
}
