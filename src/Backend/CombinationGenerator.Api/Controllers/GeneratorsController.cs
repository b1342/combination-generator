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
        OperationResult result = await _service.StartAsync(request.N, cancellationToken);

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
    [HttpGet("{id:guid}")]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetState(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(await _service.GetStateAsync(id, cancellationToken));

    /// <summary>GetNextAPI — "הבא". POST /api/generators/{id}/next</summary>
    [HttpPost("{id:guid}/next")]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Next(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(await _service.NextAsync(id, cancellationToken));

    /// <summary>
    /// GetAllAPI — "כל הקומבינציות", כפתורי הדפדוף ושינוי גודל עמוד.
    /// POST /api/generators/{id}/all  גוף: { "action": "Next", "pageSize": 20, "pageNumber": "7" }
    /// </summary>
    [HttpPost("{id:guid}/all")]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ShowAll(Guid id, [FromBody] ShowAllRequest request, CancellationToken cancellationToken)
    {
        // מספר הדף מגיע כטקסט; ממירים אותו ל-long (מדויק עד 9.2 × 10^18)
        long? pageNumber = null;
        if (request.Action == PageNavigation.Specific)
        {
            if (!long.TryParse(request.PageNumber, NumberStyles.None, CultureInfo.InvariantCulture, out long parsed))
            {
                return ErrorResponse(ApiErrors.For(OperationStatus.InvalidPageNumber));
            }

            pageNumber = parsed;
        }

        return ToActionResult(await _service.ShowAllAsync(id, request.Action, request.PageSize, pageNumber, cancellationToken));
    }

    /// <summary>"חזור". POST /api/generators/{id}/back</summary>
    [HttpPost("{id:guid}/back")]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Back(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(await _service.BackAsync(id, cancellationToken));

    /// <summary>
    /// "איפוס". DELETE /api/generators/{id}
    /// תמיד 204, גם אם ה-Session כבר לא קיים: התוצאה זהה — אין Session (פעולה אידמפוטנטית).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Reset(Guid id, CancellationToken cancellationToken)
    {
        await _service.ResetAsync(id, cancellationToken);
        return NoContent();
    }

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

    /// <summary>
    /// שגיאה בפורמט הסטנדרטי ProblemDetails, עם שדה "code" שהלקוח יכול לבדוק.
    /// </summary>
    private static ObjectResult ErrorResponse(ApiError error)
    {
        var problem = new ProblemDetails
        {
            Status = error.StatusCode,
            Title = error.Title,
            Detail = error.Message,
        };
        problem.Extensions["code"] = error.Code;

        return new ObjectResult(problem)
        {
            StatusCode = error.StatusCode,
            ContentTypes = { "application/problem+json" },
        };
    }
}
