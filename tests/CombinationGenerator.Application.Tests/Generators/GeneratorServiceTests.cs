using CombinationGenerator.Application.Generators;
using CombinationGenerator.Application.Pagination;
using CombinationGenerator.Domain.Sessions;

namespace CombinationGenerator.Application.Tests.Generators;

public class GeneratorServiceTests
{
    private readonly FakeSessionStore _store = new();
    private readonly GeneratorService _service;

    public GeneratorServiceTests()
    {
        _service = new GeneratorService(_store, new GeneratorOptions { MaxPageSize = 200 });
    }

    // ---------- עזרים ----------

    private static SessionView Ok(OperationResult result)
    {
        Assert.Equal(OperationStatus.Success, result.Status);
        var view = result.View;
        Assert.NotNull(view);
        return view;
    }

    private async Task<Guid> StartAsync(int n) => Ok(await _service.StartAsync(n)).SessionId;

    private async Task<SessionView> NextTimesAsync(Guid id, int times)
    {
        SessionView? view = null;
        for (int i = 0; i < times; i++)
        {
            view = Ok(await _service.NextAsync(id));
        }

        Assert.NotNull(view);
        return view;
    }

    private static string Links(PageView page) =>
        string.Join(" ", page.Links.Select(token => token switch
        {
            PageNumberToken { IsCurrent: true } p => $"[{p.Number}]",
            PageNumberToken p => p.Number.ToString(),
            EllipsisToken => "…",
            _ => "?",
        }));

    // ---------- התחל ----------

