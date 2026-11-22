using System.Globalization;
using CombinationGenerator.Api.Contracts;
using CombinationGenerator.Api.Errors;
using CombinationGenerator.Application.Generators;
using CombinationGenerator.Domain.Sessions;
using Microsoft.AspNetCore.Mvc;

namespace CombinationGenerator.Api.Controllers;

/// <summary>
/// הכתובות שהדפדפן פונה אליהן. כל פונקציה כאן: מקבלת בקשת HTTP, קוראת ל-Service, ומתרגמת את התוצאה ל-HTTP.
/// אין כאן שום לוגיקה של המערכת.
/// ה-ProducesResponseType מעל כל פונקציה מתארים ל-Swagger אילו תשובות אפשריות; הם לא משנים את ההתנהגות.
/// </summary>
[ApiController]
[Route("api/generators")]
// מצב של Session משתנה בכל לחיצה: אסור לדפדפן או לפרוקסי לשמור עותק ולהחזיר אותו במקום לשאול את השרת
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class GeneratorsController : ControllerBase
{
    private readonly GeneratorService _service;

    public GeneratorsController(GeneratorService service)
    {
        _service = service;
    }

    /// <summary>StartAPI — "התחל". POST /api/generators  גוף: { "n": 3 }</summary>
    [HttpPost]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Start([FromBody] StartRequest request, CancellationToken cancellationToken)
    {
        // n חובה. שדה חסר הוא בקשה לא תקינה, ולא n = 0
        if (request.N is not { } n)
        {
            return ErrorResponse(ApiErrors.InvalidRequest);
        }

        OperationResult result = await _service.StartAsync(n, cancellationToken);

        // 201 Created + הכתובת של ה-Session החדש בכותרת Location
        if (result is { Status: OperationStatus.Success, View: { } view })
        {
            return CreatedAtAction(
                nameof(GetState),
                new { id = view.SessionId },
                SessionResponseMapper.ToResponse(view, noMorePermutations: false));
        }

        return ToActionResult(result);
    }

    /// <summary>המצב הנוכחי, בלי לשנות כלום. GET /api/generators/{id}</summary>
    [HttpGet("{id}")]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetState(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(await _service.GetStateAsync(id, cancellationToken));

    /// <summary>GetNextAPI — "הבא". POST /api/generators/{id}/next</summary>
    [HttpPost("{id}/next")]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Next(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(await _service.NextAsync(id, cancellationToken));

    /// <summary>"הקודם": הקומבינציה הקודמת. POST /api/generators/{id}/previous</summary>
    [HttpPost("{id}/previous")]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Previous(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(await _service.PreviousAsync(id, cancellationToken));

    /// <summary>
    /// GetAllAPI — "כל הקומבינציות", כפתורי הדפדוף ושינוי גודל עמוד.
    /// POST /api/generators/{id}/all  גוף: { "action": "Next", "pageSize": 20, "pageNumber": "7" }
    /// </summary>
    [HttpPost("{id}/all")]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ShowAll(Guid id, [FromBody] ShowAllRequest request, CancellationToken cancellationToken)
    {
        // הפעולה חובה, וחייבת להיות אחד מהשמות המוגדרים
        if (request.Action is not { } action || !Enum.IsDefined(action))
        {
            return ErrorResponse(ApiErrors.InvalidAction);
        }

        // גודל העמוד חובה בכל קריאה
        if (request.PageSize is not { } pageSize)
        {
            return ErrorResponse(ApiErrors.InvalidRequest);
        }

        // מספר דף שייך רק ל-Specific: חובה בה, ואסור בכל השאר (כדי שבקשה לא ברורה לא תבוצע בשקט)
        bool isSpecific = action == PageAction.Specific;
        bool hasPageNumber = !string.IsNullOrEmpty(request.PageNumber);
        if (isSpecific && !hasPageNumber)
        {
            return ErrorResponse(ApiErrors.InvalidRequest);
        }

        if (!isSpecific && hasPageNumber)
        {
            return ErrorResponse(ApiErrors.InvalidRequest);
        }

        long? pageNumber = null;
        if (isSpecific)
        {
            if (!TryParsePageNumber(request.PageNumber!, out long parsed))
            {
                return ErrorResponse(ApiErrors.For(OperationStatus.InvalidPageNumber));
            }

            pageNumber = parsed;
        }

        return ToActionResult(await _service.ShowAllAsync(id, ToNavigation(action), pageSize, pageNumber, cancellationToken));
    }

    /// <summary>"חזור". POST /api/generators/{id}/back</summary>
    [HttpPost("{id}/back")]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Back(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(await _service.BackAsync(id, cancellationToken));

    /// <summary>
    /// "איפוס". DELETE /api/generators/{id}
    /// תמיד 204, גם אם ה-Session כבר לא קיים: התוצאה זהה — אין Session (פעולה אידמפוטנטית).
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Reset(Guid id, CancellationToken cancellationToken)
    {
        await _service.ResetAsync(id, cancellationToken);
        return NoContent();
    }

    // ---------- תרגום הקלט ----------

    /// <summary>
    /// מספר דף מטקסט ל-long (מדויק עד 9.2 × 10^18). רק ספרות, ובלי אפסים מובילים: "007" נדחה, כדי שלכל דף יהיה כתיב אחד.
    /// </summary>
    private static bool TryParsePageNumber(string text, out long pageNumber)
    {
        pageNumber = 0;
        if (text.Length > 1 && text[0] == '0')
        {
            return false;
        }

        return long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out pageNumber);
    }

    /// <summary>מה-enum של ה-API ל-enum של ה-Domain. כאן, ורק כאן, שני העולמות נפגשים.</summary>
    private static PageNavigation ToNavigation(PageAction action) => action switch
    {
        PageAction.First => PageNavigation.First,
        PageAction.Previous => PageNavigation.Previous,
        PageAction.Next => PageNavigation.Next,
        PageAction.Last => PageNavigation.Last,
        PageAction.Specific => PageNavigation.Specific,
        PageAction.Current => PageNavigation.Current,
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Unknown page action."),
    };

    // ---------- תרגום תוצאה ל-HTTP ----------

    /// <summary>
    /// Success ו-NoMorePermutations הם 200 עם המצב למסך. כל השאר הם שגיאות.
    /// </summary>
    private IActionResult ToActionResult(OperationResult result) => result switch
    {
        { Status: OperationStatus.Success, View: { } view } =>
            Ok(SessionResponseMapper.ToResponse(view, noMorePermutations: false)),

        { Status: OperationStatus.NoMorePermutations, View: { } view } =>
            Ok(SessionResponseMapper.ToResponse(view, noMorePermutations: true)),

        _ => ErrorResponse(ApiErrors.For(result.Status)),
    };

    /// <summary>שגיאה בפורמט ProblemDetails, עם שדה "code". הבנייה עצמה ב-ApiErrors.ToResult.</summary>
    private static ObjectResult ErrorResponse(ApiError error) => ApiErrors.ToResult(error);
}
