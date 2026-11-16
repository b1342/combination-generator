namespace CombinationGenerator.Domain.Permutations;

/// <summary>
/// טבלת עצרות 0! עד 20!, מחושבת פעם אחת בטעינת המחלקה.
/// זו לא רשימת תמורות: רק 21 מספרים, ולכן היא לא מפרה את האיסור לחשב מראש.
/// </summary>
public static class FactorialCalculator
{
    /// <summary>ה-N הגדול ביותר שהמערכת תומכת בו. 20! עוד נכנס ב-long, 21! כבר לא.</summary>
    public const int MaxN = 20;

    private static readonly long[] Table = BuildTable();

    /// <summary>מחזיר את n! עבור n בין 0 ל-20.</summary>
    public static long Of(int n)
    {
        if (n < 0 || n > MaxN)
        {
            throw new ArgumentOutOfRangeException(nameof(n), n, $"n must be between 0 and {MaxN}.");
        }

        return Table[n];
    }

    private static long[] BuildTable()
    {
        var table = new long[MaxN + 1];
        table[0] = 1; // 0! = 1. נחוץ ל-Unrank: בלוק בגודל 0! בסוף החישוב.

        for (int i = 1; i <= MaxN; i++)
        {
            // checked: אם מישהו יגדיל את MaxN בטעות, נקבל חריגה ולא מספר שגוי בשקט.
            table[i] = checked(table[i - 1] * i);
        }

        return table;
    }
}
