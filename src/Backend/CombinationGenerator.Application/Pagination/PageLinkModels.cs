namespace CombinationGenerator.Application.Pagination;

/// <summary>פריט אחד בשורת מספרי הדפים שהשרת מחזיר ללקוח.</summary>
public abstract record PageToken;

/// <summary>מספר דף שאפשר ללחוץ עליו.</summary>
public sealed record PageNumberToken(long Number, bool IsCurrent) : PageToken;

/// <summary>"…" — דפים שדילגנו עליהם. לא לחיץ.</summary>
public sealed record EllipsisToken : PageToken;
