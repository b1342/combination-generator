namespace CombinationGenerator.Domain.Sessions;

/// <summary>איפה המשתמש נמצא: תמיד אחד משני המצבים, NextProgress או AllProgress.</summary>
public abstract record Progress;

/// <summary>מצב "הבא". Cursor הוא null לפני הלחיצה הראשונה.</summary>
public sealed record NextProgress(Cursor? Cursor) : Progress;

/// <summary>
/// מצב "כל הקומבינציות": מאיפה התחלנו, גודל העמוד ובאיזה עמוד אנחנו.
/// אין כאן Cursor ואין שורות; את השורות מחשבים מחדש מהמספרים האלה.
/// </summary>
public sealed record AllProgress(long StartIndex, int PageSize, long PageNumber) : Progress;
