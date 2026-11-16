using CombinationGenerator.Domain.Permutations;
using CombinationGenerator.Domain.Sessions;

namespace CombinationGenerator.Domain.Tests.Sessions;

public class GeneratorSessionTests
{
    // ---------- עזרים ----------

    private static GeneratorSession Changed(DomainTransition result) =>
        Assert.IsType<DomainTransition.Changed>(result).Session;

    private static DomainError Rejected(DomainTransition result) =>
        Assert.IsType<DomainTransition.Rejected>(result).Error;

    private static Cursor CursorOf(GeneratorSession session)
    {
        var cursor = Assert.IsType<NextProgress>(session.Progress).Cursor;
        Assert.NotNull(cursor);
        return cursor;
    }

    private static AllProgress AllOf(GeneratorSession session) =>
        Assert.IsType<AllProgress>(session.Progress);

    private static GeneratorSession AfterNext(GeneratorSession session, int times)
    {
        for (int i = 0; i < times; i++)
        {
            session = Changed(session.AdvanceNext());
        }

        return session;
    }

    private static NavigationRequest Nav(PageNavigation action, int size, long? page = null) =>
        new(action, size, page);

    // ---------- Start ----------

    [Fact]
    public void Create_StartsInNextMode_WithoutCursor()
    {
        var session = GeneratorSession.Create(3);

        Assert.Equal(3, session.N);
        Assert.Equal(6, session.TotalCount);
        Assert.Null(Assert.IsType<NextProgress>(session.Progress).Cursor);
        Assert.Null(session.CurrentPage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void Create_OutsideRange_Throws(int n)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GeneratorSession.Create(n));
    }

    // ---------- "הבא" ----------

    [Fact]
    public void Next_N3_ProducesAssignmentSequence_ThenCompleted()
    {
        int[][] expected = [[1, 2, 3], [1, 3, 2], [2, 1, 3], [2, 3, 1], [3, 1, 2], [3, 2, 1]];
        var session = GeneratorSession.Create(3);

        for (int i = 0; i < expected.Length; i++)
        {
            session = Changed(session.AdvanceNext());
            Assert.Equal(i, CursorOf(session).Index);
            Assert.Equal(expected[i], CursorOf(session).Permutation);
        }

        Assert.IsType<DomainTransition.Completed>(session.AdvanceNext());
    }

    [Fact]
    public void Transition_DoesNotModifyOriginalSession()
    {
        var before = AfterNext(GeneratorSession.Create(3), 1);

        var after = Changed(before.AdvanceNext());

        Assert.Equal(0, CursorOf(before).Index);
        Assert.Equal(new[] { 1, 2, 3 }, CursorOf(before).Permutation);
        Assert.Equal(1, CursorOf(after).Index);
    }

    [Fact]
    public void Next_InAllMode_IsRejected()
    {
        var all = Changed(GeneratorSession.Create(3).ShowAll(Nav(PageNavigation.First, 2)));

        Assert.Equal(DomainError.InvalidMode, Rejected(all.AdvanceNext()));
    }

    // ---------- כניסה ל"כל הקומבינציות" ----------

    [Fact]
    public void EnterAll_BeforeAnyNext_StartsAtIndexZero()
    {
        var all = Changed(GeneratorSession.Create(3).ShowAll(Nav(PageNavigation.First, 2)));

        Assert.Equal(new AllProgress(StartIndex: 0, PageSize: 2, PageNumber: 1), AllOf(all));
    }

    [Fact]
    public void EnterAll_AfterTwoNext_StartsAfterLastShown()
    {
        // הדוגמה מהמטלה בקטן: מוצגת 2, ולכן מתחילים ב-3 (אינדקס 2)
        var session = AfterNext(GeneratorSession.Create(3), 2);

        var all = Changed(session.ShowAll(Nav(PageNavigation.First, 2)));

        Assert.Equal(new AllProgress(2, 2, 1), AllOf(all));
        Assert.Equal(2, all.CurrentPage!.Value.StartIndex);
    }

    [Theory]
    [InlineData(PageNavigation.Next)]
    [InlineData(PageNavigation.Last)]
    [InlineData(PageNavigation.Previous)]
    [InlineData(PageNavigation.Specific)]
    public void EnterAll_RequiresFirst(PageNavigation action)
    {
        var session = GeneratorSession.Create(3);

        Assert.Equal(DomainError.AllEntryRequiresFirst, Rejected(session.ShowAll(Nav(action, 2, 1))));
    }

