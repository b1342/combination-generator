namespace CombinationGenerator.Domain.Permutations;

/// <summary>
/// מחזיר תמורה לתמורה הקודמת בסדר מילוני. תמונת מראה של NextPermutation: כל השוואה הפוכה.
/// </summary>
public static class PreviousPermutation
{
    /// <summary>
    /// משנה את <paramref name="items"/> במקום לתמורה הקודמת ומחזיר true.
    /// אם זו התמורה הראשונה (סדר עולה, למשל [1,2,3]) מחזיר false והמערך לא משתנה כלל.
    /// </summary>
    /// <remarks>זמן O(n) במקרה הגרוע, זיכרון נוסף O(1).</remarks>
    public static bool TryRetreat(Span<int> items)
    {
        // 1. מחפשים מימין את i הראשון שבו items[i] > items[i+1].
        //    כל מה שמימין ל-i הוא סדר עולה, כלומר "בהתחלה" ואין לו תמורה קודמת משלו.
        int i = items.Length - 2;
        while (i >= 0 && items[i] <= items[i + 1])
        {
            i--;
        }

        // אין i כזה: כל המערך עולה, זו התמורה הראשונה. יוצאים לפני כל שינוי.
        if (i < 0)
        {
            return false;
        }

        // 2. מחפשים מימין את j הראשון שבו items[j] < items[i].
        //    כי הסיפא עולה, זה המספר הגדול ביותר בסיפא שקטן מ-items[i].
        int j = items.Length - 1;
        while (items[j] >= items[i])
        {
            j--;
        }

        // 3. מחליפים: ה"ספרה" במקום i יורדת במינימום האפשרי.
        (items[i], items[j]) = (items[j], items[i]);

        // 4. הסיפא עדיין עולה. הופכים אותה ליורדת, כדי שתהיה הגדולה ביותר האפשרית.
        items[(i + 1)..].Reverse();

        return true;
    }
}
