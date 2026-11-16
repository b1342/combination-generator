namespace CombinationGenerator.Domain.Permutations;

/// <summary>
/// מחשב ישירות את התמורה שנמצאת באינדקס מסוים בסדר המילוני, בלי לעבור על התמורות שלפניה.
/// </summary>
public static class PermutationUnranker
{
    /// <summary>
    /// מחזיר את התמורה של 1..n שנמצאת באינדקס <paramref name="index"/> (סופרים מ-0).
    /// לדוגמה: n=3, index=3 מחזיר [2,3,1].
    /// </summary>
    /// <remarks>
    /// n סיבובים של חילוק ושארית, ללא קשר לגודל האינדקס.
    /// זמן O(n²) בגלל ההסרה מהרשימה; עבור n≤20 זה זניח.
    /// </remarks>
    public static int[] Unrank(int n, long index)
    {
        if (n < 1 || n > FactorialCalculator.MaxN)
        {
            throw new ArgumentOutOfRangeException(nameof(n), n, $"n must be between 1 and {FactorialCalculator.MaxN}.");
        }

        long total = FactorialCalculator.Of(n);
        if (index < 0 || index >= total)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, $"index must be between 0 and {total - 1}.");
        }

        // הספרות שעוד לא נבחרו, מסודרות מהקטנה לגדולה: [1, 2, ..., n]
        var available = new List<int>(n);
        for (int digit = 1; digit <= n; digit++)
        {
            available.Add(digit);
        }

        var result = new int[n];
        long remainingIndex = index;

        for (int position = 0; position < n; position++)
        {
            // כמה ספרות נשארו לבחירה, ומה גודל כל קבוצה
            int remainingCount = n - position;
            long blockSize = FactorialCalculator.Of(remainingCount - 1);

            // באיזו קבוצה אנחנו, ואיפה בתוך הקבוצה
            long choice = remainingIndex / blockSize;
            remainingIndex %= blockSize;

            // choice תמיד קטן מ-remainingCount, ולכן ההמרה ל-int בטוחה
            int choiceIndex = (int)choice;
            result[position] = available[choiceIndex];
            available.RemoveAt(choiceIndex);
        }

        return result;
    }
}
