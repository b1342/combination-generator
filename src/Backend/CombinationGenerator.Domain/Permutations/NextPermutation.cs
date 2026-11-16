namespace CombinationGenerator.Domain.Permutations;

/// <summary>
/// מקדם תמורה לתמורה העוקבת בסדר מילוני (לקסיקוגרפי).
/// </summary>
public static class NextPermutation
{
    /// <summary>
    /// משנה את <paramref name="items"/> במקום לתמורה הבאה ומחזיר true.
    /// אם זו התמורה האחרונה (סדר יורד, למשל [3,2,1]) מחזיר false והמערך לא משתנה כלל.
    /// </summary>
    /// <remarks>
    /// זמן O(n) במקרה הגרוע, זיכרון נוסף O(1).
    /// מקבל Span כדי שהקורא יחליט על איזה עותק לעבוד; לעולם לא על המערך השמור במצב.
    /// </remarks>
    public static bool TryAdvance(Span<int> items)
    {
        // 1. מחפשים מימין את i הראשון שבו items[i] < items[i+1].
        //    כל מה שמימין ל-i הוא סדר יורד, כלומר "מוצה" ואין לו המשך משלו.
        int i = items.Length - 2;
        while (i >= 0 && items[i] >= items[i + 1])
        {
            i--;
        }

        // אין i כזה: כל המערך יורד, זו התמורה האחרונה. יוצאים לפני כל שינוי.
        if (i < 0)
        {
            return false;
        }

        // 2. מחפשים מימין את j הראשון שבו items[j] > items[i].
        //    כי הסיפא יורדת, זה המספר הקטן ביותר בסיפא שגדול מ-items[i].
        int j = items.Length - 1;
        while (items[j] <= items[i])
        {
            j--;
        }

        // 3. מחליפים: ה"ספרה" במקום i עולה במינימום האפשרי.
        (items[i], items[j]) = (items[j], items[i]);

        // 4. הסיפא עדיין יורדת. הופכים אותה לעולה, כדי שתהיה הקטנה ביותר האפשרית.
        items[(i + 1)..].Reverse();

        return true;
    }
}
