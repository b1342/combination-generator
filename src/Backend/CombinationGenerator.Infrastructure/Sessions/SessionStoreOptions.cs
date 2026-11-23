namespace CombinationGenerator.Infrastructure.Sessions;

/// <summary>
/// הגדרות המגירה. נקראות מ-appsettings.json, מהקטע "SessionStore", כך שאפשר לשנות אותן בלי לגעת בקוד.
/// </summary>
public sealed class SessionStoreOptions
{
    /// <summary>שם הקטע ב-appsettings.json.</summary>
    public const string SectionName = "SessionStore";

    /// <summary>
    /// אחרי כמה דקות בלי שאף אחד נגע ב-Session הוא פג. השעון מתאפס בכל קריאה או החלפה.
    /// </summary>
    public int IdleTimeoutMinutes { get; set; } = 30;

    /// <summary>כמה Sessions לכל היותר בו-זמנית. הגנה מפני הצפה.</summary>
    public int MaxSessions { get; set; } = 1000;

    /// <summary>כל כמה שניות המנקה ברקע עובר ומוחק Sessions שפגו.</summary>
    public int CleanupIntervalSeconds { get; set; } = 60;

    /// <summary>IdleTimeoutMinutes כ-TimeSpan.</summary>
    public TimeSpan IdleTimeout => TimeSpan.FromMinutes(IdleTimeoutMinutes);

    /// <summary>CleanupIntervalSeconds כ-TimeSpan.</summary>
    public TimeSpan CleanupInterval => TimeSpan.FromSeconds(CleanupIntervalSeconds);

    /// <summary>
    /// הגבול העליון למרווח הניקוי: יום אחד. מעבר לזה אין היגיון בניקוי, ו-PeriodicTimer גם לא מקבל מרווחים ארוכים מאוד.
    /// </summary>
    public const int MaxCleanupIntervalSeconds = 86_400;

    /// <summary>כל הערכים חייבים להיות חיוביים, ומרווח הניקוי לכל היותר יום.</summary>
    public bool IsValid =>
        IdleTimeoutMinutes > 0 &&
        MaxSessions > 0 &&
        CleanupIntervalSeconds > 0 &&
        CleanupIntervalSeconds <= MaxCleanupIntervalSeconds;
}
