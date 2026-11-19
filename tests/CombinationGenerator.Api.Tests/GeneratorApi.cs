using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CombinationGenerator.Api.Contracts;

namespace CombinationGenerator.Api.Tests;

/// <summary>
/// עוטף את הכתובות של השרת, כדי שהבדיקות יקראו כמו לחיצות על כפתורים: StartAsync, NextAsync, AllAsync...
/// </summary>
internal sealed class GeneratorApi
{
    // כמו בשרת: שמות בפורמט camelCase, ו-enum-ים כטקסט ("Next", "All")
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly HttpClient _client;

    public GeneratorApi(HttpClient client)
    {
        _client = client;
    }

    // ---------- הבקשות עצמן: מחזירות את תשובת ה-HTTP כמו שהיא ----------

    /// <summary>"התחל": POST /api/generators</summary>
    public Task<HttpResponseMessage> StartAsync(int n) =>
        _client.PostAsJsonAsync("/api/generators", new { n });

    /// <summary>המצב הנוכחי: GET /api/generators/{id}</summary>
    public Task<HttpResponseMessage> GetStateAsync(Guid id) =>
        _client.GetAsync($"/api/generators/{id}");

    /// <summary>"הבא": POST /api/generators/{id}/next</summary>
    public Task<HttpResponseMessage> NextAsync(Guid id) =>
        _client.PostAsync($"/api/generators/{id}/next", content: null);

    /// <summary>"כל הקומבינציות" ודפדוף: POST /api/generators/{id}/all</summary>
    public Task<HttpResponseMessage> AllAsync(Guid id, string action, int pageSize, string? pageNumber = null) =>
        _client.PostAsJsonAsync($"/api/generators/{id}/all", new { action, pageSize, pageNumber });

    /// <summary>"כל הקומבינציות" עם גוף JSON גולמי, לבדיקת קלט שגוי (מספר במקום מילה, שדה חסר).</summary>
    public Task<HttpResponseMessage> AllRawAsync(Guid id, string json) =>
        _client.PostAsync($"/api/generators/{id}/all", new StringContent(json, Encoding.UTF8, "application/json"));

    /// <summary>"חזור": POST /api/generators/{id}/back</summary>
    public Task<HttpResponseMessage> BackAsync(Guid id) =>
        _client.PostAsync($"/api/generators/{id}/back", content: null);

    /// <summary>"איפוס": DELETE /api/generators/{id}</summary>
    public Task<HttpResponseMessage> ResetAsync(Guid id) =>
        _client.DeleteAsync($"/api/generators/{id}");

    // ---------- קיצורים: שולחים, בודקים שהצליח, ומחזירים את המצב ----------

    /// <summary>"התחל" שחייב להצליח (201).</summary>
    public async Task<SessionResponse> StartOkAsync(int n)
    {
        using HttpResponseMessage response = await StartAsync(n);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ReadSessionAsync(response);
    }

    /// <summary>"הבא" שחייב להחזיר 200.</summary>
    public async Task<SessionResponse> NextOkAsync(Guid id) =>
        await ReadOkAsync(await NextAsync(id));

    /// <summary>"הבא" כמה פעמים ברצף. מחזיר את התשובה האחרונה.</summary>
    public async Task<SessionResponse> NextTimesAsync(Guid id, int times)
    {
        SessionResponse? last = null;
        for (int i = 0; i < times; i++)
        {
            last = await NextOkAsync(id);
        }

        Assert.NotNull(last);
        return last;
    }

    /// <summary>"כל הקומבינציות" או דפדוף שחייב להחזיר 200.</summary>
    public async Task<SessionResponse> AllOkAsync(Guid id, string action, int pageSize, string? pageNumber = null) =>
        await ReadOkAsync(await AllAsync(id, action, pageSize, pageNumber));

    /// <summary>"חזור" שחייב להחזיר 200.</summary>
    public async Task<SessionResponse> BackOkAsync(Guid id) =>
        await ReadOkAsync(await BackAsync(id));

    // ---------- קריאת התשובה ----------

    /// <summary>הופך את גוף התשובה (JSON) לאובייקט SessionResponse.</summary>
    public static async Task<SessionResponse> ReadSessionAsync(HttpResponseMessage response)
    {
        SessionResponse? session = await response.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions);
        Assert.NotNull(session);
        return session;
    }

    /// <summary>
    /// בודק שהתשובה היא שגיאה בפורמט ProblemDetails, עם הקוד והשדה "code" הצפויים.
    /// </summary>
    public static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode expectedStatus, string expectedCode)
    {
        using (response)
        {
            Assert.Equal(expectedStatus, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

            using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(expectedCode, body.RootElement.GetProperty("code").GetString());
        }
    }

    /// <summary>העמוד מהתשובה. חייב להיות קיים (מצב "כל הקומבינציות").</summary>
    public static PageResponse RequirePage(SessionResponse session)
    {
        Assert.NotNull(session.Page);
        return session.Page;
    }

    /// <summary>הקומבינציה הנוכחית מהתשובה. חייבת להיות קיימת (מצב "הבא").</summary>
    public static CombinationResponse RequireCurrent(SessionResponse session)
    {
        Assert.NotNull(session.Current);
        return session.Current;
    }

    private static async Task<SessionResponse> ReadOkAsync(HttpResponseMessage response)
    {
        using (response)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await ReadSessionAsync(response);
        }
    }
}
