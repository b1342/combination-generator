using System.Collections.Immutable;

namespace CombinationGenerator.Domain.Sessions;

/// <summary>
/// הקומבינציה האחרונה שהוצגה במצב "הבא": האינדקס שלה והתמורה עצמה.
/// שומרים את שניהם: התמורה כדי להפעיל עליה NextPermutation, והאינדקס להצגת המספר הסידורי.
/// </summary>
public sealed class Cursor
{
    private Cursor(long index, ImmutableArray<int> permutation)
    {
        Index = index;
        Permutation = permutation;
    }

    /// <summary>האינדקס של התמורה, מ-0. המספר שמוצג למשתמש הוא Index + 1.</summary>
    public long Index { get; }

    /// <summary>התמורה עצמה. ImmutableArray: אי אפשר לשנות אותה אחרי שנוצרה.</summary>
    public ImmutableArray<int> Permutation { get; }

    /// <summary>
    /// יוצר Cursor מעותק של המערך. שינוי מאוחר של המערך המקורי לא ישפיע על ה-Cursor.
    /// </summary>
    public static Cursor Create(long index, int[] permutation)
    {
        ArgumentNullException.ThrowIfNull(permutation);

        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "index must be non-negative.");
        }

        if (permutation.Length == 0)
        {
            throw new ArgumentException("permutation must not be empty.", nameof(permutation));
        }

        // ToImmutableArray מעתיק את הערכים למערך חדש שאף אחד אחר לא מחזיק
        return new Cursor(index, permutation.ToImmutableArray());
    }
}

/// <summary>איפה המשתמש נמצא: תמיד אחד משני המצבים, NextProgress או AllProgress.</summary>
public abstract record Progress;

/// <summary>מצב "הבא". Cursor הוא null לפני הלחיצה הראשונה.</summary>
public sealed record NextProgress(Cursor? Cursor) : Progress;

/// <summary>
/// מצב "כל הקומבינציות": מאיפה התחלנו, גודל העמוד ובאיזה עמוד אנחנו.
/// אין כאן Cursor ואין שורות; את השורות מחשבים מחדש מהמספרים האלה.
/// </summary>
public sealed record AllProgress(long StartIndex, int PageSize, long PageNumber) : Progress;
