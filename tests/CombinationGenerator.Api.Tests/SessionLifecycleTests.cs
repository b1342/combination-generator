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

    [Fact]
    public async Task DoubleClick_OnSameSession_NeverSkipsOrRepeatsACombination()
    {
        SessionResponse session = await _api.StartOkAsync(5);

        // 10 לחיצות "הבא" באותו רגע על אותו Session
        HttpResponseMessage[] responses = await Task.WhenAll(
            Enumerable.Range(0, 10).Select(_ => _api.NextAsync(session.SessionId)));

        var shownNumbers = new List<string>();
        foreach (HttpResponseMessage response in responses)
        {
            using (response)
            {
                // כל לחיצה או מצליחה, או נדחית כהתנגשות. אף פעם לא 500.
                Assert.True(
                    response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Conflict,
                    $"Unexpected status {response.StatusCode}");

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    shownNumbers.Add(GeneratorApi.RequireCurrent(await GeneratorApi.ReadSessionAsync(response)).Number);
                }
            }
        }

        // כל הצלחה קידמה בדיוק צעד אחד: המספרים שונים זה מזה, והמצב הסופי הוא מספר ההצלחות
        Assert.Equal(shownNumbers.Count, shownNumbers.Distinct().Count());
        using HttpResponseMessage state = await _api.GetStateAsync(session.SessionId);
        Assert.Equal(
            shownNumbers.Count.ToString(),
            GeneratorApi.RequireCurrent(await GeneratorApi.ReadSessionAsync(state)).Number);
    }

    // ---------- כותרות HTTP ----------

    [Fact]
    public async Task SessionResponses_AreNeverCached()
    {
        SessionResponse session = await _api.StartOkAsync(3);

        using HttpResponseMessage response = await _api.GetStateAsync(session.SessionId);

        Assert.True(response.Headers.CacheControl?.NoStore);
    }

    [Fact]
    public async Task Cors_AllowsTheClient_AlsoOnErrorsAndPreflight()
    {
        const string client = "http://localhost:4200";
        using HttpClient http = _factory.CreateClient();

        // שגיאה (404): גם עליה הדפדפן צריך אישור, אחרת הלקוח לא יוכל לקרוא את ההודעה
        using var get = new HttpRequestMessage(HttpMethod.Get, $"/api/generators/{Guid.NewGuid()}");
        get.Headers.Add("Origin", client);
        using HttpResponseMessage error = await http.SendAsync(get);
        Assert.Equal(HttpStatusCode.NotFound, error.StatusCode);
        Assert.Equal(client, Assert.Single(error.Headers.GetValues("Access-Control-Allow-Origin")));

        // preflight: הדפדפן שואל לפני POST עם JSON
        using var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/generators");
        preflight.Headers.Add("Origin", client);
        preflight.Headers.Add("Access-Control-Request-Method", "POST");
        preflight.Headers.Add("Access-Control-Request-Headers", "content-type");
        using HttpResponseMessage allowed = await http.SendAsync(preflight);
        Assert.Equal(client, Assert.Single(allowed.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    [Fact]
    public async Task Cors_RejectsOtherOrigins()
    {
        using HttpClient http = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/generators/{Guid.NewGuid()}");
        request.Headers.Add("Origin", "http://evil.example");

        using HttpResponseMessage response = await http.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
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
        Assert.Contains("\"/api/generators/{id}/previous\"", document);
        Assert.Contains("\"/api/generators/{id}/all\"", document);
        Assert.Contains("\"/api/generators/{id}/back\"", document);
    }
}
