using System.Collections.Immutable;

namespace CombinationGenerator.Domain.Sessions;

/// <summary>
/// הקומבינציה האחרונה שהוצגה במצב "הבא": האינדקס שלה והתמורה עצמה.
/// שומרים את שניהם: התמורה כדי להפעיל עליה NextPermutation, והאינדקס להצגת המספר הסידורי.
/// </summary>
/// <remarks>
/// class ולא record: השוואת record משווה שדה-שדה, ו-ImmutableArray נחשב "שווה" רק אם זה אותו מערך בזיכרון,
/// כך ששני Cursor עם אותו תוכן היו נחשבים שונים. עדיף לא להציע השוואה מטעה בכלל.
/// </remarks>
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
