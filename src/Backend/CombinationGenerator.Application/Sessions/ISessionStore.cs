using CombinationGenerator.Domain.Sessions;

namespace CombinationGenerator.Application.Sessions;

/// <summary>
/// "המגירה של הסימניות": איפה כל ה-Sessions שמורים, לפי מזהה.
/// זה רק ממשק — הבטחה למה המגירה יודעת לעשות. המימוש (בזיכרון היום, אולי Redis מחר)
/// נמצא ב-Infrastructure, ושום קוד אחר לא תלוי בו.
/// </summary>
/// <remarks>
/// כל הפעולות אסינכרוניות (Task), למרות שהמימוש בזיכרון מיידי:
/// אחסון חיצוני כמו Redis עובר ברשת, וממשק סינכרוני היה מחייב לשנות את כל הקוראים במעבר.
/// </remarks>
public interface ISessionStore
{
    /// <summary>
    /// שומר Session חדש עם מזהה חדש וגרסה 1, או מחזיר CapacityReached אם הגענו לתקרה.
    /// </summary>
    Task<CreateResult> CreateAsync(GeneratorSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// מחזיר את ה-Session לפי מזהה, או null אם אין כזה (לא קיים, נמחק או פג תוקף).
    /// </summary>
    Task<StoredSession?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// מחליף את ה-Session רק אם הגרסה השמורה עדיין <paramref name="expectedVersion"/>.
    /// הבדיקה וההחלפה הן פעולה אחת: אף בקשה אחרת לא יכולה להשתחל ביניהן.
    /// </summary>
    Task<ReplaceResult> ReplaceAsync(Guid id, long expectedVersion, GeneratorSession next, CancellationToken cancellationToken = default);

    /// <summary>מוחק את ה-Session (כפתור איפוס). מחזיר false אם לא היה כזה.</summary>
    Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default);
}
