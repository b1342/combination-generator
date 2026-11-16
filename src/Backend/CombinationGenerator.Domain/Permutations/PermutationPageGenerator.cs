using CombinationGenerator.Domain.Pagination;

namespace CombinationGenerator.Domain.Permutations;

/// <summary>
/// בונה את השורות של עמוד אחד: Unrank לשורה הראשונה, ו-NextPermutation לכל השאר.
/// </summary>
public static class PermutationPageGenerator
{
    /// <summary>
    /// מחזיר את התמורות של העמוד. כל שורה היא מערך נפרד, כך ששינוי בשורה אחת לא משפיע על אחרת.
    /// </summary>
    /// <remarks>
    /// זמן O(n² + k·n) לעמוד של k שורות. לא תלוי במספר העמוד: עמוד מיליון עולה כמו עמוד 1.
    /// </remarks>
    public static IReadOnlyList<int[]> Generate(int n, PageWindow window)
    {
        if (window.Count < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(window), "The page window must contain at least one row.");
        }

        // קפיצה ישירה לשורה הראשונה בעמוד
        int[] current = PermutationUnranker.Unrank(n, window.StartIndex);

        // שומרים עותק, כי NextPermutation ימשיך לשנות את current
        var rows = new List<int[]>(window.Count) { (int[])current.Clone() };

        for (int row = 1; row < window.Count; row++)
        {
            if (!NextPermutation.TryAdvance(current))
            {
                // לא אמור לקרות אם החלון חושב ב-PageCalculator; אם כן, זה באג ולא "עמוד קצר"
                throw new InvalidOperationException("The page window extends past the last permutation.");
            }

            rows.Add((int[])current.Clone());
        }

        return rows;
    }
}
