using System.Net;
using System.Text.Json;
using CombinationGenerator.Api.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CombinationGenerator.Api.Tests;

/// <summary>
/// n=20: מספרים מעל 2^53 חייבים להגיע לדפדפן כטקסט מדויק, וכל עמוד מחושב בלי לחשב מראש את מה שלפניו.
/// </summary>
public class LargeNumbersTests : IClassFixture<WebApplicationFactory<Program>>
{
    // 20! וכמה עמודים של 200 יש בו (20! מתחלק ב-200 בדיוק)
    private const string Factorial20 = "2432902008176640000";
    private const string PagesOf200 = "12164510040883200";

    private readonly GeneratorApi _api;

    public LargeNumbersTests(WebApplicationFactory<Program> factory)
    {
        _api = new GeneratorApi(factory.CreateClient());
    }

    [Fact]
    public async Task Start_N20_SendsTotalCountAsExactText()
    {
        using HttpResponseMessage response = await _api.StartAsync(20);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // קוראים את ה-JSON הגולמי: הערך חייב להיות במרכאות ("..."), לא מספר
        using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement totalCount = json.RootElement.GetProperty("totalCount");

        Assert.Equal(JsonValueKind.String, totalCount.ValueKind);
        Assert.Equal(Factorial20, totalCount.GetString());
    }

    [Fact]
    public async Task LastPage_N20_HasExactNumbersAndLastPermutation()
    {
        SessionResponse session = await _api.StartOkAsync(20);
        Guid id = session.SessionId;
        await _api.AllOkAsync(id, "First", 200);

        PageResponse last = GeneratorApi.RequirePage(await _api.AllOkAsync(id, "Last", 200));

        Assert.Equal(PagesOf200, last.PageNumber);
        Assert.Equal(PagesOf200, last.TotalPages);
        Assert.Equal(200, last.Rows.Count);
        Assert.Equal("2432902008176639801", last.Rows[0].Number);
        Assert.Equal(Factorial20, last.Rows[^1].Number);

        // הקומבינציה האחרונה: 20, 19, ..., 1
        Assert.Equal(Enumerable.Range(1, 20).Reverse().ToArray(), last.Rows[^1].Permutation);
    }

    [Fact]
    public async Task SpecificPage_N20_AcceptsHugePageNumberAsText()
    {
        SessionResponse session = await _api.StartOkAsync(20);
        Guid id = session.SessionId;
        await _api.AllOkAsync(id, "First", 200);

        PageResponse page = GeneratorApi.RequirePage(await _api.AllOkAsync(id, "Specific", 200, "12164510040883199"));

        Assert.Equal("12164510040883199", page.PageNumber);
        Assert.Equal("2432902008176639601", page.Rows[0].Number);

        // דף אחד אחרי האחרון לא קיים
        await GeneratorApi.AssertProblemAsync(
            await _api.AllAsync(id, "Specific", 200, "12164510040883201"), HttpStatusCode.BadRequest, "INVALID_PAGE_NUMBER");
    }

    [Fact]
    public async Task PageJson_SendsBigNumbersAsTextAndSmallNumbersAsNumbers()
    {
        SessionResponse session = await _api.StartOkAsync(20);
        Guid id = session.SessionId;
        await _api.AllOkAsync(id, "First", 200);

        using HttpResponseMessage response = await _api.AllAsync(id, "Last", 200);
        using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement page = json.RootElement.GetProperty("page");
        JsonElement firstRow = page.GetProperty("rows")[0];

        // מספרים שיכולים להיות גדולים → טקסט
        Assert.Equal(JsonValueKind.String, page.GetProperty("pageNumber").ValueKind);
        Assert.Equal(JsonValueKind.String, page.GetProperty("totalPages").ValueKind);
        Assert.Equal(JsonValueKind.String, firstRow.GetProperty("number").ValueKind);

        // מספרים קטנים → מספר רגיל
        Assert.Equal(JsonValueKind.Number, page.GetProperty("pageSize").ValueKind);
        Assert.Equal(JsonValueKind.Number, firstRow.GetProperty("permutation")[0].ValueKind);
    }
}
