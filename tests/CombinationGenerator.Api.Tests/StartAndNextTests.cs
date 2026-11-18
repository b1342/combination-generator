using System.Net;
using CombinationGenerator.Api.Contracts;
using CombinationGenerator.Application.Generators;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CombinationGenerator.Api.Tests;

/// <summary>"התחל", "הבא" וקריאת המצב, דרך HTTP אמיתי.</summary>
public class StartAndNextTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly GeneratorApi _api;

    public StartAndNextTests(WebApplicationFactory<Program> factory)
    {
        _api = new GeneratorApi(factory.CreateClient());
    }

    // ---------- "התחל" ----------

    [Fact]
    public async Task Start_Returns201WithTotalCountAndLocation()
    {
        using HttpResponseMessage response = await _api.StartAsync(3);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        SessionResponse session = await GeneratorApi.ReadSessionAsync(response);

        Assert.Equal(3, session.N);
        Assert.Equal("6", session.TotalCount);
        Assert.Equal(SessionMode.Next, session.Mode);
        Assert.Null(session.Current);          // עוד לא לחצו "הבא"
        Assert.Null(session.Page);
        Assert.False(session.NoMorePermutations);

        // הכותרת Location מצביעה על הכתובת של ה-Session החדש
        Assert.EndsWith($"/api/generators/{session.SessionId}", response.Headers.Location?.ToString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    [InlineData(-5)]
    public async Task Start_WithInvalidN_Returns400(int n)
    {
        await GeneratorApi.AssertProblemAsync(await _api.StartAsync(n), HttpStatusCode.BadRequest, "INVALID_N");
    }

    // ---------- "הבא" ----------

    [Fact]
    public async Task Next_ReturnsAllPermutationsInLexicographicOrder()
    {
        SessionResponse session = await _api.StartOkAsync(3);

        int[][] expected =
        {
            new[] { 1, 2, 3 },
            new[] { 1, 3, 2 },
            new[] { 2, 1, 3 },
            new[] { 2, 3, 1 },
            new[] { 3, 1, 2 },
            new[] { 3, 2, 1 },
        };

        for (int i = 0; i < expected.Length; i++)
        {
            CombinationResponse current = GeneratorApi.RequireCurrent(await _api.NextOkAsync(session.SessionId));

            Assert.Equal((i + 1).ToString(), current.Number);   // המספר הסידורי מתחיל מ-1
            Assert.Equal(expected[i], current.Permutation);
        }
    }

    [Fact]
    public async Task Next_AfterLastPermutation_ReturnsNoMoreAndKeepsLastOne()
    {
        SessionResponse session = await _api.StartOkAsync(3);
        await _api.NextTimesAsync(session.SessionId, 6);

        SessionResponse afterEnd = await _api.NextOkAsync(session.SessionId);

        Assert.True(afterEnd.NoMorePermutations);
        Assert.Equal("אין יותר קומבינציות", afterEnd.Message);

        // הקומבינציה האחרונה נשארת על המסך
        CombinationResponse current = GeneratorApi.RequireCurrent(afterEnd);
        Assert.Equal("6", current.Number);
        Assert.Equal(new[] { 3, 2, 1 }, current.Permutation);
    }

    // ---------- קריאת המצב ----------

    [Fact]
    public async Task GetState_ReturnsCurrentStateWithoutChangingIt()
    {
        SessionResponse session = await _api.StartOkAsync(3);
        await _api.NextTimesAsync(session.SessionId, 2);

        // קוראים פעמיים: אותה תשובה, שום דבר לא התקדם
        for (int i = 0; i < 2; i++)
        {
            using HttpResponseMessage response = await _api.GetStateAsync(session.SessionId);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("2", GeneratorApi.RequireCurrent(await GeneratorApi.ReadSessionAsync(response)).Number);
        }

        // ו"הבא" ממשיך מאותו מקום
        Assert.Equal("3", GeneratorApi.RequireCurrent(await _api.NextOkAsync(session.SessionId)).Number);
    }

    [Fact]
    public async Task UnknownSession_Returns404()
    {
        Guid unknown = Guid.NewGuid();

        await GeneratorApi.AssertProblemAsync(await _api.GetStateAsync(unknown), HttpStatusCode.NotFound, "SESSION_NOT_FOUND");
        await GeneratorApi.AssertProblemAsync(await _api.NextAsync(unknown), HttpStatusCode.NotFound, "SESSION_NOT_FOUND");
        await GeneratorApi.AssertProblemAsync(await _api.AllAsync(unknown, "First", 10), HttpStatusCode.NotFound, "SESSION_NOT_FOUND");
        await GeneratorApi.AssertProblemAsync(await _api.BackAsync(unknown), HttpStatusCode.NotFound, "SESSION_NOT_FOUND");
    }
}
