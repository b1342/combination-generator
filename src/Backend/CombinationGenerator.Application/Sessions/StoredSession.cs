using CombinationGenerator.Domain.Sessions;

namespace CombinationGenerator.Application.Sessions;

/// <summary>
/// סימנייה כפי שהיא שמורה ב-Store: המזהה, מספר הגרסה וה-Session עצמו.
/// ה-Domain לא יודע על מזהים וגרסאות; אלה מתווספים רק כאן, ברגע השמירה.
/// </summary>
/// <param name="Id">המזהה הייחודי של ה-Session. נשלח לדפדפן ומגיע בכל בקשה.</param>
/// <param name="Version">עולה ב-1 בכל החלפה מוצלחת. מתחיל ב-1.</param>
/// <param name="Session">המצב עצמו, מה-Domain.</param>
public sealed record StoredSession(Guid Id, long Version, GeneratorSession Session);