    [Fact]
    public async Task Start_CreatesSessionInNextMode_WithTotalCount()
    {
        var view = Ok(await _service.StartAsync(3));

        Assert.NotEqual(Guid.Empty, view.SessionId);
        Assert.Equal(3, view.N);
        Assert.Equal(6, view.TotalCount);
        Assert.Equal(SessionMode.Next, view.Mode);
        Assert.Null(view.Current); // עוד לא לחצו "הבא"
        Assert.Null(view.Page);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public async Task Start_InvalidN_IsRejected_AndNothingIsStored(int n)
    {
        var result = await _service.StartAsync(n);

        Assert.Equal(OperationStatus.InvalidN, result.Status);
        Assert.Null(result.View);
        Assert.Equal(0, _store.Count);
    }

    [Fact]
    public async Task Start_WhenStoreIsFull_ReturnsCapacityReached()
    {
        _store.IsFull = true;

        Assert.Equal(OperationStatus.CapacityReached, (await _service.StartAsync(3)).Status);
    }

    // ---------- הבא ----------

    [Fact]
    public async Task Next_ThreeTimes_ShowsCombinationNumberThree()
    {
        var id = await StartAsync(3);

        var view = await NextTimesAsync(id, 3);

        Assert.Equal(3, view.Current?.Number);
        Assert.Equal(new[] { 2, 1, 3 }, view.Current?.Permutation);
    }

    [Fact]
    public async Task Next_AfterLast_ReturnsNoMore_WithUnchangedView()
    {
        var id = await StartAsync(3);
        await NextTimesAsync(id, 6);

        var result = await _service.NextAsync(id);

        Assert.Equal(OperationStatus.NoMorePermutations, result.Status);
        Assert.Equal(6, result.View?.Current?.Number);
        Assert.Equal(new[] { 3, 2, 1 }, result.View?.Current?.Permutation);
    }

    [Fact]
    public async Task Next_UnknownSession_IsNotFound()
    {
        Assert.Equal(OperationStatus.SessionNotFound, (await _service.NextAsync(Guid.NewGuid())).Status);
    }

    [Fact]
    public async Task Next_WhenSomeoneElseSavedFirst_ReturnsConflict_WithoutRetrying()
    {
        var id = await StartAsync(3);
        await NextTimesAsync(id, 1);
        _store.BeforeReplace = _store.BumpVersion; // "לשונית אחרת" שומרת רגע לפנינו

        var result = await _service.NextAsync(id);

        Assert.Equal(OperationStatus.Conflict, result.Status);
        Assert.Null(result.View);

        // לא ניסה שוב אוטומטית: עדיין מוצגת קומבינציה 1
        _store.BeforeReplace = null;
        Assert.Equal(1, Ok(await _service.GetStateAsync(id)).Current?.Number);
    }

    // ---------- כל הקומבינציות ----------

    [Fact]
    public async Task ShowAll_AfterTwoNext_ShowsRowsThreeAndFour_WithLinks()
    {
        var id = await StartAsync(3);
        await NextTimesAsync(id, 2);

        var view = Ok(await _service.ShowAllAsync(id, PageNavigation.First, pageSize: 2));

        Assert.Equal(SessionMode.All, view.Mode);
        Assert.Null(view.Current);
        var page = view.Page;
        Assert.NotNull(page);
        Assert.Equal(1, page.PageNumber);
        Assert.Equal(2, page.TotalPages);
        Assert.Equal(new long[] { 3, 4 }, page.Rows.Select(r => r.Number));
        Assert.Equal(new[] { 2, 1, 3 }, page.Rows[0].Permutation);
        Assert.Equal(new[] { 2, 3, 1 }, page.Rows[1].Permutation);
        Assert.Equal("[1] 2", Links(page));
    }

    [Fact]
    public async Task ShowAll_Last_ShowsLastPage()
    {
        var id = await StartAsync(3);
        await NextTimesAsync(id, 2);
        await _service.ShowAllAsync(id, PageNavigation.First, 2);

        var page = Ok(await _service.ShowAllAsync(id, PageNavigation.Last, 2)).Page;

        Assert.Equal(new long[] { 5, 6 }, page?.Rows.Select(r => r.Number));
        Assert.Equal(new[] { 3, 2, 1 }, page?.Rows[1].Permutation);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(201)]
    public async Task ShowAll_PageSizeOutOfRange_IsRejected(int pageSize)
    {
        var id = await StartAsync(3);

        var result = await _service.ShowAllAsync(id, PageNavigation.First, pageSize);

        Assert.Equal(OperationStatus.InvalidPageSize, result.Status);
        Assert.Equal(SessionMode.Next, Ok(await _service.GetStateAsync(id)).Mode); // לא השתנה כלום
    }

    [Fact]
    public async Task ShowAll_EntryWithoutFirst_IsRejected()
    {
        var id = await StartAsync(3);

        Assert.Equal(OperationStatus.AllEntryRequiresFirst, (await _service.ShowAllAsync(id, PageNavigation.Next, 2)).Status);
    }

    [Fact]
    public async Task ShowAll_WhenEverythingWasShown_ReturnsNoMore_AndStaysInNext()
    {
        var id = await StartAsync(1);
        await NextTimesAsync(id, 1);

        var result = await _service.ShowAllAsync(id, PageNavigation.First, 5);

        Assert.Equal(OperationStatus.NoMorePermutations, result.Status);
        Assert.Equal(SessionMode.Next, result.View?.Mode);
    }

    [Fact]
    public async Task Next_InAllMode_IsRejected()
    {
        var id = await StartAsync(3);
        await _service.ShowAllAsync(id, PageNavigation.First, 2);

        Assert.Equal(OperationStatus.InvalidMode, (await _service.NextAsync(id)).Status);
    }

    // ---------- חזור ----------

    [Fact]
    public async Task Back_FromFirstPage_ReturnsToNext_WithLastRowOfPage()
    {
        var id = await StartAsync(3);
        await NextTimesAsync(id, 2);
        await _service.ShowAllAsync(id, PageNavigation.First, 2);

        var view = Ok(await _service.BackAsync(id));

        Assert.Equal(SessionMode.Next, view.Mode);
        Assert.Equal(4, view.Current?.Number);
        Assert.Equal(new[] { 2, 3, 1 }, view.Current?.Permutation);

        // ו"הבא" ממשיך מ-5
        Assert.Equal(5, Ok(await _service.NextAsync(id)).Current?.Number);
    }

    [Fact]
    public async Task Back_InNextMode_IsRejected()
    {
        var id = await StartAsync(3);

        Assert.Equal(OperationStatus.InvalidMode, (await _service.BackAsync(id)).Status);
    }

    // ---------- מצב ואיפוס ----------

    [Fact]
    public async Task GetState_ReturnsCurrentView_WithoutChangingIt()
    {
        var id = await StartAsync(3);
        await NextTimesAsync(id, 2);

        var first = Ok(await _service.GetStateAsync(id));
        var second = Ok(await _service.GetStateAsync(id));

        Assert.Equal(2, first.Current?.Number);
        Assert.Equal(2, second.Current?.Number);
    }

    [Fact]
    public async Task Reset_RemovesSession()
    {
        var id = await StartAsync(3);

        Assert.Equal(OperationStatus.Success, (await _service.ResetAsync(id)).Status);
        Assert.Equal(OperationStatus.SessionNotFound, (await _service.GetStateAsync(id)).Status);
        Assert.Equal(OperationStatus.SessionNotFound, (await _service.ResetAsync(id)).Status);
    }
}
