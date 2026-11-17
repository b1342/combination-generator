namespace CombinationGenerator.Application.Sessions;

/// <summary>התוצאה של ניסיון ליצור Session חדש.</summary>
public enum CreateStatus
{
    /// <summary>נוצר, עם מזהה חדש וגרסה 1.</summary>
    Created,

    /// <summary>הגענו לתקרת ה-Sessions. שום דבר לא נוצר.</summary>
    CapacityReached,
}

/// <summary>תוצאת יצירה. Stored מכיל את ה-Session החדש כשהסטטוס Created, ואחרת null.</summary>
public readonly record struct CreateResult(CreateStatus Status, StoredSession? Stored);

/// <summary>התוצאה של ניסיון החלפה.</summary>
public enum ReplaceStatus
{
    /// <summary>הוחלף. הגרסה עלתה ב-1.</summary>
    Replaced,

    /// <summary>מישהו אחר החליף קודם: הגרסה השמורה כבר לא זו שקראנו. שום דבר לא השתנה.</summary>
    VersionConflict,

    /// <summary>אין Session עם המזהה הזה (לא היה, נמחק או פג תוקף).</summary>
    NotFound,
}

/// <summary>תוצאת החלפה. Stored מכיל את הסימנייה החדשה כשהסטטוס Replaced, ואחרת null.</summary>
public readonly record struct ReplaceResult(ReplaceStatus Status, StoredSession? Stored);
