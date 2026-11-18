using CombinationGenerator.Application.Pagination;
using CombinationGenerator.Application.Sessions;
using CombinationGenerator.Domain.Permutations;
using CombinationGenerator.Domain.Sessions;

namespace CombinationGenerator.Application.Generators;

/// <summary>
/// מחבר את כל החלקים: לכל כפתור — קוראים מה-Store, מפעילים את חוקי ה-Domain, שומרים, ובונים תשובה למסך.
/// לא יודע מה זה HTTP; את זה מוסיף ה-API.
/// </summary>
public sealed class GeneratorService
{
    private readonly ISessionStore _store;
    private readonly GeneratorOptions _options;

    public GeneratorService(ISessionStore store, GeneratorOptions options)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(options);

        if (options.MaxPageSize < 1)
        {
            throw new ArgumentException("MaxPageSize must be positive.", nameof(options));
        }

        _store = store;
        _options = options;
    }

    // ---------- הכפתורים ----------

    /// <summary>"התחל": Session חדש עם n.</summary>
    public async Task<OperationResult> StartAsync(int n, CancellationToken cancellationToken = default)
    {
        if (n < 1 || n > FactorialCalculator.MaxN)
        {
            return OperationResult.Fail(OperationStatus.InvalidN);
        }

        CreateResult created = await _store.CreateAsync(GeneratorSession.Create(n), cancellationToken);

        return created is { Status: CreateStatus.Created, Stored: { } stored }
            ? OperationResult.Success(BuildView(stored))
            : OperationResult.Fail(OperationStatus.CapacityReached);
    }

    /// <summary>המצב הנוכחי, בלי לשנות כלום. משמש אחרי רענון דף, ואחרי Conflict.</summary>
    public async Task<OperationResult> GetStateAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        StoredSession? stored = await _store.GetAsync(sessionId, cancellationToken);

        return stored is null
            ? OperationResult.Fail(OperationStatus.SessionNotFound)
            : OperationResult.Success(BuildView(stored));
    }

    /// <summary>"הבא".</summary>
    public Task<OperationResult> NextAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        ApplyAsync(sessionId, session => session.AdvanceNext(), cancellationToken);

    /// <summary>"כל הקומבינציות", כל כפתורי הדפדוף ושינוי גודל העמוד.</summary>
    public Task<OperationResult> ShowAllAsync(
        Guid sessionId,
        PageNavigation action,
        int pageSize,
        long? pageNumber = null,
        CancellationToken cancellationToken = default)
    {
        // הגבול העליון הוא הגדרה של השרת, ולכן נבדק כאן ולא ב-Domain
        if (pageSize < 1 || pageSize > _options.MaxPageSize)
        {
            return Task.FromResult(OperationResult.Fail(OperationStatus.InvalidPageSize));
        }

        var request = new NavigationRequest(action, pageSize, pageNumber);
        return ApplyAsync(sessionId, session => session.ShowAll(request), cancellationToken);
    }

    /// <summary>"חזור".</summary>
    public Task<OperationResult> BackAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        ApplyAsync(sessionId, session => session.BackToNext(), cancellationToken);

    /// <summary>"איפוס": מוחק את ה-Session. אין View, כי אין יותר Session.</summary>
    public async Task<OperationResult> ResetAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        bool removed = await _store.RemoveAsync(sessionId, cancellationToken);

        return removed
            ? OperationResult.Success(null)
            : OperationResult.Fail(OperationStatus.SessionNotFound);
    }

    // ---------- ארבעת השלבים הקבועים ----------

    /// <summary>
    /// קוראים ← מחשבים ← שומרים ← בונים תשובה. כל הכפתורים שמשנים מצב עוברים כאן.
    /// </summary>
    private async Task<OperationResult> ApplyAsync(
        Guid sessionId,
        Func<GeneratorSession, DomainTransition> action,
        CancellationToken cancellationToken)
    {
        // 1. קוראים
        StoredSession? stored = await _store.GetAsync(sessionId, cancellationToken);
        if (stored is null)
        {
            return OperationResult.Fail(OperationStatus.SessionNotFound);
        }

        // 2. מחשבים (Domain)
        DomainTransition transition = action(stored.Session);

        switch (transition)
        {
            case DomainTransition.Completed:
                // אין יותר: שום דבר לא נשמר, מחזירים את המצב כמו שהוא
                return OperationResult.NoMore(BuildView(stored));

            case DomainTransition.Rejected rejected:
                return OperationResult.Fail(ToStatus(rejected.Error));

            case DomainTransition.Changed changed:
                // 3. שומרים — רק אם אף אחד לא שינה בינתיים
                ReplaceResult replaced = await _store.ReplaceAsync(sessionId, stored.Version, changed.Session, cancellationToken);

                return replaced switch
                {
                    // 4. בונים תשובה
                    { Status: ReplaceStatus.Replaced, Stored: { } saved } => OperationResult.Success(BuildView(saved)),

                    // לא מנסים שוב אוטומטית: בלחיצה כפולה זה היה מקדם פעמיים
                    { Status: ReplaceStatus.VersionConflict } => OperationResult.Fail(OperationStatus.Conflict),

                    { Status: ReplaceStatus.NotFound } => OperationResult.Fail(OperationStatus.SessionNotFound),

                    _ => throw new InvalidOperationException($"Unexpected replace status {replaced.Status}."),
                };

            default:
                throw new InvalidOperationException("Unknown domain transition.");
        }
    }

    // ---------- בניית התשובה למסך ----------

    private static SessionView BuildView(StoredSession stored)
    {
        GeneratorSession session = stored.Session;

        return session.Progress switch
        {
            NextProgress next => new SessionView(
                stored.Id,
                session.N,
                session.TotalCount,
                SessionMode.Next,
                Current: next.Cursor is null
                    ? null
                    : new CurrentCombination(next.Cursor.Index + 1, next.Cursor.Permutation),
                Page: null),

            AllProgress all => new SessionView(
                stored.Id,
                session.N,
                session.TotalCount,
                SessionMode.All,
                Current: null,
                Page: BuildPage(session, all)),

            _ => throw new InvalidOperationException("Unknown progress type."),
        };
    }

    private static PageView BuildPage(GeneratorSession session, AllProgress all)
    {
        var window = session.CurrentPage
            ?? throw new InvalidOperationException("A session in All mode must have a current page.");

        // כאן, ורק כאן, נוצרות השורות: Unrank לשורה הראשונה ו-NextPermutation לשאר
        IReadOnlyList<int[]> permutations = PermutationPageGenerator.Generate(session.N, window);

        var rows = new List<PageRow>(permutations.Count);
        for (int i = 0; i < permutations.Count; i++)
        {
            rows.Add(new PageRow(window.StartIndex + i + 1, permutations[i]));
        }

        return new PageView(
            window.PageNumber,
            window.TotalPages,
            all.PageSize,
            rows,
            PageLinksBuilder.Build(window.PageNumber, window.TotalPages));
    }

    private static OperationStatus ToStatus(DomainError error) => error switch
    {
        DomainError.InvalidMode => OperationStatus.InvalidMode,
        DomainError.AllEntryRequiresFirst => OperationStatus.AllEntryRequiresFirst,
        DomainError.InvalidPageSize => OperationStatus.InvalidPageSize,
        DomainError.NoPreviousPage => OperationStatus.NoPreviousPage,
        DomainError.NoNextPage => OperationStatus.NoNextPage,
        DomainError.InvalidPageNumber => OperationStatus.InvalidPageNumber,
        _ => throw new ArgumentOutOfRangeException(nameof(error), error, "Unknown domain error."),
    };
}
