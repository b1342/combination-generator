using System.Net;
using CombinationGenerator.Api.Contracts;
using CombinationGenerator.Application.Generators;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CombinationGenerator.Api.Tests;

/// <summary>"כל הקומבינציות", הדפדוף, שינוי גודל העמוד ו"חזור", דרך HTTP אמיתי.</summary>
public class AllCombinationsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly GeneratorApi _api;

    public AllCombinationsTests(WebApplicationFactory<Program> factory)
    {
        _api = new GeneratorApi(factory.CreateClient());
    }

    private static IEnumerable<string> Numbers(PageResponse page) => page.Rows.Select(row => row.Number);

    // ---------- הכניסה ----------

    [Fact]
    public async Task All_StartsAfterLastShownCombination()
    {
        SessionResponse session = await _api.StartOkAsync(3);
        await _api.NextTimesAsync(session.SessionId, 2);      // הוצגו 1 ו-2

        SessionResponse all = await _api.AllOkAsync(session.SessionId, "First", 2);

        Assert.Equal(SessionMode.All, all.Mode);
        Assert.Null(all.Current);

        PageResponse page = GeneratorApi.RequirePage(all);
        Assert.Equal("1", page.PageNumber);
        Assert.Equal("2", page.TotalPages);                   // נשארו 4 (3..6), 2 בעמוד
        Assert.Equal(new[] { "3", "4" }, Numbers(page));
        Assert.Equal(new[] { 2, 1, 3 }, page.Rows[0].Permutation);
        Assert.Equal(new[] { 2, 3, 1 }, page.Rows[1].Permutation);
    }

    [Fact]
    public async Task All_WithoutAnyNext_StartsFromFirstCombination()
    {
        SessionResponse session = await _api.StartOkAsync(3);

        PageResponse page = GeneratorApi.RequirePage(await _api.AllOkAsync(session.SessionId, "First", 2));

        Assert.Equal("3", page.TotalPages);
        Assert.Equal(new[] { "1", "2" }, Numbers(page));
    }

    [Fact]
    public async Task All_EntryMustBeFirstPage()
    {
        SessionResponse session = await _api.StartOkAsync(3);

        await GeneratorApi.AssertProblemAsync(
            await _api.AllAsync(session.SessionId, "Next", 2), HttpStatusCode.Conflict, "ALL_ENTRY_REQUIRES_FIRST");
    }

    [Fact]
    public async Task All_AfterLastCombination_ReturnsNoMore()
    {
        SessionResponse session = await _api.StartOkAsync(3);
        await _api.NextTimesAsync(session.SessionId, 6);      // הוצגו כולן

        SessionResponse result = await _api.AllOkAsync(session.SessionId, "First", 2);

        Assert.True(result.NoMorePermutations);
        Assert.Equal(SessionMode.Next, result.Mode);          // נשארים במסך "הבא"
    }

    // ---------- דפדוף ----------

    [Fact]
    public async Task Navigation_MovesBetweenPages()
    {
        // n=4: 24 קומבינציות, 5 בעמוד → 5 עמודים (האחרון עם 4 שורות)
        SessionResponse session = await _api.StartOkAsync(4);
        Guid id = session.SessionId;
        await _api.AllOkAsync(id, "First", 5);

        PageResponse next = GeneratorApi.RequirePage(await _api.AllOkAsync(id, "Next", 5));
        Assert.Equal("2", next.PageNumber);
        Assert.Equal(new[] { "6", "7", "8", "9", "10" }, Numbers(next));

        PageResponse last = GeneratorApi.RequirePage(await _api.AllOkAsync(id, "Last", 5));
        Assert.Equal("5", last.PageNumber);
        Assert.Equal(new[] { "21", "22", "23", "24" }, Numbers(last));

        PageResponse previous = GeneratorApi.RequirePage(await _api.AllOkAsync(id, "Previous", 5));
        Assert.Equal("4", previous.PageNumber);

        PageResponse specific = GeneratorApi.RequirePage(await _api.AllOkAsync(id, "Specific", 5, "3"));
        Assert.Equal("3", specific.PageNumber);
        Assert.Equal("11", specific.Rows[0].Number);

        PageResponse first = GeneratorApi.RequirePage(await _api.AllOkAsync(id, "First", 5));
        Assert.Equal("1", first.PageNumber);
    }

    [Fact]
    public async Task Navigation_PastTheEdges_Returns409()
    {
        SessionResponse session = await _api.StartOkAsync(3);
        Guid id = session.SessionId;
        await _api.AllOkAsync(id, "First", 2);

        await GeneratorApi.AssertProblemAsync(await _api.AllAsync(id, "Previous", 2), HttpStatusCode.Conflict, "NO_PREVIOUS_PAGE");

        await _api.AllOkAsync(id, "Last", 2);
        await GeneratorApi.AssertProblemAsync(await _api.AllAsync(id, "Next", 2), HttpStatusCode.Conflict, "NO_NEXT_PAGE");
    }

    [Theory]
    [InlineData("0")]       // אין דף 0
    [InlineData("4")]       // יש רק 3 דפים
    [InlineData("-1")]      // שלילי
    [InlineData("abc")]     // לא מספר
    [InlineData("")]        // ריק
    [InlineData(null)]      // חסר
    public async Task SpecificPage_Invalid_Returns400(string? pageNumber)
    {
        SessionResponse session = await _api.StartOkAsync(3);
        await _api.AllOkAsync(session.SessionId, "First", 2);

        await GeneratorApi.AssertProblemAsync(
            await _api.AllAsync(session.SessionId, "Specific", 2, pageNumber), HttpStatusCode.BadRequest, "INVALID_PAGE_NUMBER");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(201)]       // מעל MaxPageSize (200)
    public async Task PageSize_OutOfRange_Returns400(int pageSize)
    {
        SessionResponse session = await _api.StartOkAsync(3);

        await GeneratorApi.AssertProblemAsync(
            await _api.AllAsync(session.SessionId, "First", pageSize), HttpStatusCode.BadRequest, "INVALID_PAGE_SIZE");
    }

    [Fact]
    public async Task UnknownAction_Returns400()
    {
        SessionResponse session = await _api.StartOkAsync(3);

        // "Jump" הוא לא ערך של PageNavigation, ולכן ASP.NET דוחה את הגוף עוד לפני ה-Controller
        using HttpResponseMessage response = await _api.AllAsync(session.SessionId, "Jump", 2);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- שינוי גודל עמוד ----------

    [Fact]
    public async Task ChangingPageSize_KeepsTopRowOnScreen()
    {
        SessionResponse session = await _api.StartOkAsync(4);
        Guid id = session.SessionId;
        await _api.AllOkAsync(id, "First", 5);
        await _api.AllOkAsync(id, "Next", 5);                 // עמוד 2: שורות 6..10, בראש המסך 6

        PageResponse resized = GeneratorApi.RequirePage(await _api.AllOkAsync(id, "Current", 2));

        // בגודל 2, שורה 6 נמצאת בעמוד 3 (שורות 5, 6)
        Assert.Equal("3", resized.PageNumber);
        Assert.Equal("12", resized.TotalPages);
        Assert.Equal(2, resized.PageSize);
        Assert.Equal(new[] { "5", "6" }, Numbers(resized));
    }

    // ---------- שורת מספרי הדפים ----------

    [Fact]
    public async Task PageLinks_ShowFirstLastNeighboursAndEllipsis()
    {
        SessionResponse session = await _api.StartOkAsync(4);
        Guid id = session.SessionId;
        await _api.AllOkAsync(id, "First", 2);                // 12 עמודים

        PageResponse page = GeneratorApi.RequirePage(await _api.AllOkAsync(id, "Specific", 2, "6"));

        // כל כפתור כטקסט: מספר, "[מספר]" לדף הנוכחי, או "…"
        IEnumerable<string> links = page.Links.Select(link =>
            link.Type == "ellipsis" ? "…" : link.IsCurrent ? $"[{link.Number}]" : link.Number ?? "");

        Assert.Equal(new[] { "1", "…", "4", "5", "[6]", "7", "8", "…", "12" }, links);
    }

    // ---------- "חזור" ----------

    [Fact]
    public async Task Back_ReturnsToNextWithLastRowOfPage()
    {
        SessionResponse session = await _api.StartOkAsync(3);
        Guid id = session.SessionId;
        await _api.NextTimesAsync(id, 2);
        await _api.AllOkAsync(id, "First", 2);                // עמוד 1: שורות 3, 4

        SessionResponse back = await _api.BackOkAsync(id);

        Assert.Equal(SessionMode.Next, back.Mode);
        Assert.Null(back.Page);
        CombinationResponse current = GeneratorApi.RequireCurrent(back);
        Assert.Equal("4", current.Number);                    // השורה האחרונה בעמוד
        Assert.Equal(new[] { 2, 3, 1 }, current.Permutation);

        // ו"הבא" ממשיך משם
        Assert.Equal("5", GeneratorApi.RequireCurrent(await _api.NextOkAsync(id)).Number);
    }

    [Fact]
    public async Task Back_InNextMode_Returns409()
    {
        SessionResponse session = await _api.StartOkAsync(3);

        await GeneratorApi.AssertProblemAsync(await _api.BackAsync(session.SessionId), HttpStatusCode.Conflict, "INVALID_MODE");
    }

    [Fact]
    public async Task Next_InAllMode_Returns409()
    {
        SessionResponse session = await _api.StartOkAsync(3);
        await _api.AllOkAsync(session.SessionId, "First", 2);

        await GeneratorApi.AssertProblemAsync(await _api.NextAsync(session.SessionId), HttpStatusCode.Conflict, "INVALID_MODE");
    }
}
