using System.Net;
using CombinationGenerator.Api.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CombinationGenerator.Api.Tests;

/// <summary>"איפוס", כמה משתמשות במקביל, ו-Swagger.</summary>
public class SessionLifecycleTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly GeneratorApi _api;

    public SessionLifecycleTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _api = new GeneratorApi(factory.CreateClient());
    }

    // ---------- "איפוס" ----------

    [Fact]
    public async Task Reset_DeletesSession()
    {
        SessionResponse session = await _api.StartOkAsync(3);

        using (HttpResponseMessage reset = await _api.ResetAsync(session.SessionId))
        {
            Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);
        }

        await GeneratorApi.AssertProblemAsync(await _api.GetStateAsync(session.SessionId), HttpStatusCode.NotFound, "SESSION_NOT_FOUND");
        await GeneratorApi.AssertProblemAsync(await _api.NextAsync(session.SessionId), HttpStatusCode.NotFound, "SESSION_NOT_FOUND");
    }

    [Fact]
    public async Task Reset_Twice_StillReturns204()
    {
        SessionResponse session = await _api.StartOkAsync(3);

        // אידמפוטנטי: התוצאה זהה — אין Session
        for (int i = 0; i < 2; i++)
        {
            using HttpResponseMessage reset = await _api.ResetAsync(session.SessionId);
            Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);
        }
    }

    // ---------- כמה משתמשות במקביל ----------

    [Fact]
    public async Task ThreeUsersAtOnce_EachKeepsHerOwnState()
    {
        // שלוש משתמשות, כל אחת עם n אחר ומספר לחיצות אחר, כולן באותו זמן
        (int N, int Clicks)[] users = { (3, 1), (4, 3), (5, 5) };

        SessionResponse[] results = await Task.WhenAll(users.Select(async user =>
        {
            // לכל משתמשת לקוח משלה, כמו דפדפן נפרד
            var api = new GeneratorApi(_factory.CreateClient());
            SessionResponse session = await api.StartOkAsync(user.N);
            return await api.NextTimesAsync(session.SessionId, user.Clicks);
        }));

        // כל אחת קיבלה Session משלה
        Assert.Equal(3, results.Select(r => r.SessionId).Distinct().Count());

        for (int i = 0; i < users.Length; i++)
        {
            Assert.Equal(users[i].N, results[i].N);
            Assert.Equal(users[i].Clicks.ToString(), GeneratorApi.RequireCurrent(results[i]).Number);
        }
    }

    // ---------- Swagger ----------

    [Fact]
    public async Task Swagger_DescribesAllEndpoints()
    {
        using HttpClient client = _factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string document = await response.Content.ReadAsStringAsync();

        Assert.Contains("\"/api/generators\"", document);
        Assert.Contains("\"/api/generators/{id}\"", document);
        Assert.Contains("\"/api/generators/{id}/next\"", document);
        Assert.Contains("\"/api/generators/{id}/all\"", document);
        Assert.Contains("\"/api/generators/{id}/back\"", document);
    }
}