    [Fact]
    public void EnterAll_WhenNothingLeft_IsCompleted_AndStaysInNext()
    {
        var session = AfterNext(GeneratorSession.Create(1), 1); // N=1: הקומבינציה היחידה כבר הוצגה

        Assert.IsType<DomainTransition.Completed>(session.ShowAll(Nav(PageNavigation.First, 5)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void ShowAll_InvalidPageSize_IsRejected(int size)
    {
        Assert.Equal(DomainError.InvalidPageSize, Rejected(GeneratorSession.Create(3).ShowAll(Nav(PageNavigation.First, size))));
    }

    // ---------- דפדוף ----------

    [Fact]
    public void Navigate_AllButtons_ChangeOnlyPageNumber()
    {
        // N=4: 24 קומבינציות, עוגן 0, גודל 5 => 5 עמודים
        var session = Changed(GeneratorSession.Create(4).ShowAll(Nav(PageNavigation.First, 5)));

        session = Changed(session.ShowAll(Nav(PageNavigation.Next, 5)));
        Assert.Equal(2, AllOf(session).PageNumber);

        session = Changed(session.ShowAll(Nav(PageNavigation.Last, 5)));
        Assert.Equal(5, AllOf(session).PageNumber);
        Assert.Equal(4, session.CurrentPage!.Value.Count); // 24 = 5+5+5+5+4

        session = Changed(session.ShowAll(Nav(PageNavigation.Previous, 5)));
        Assert.Equal(4, AllOf(session).PageNumber);

        session = Changed(session.ShowAll(Nav(PageNavigation.Specific, 5, 3)));
        Assert.Equal(3, AllOf(session).PageNumber);

        session = Changed(session.ShowAll(Nav(PageNavigation.First, 5)));
        Assert.Equal(new AllProgress(0, 5, 1), AllOf(session));
    }

    [Fact]
    public void Navigate_PastEdges_IsRejected()
    {
        var first = Changed(GeneratorSession.Create(3).ShowAll(Nav(PageNavigation.First, 2)));
        var last = Changed(first.ShowAll(Nav(PageNavigation.Last, 2)));

        Assert.Equal(DomainError.NoPreviousPage, Rejected(first.ShowAll(Nav(PageNavigation.Previous, 2))));
        Assert.Equal(DomainError.NoNextPage, Rejected(last.ShowAll(Nav(PageNavigation.Next, 2))));
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(4L)]
    [InlineData(long.MaxValue)]
    [InlineData(null)]
    public void Navigate_InvalidSpecificPage_IsRejected(long? page)
    {
        var all = Changed(GeneratorSession.Create(3).ShowAll(Nav(PageNavigation.First, 2))); // 3 עמודים

        Assert.Equal(DomainError.InvalidPageNumber, Rejected(all.ShowAll(Nav(PageNavigation.Specific, 2, page))));
    }

    [Fact]
    public void Navigate_WithDifferentPageSize_IsRejected()
    {
        var all = Changed(GeneratorSession.Create(3).ShowAll(Nav(PageNavigation.First, 2)));

        Assert.Equal(DomainError.PageSizeLocked, Rejected(all.ShowAll(Nav(PageNavigation.First, 3))));
    }

    // ---------- "חזור" ----------

    [Fact]
    public void FullFlow_N3_MatchesSpecificationExample()
    {
        // Next×2 ← All(2) ← Last ← Previous ← Back ← Next
        var session = AfterNext(GeneratorSession.Create(3), 2);
        session = Changed(session.ShowAll(Nav(PageNavigation.First, 2)));
        session = Changed(session.ShowAll(Nav(PageNavigation.Last, 2)));
        session = Changed(session.ShowAll(Nav(PageNavigation.Previous, 2)));

        session = Changed(session.BackToNext());
        Assert.Equal(3, CursorOf(session).Index);                       // קומבינציה מספר 4
        Assert.Equal(new[] { 2, 3, 1 }, CursorOf(session).Permutation);

        session = Changed(session.AdvanceNext());
        Assert.Equal(4, CursorOf(session).Index);                       // קומבינציה מספר 5
        Assert.Equal(new[] { 3, 1, 2 }, CursorOf(session).Permutation);
    }

    [Fact]
    public void Back_InNextMode_IsRejected()
    {
        Assert.Equal(DomainError.InvalidMode, Rejected(GeneratorSession.Create(3).BackToNext()));
    }

    [Fact]
    public void AfterBack_CanReenterAll_WithDifferentPageSize_FromNewPosition()
    {
        var session = Changed(GeneratorSession.Create(3).ShowAll(Nav(PageNavigation.First, 2)));
        session = Changed(session.BackToNext()); // חוזרים לאינדקס 1

        var again = Changed(session.ShowAll(Nav(PageNavigation.First, 3)));

        Assert.Equal(new AllProgress(StartIndex: 2, PageSize: 3, PageNumber: 1), AllOf(again));
    }

    [Fact]
    public void N20_LastPage_ThenBack_ReachesLastPermutation_WithoutScanning()
    {
        long total = FactorialCalculator.Of(20);
        var session = Changed(GeneratorSession.Create(20).ShowAll(Nav(PageNavigation.First, 1)));

        session = Changed(session.ShowAll(Nav(PageNavigation.Last, 1)));
        Assert.Equal(total, AllOf(session).PageNumber);

        session = Changed(session.BackToNext());
        Assert.Equal(total - 1, CursorOf(session).Index);
        Assert.Equal(Enumerable.Range(1, 20).Reverse(), CursorOf(session).Permutation);

        Assert.IsType<DomainTransition.Completed>(session.AdvanceNext());
    }
}
