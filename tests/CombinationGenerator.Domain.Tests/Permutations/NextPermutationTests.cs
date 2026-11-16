using CombinationGenerator.Domain.Permutations;

namespace CombinationGenerator.Domain.Tests.Permutations;

public class NextPermutationTests
{
    [Fact]
    public void N3_ProducesExactSequenceFromAssignment()
    {
        // הרצף מתוך המטלה עצמה.
        int[][] expected =
        [
            [1, 2, 3], [1, 3, 2], [2, 1, 3], [2, 3, 1], [3, 1, 2], [3, 2, 1],
        ];

        int[] current = [1, 2, 3];
        var produced = new List<int[]> { (int[])current.Clone() };
        while (NextPermutation.TryAdvance(current))
        {
            produced.Add((int[])current.Clone());
        }

        Assert.Equal(expected, produced);
    }

    [Fact]
    public void LastPermutation_ReturnsFalse_AndDoesNotModifyArray()
    {
        int[] last = [3, 2, 1];

        bool advanced = NextPermutation.TryAdvance(last);

        Assert.False(advanced);
        Assert.Equal(new[] { 3, 2, 1 }, last); // לא "מתאפס" לתמורה הראשונה
    }

    [Fact]
    public void N1_HasNoNextPermutation()
    {
        int[] single = [1];

        Assert.False(NextPermutation.TryAdvance(single));
        Assert.Equal(new[] { 1 }, single);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(7)]
    public void FullWalk_VisitsExactlyNFactorialUniquePermutations_InIncreasingOrder(int n)
    {
        // בודקים תכונות ולא רק השוואה לרשימה: כמות, ייחודיות, וכל תמורה גדולה מקודמתה.
        int[] current = Enumerable.Range(1, n).ToArray();
        var seen = new HashSet<string> { string.Join(",", current) };
        long count = 1;
        int[] previous = (int[])current.Clone();

        while (NextPermutation.TryAdvance(current))
        {
            Assert.True(IsLexicographicallyGreater(current, previous));
            Assert.True(seen.Add(string.Join(",", current)));
            previous = (int[])current.Clone();
            count++;
        }

        Assert.Equal(FactorialCalculator.Of(n), count);
        Assert.Equal(Enumerable.Range(1, n).Reverse(), current); // נעצר בתמורה האחרונה
    }

    private static bool IsLexicographicallyGreater(int[] a, int[] b)
    {
        for (int k = 0; k < a.Length; k++)
        {
            if (a[k] != b[k])
            {
                return a[k] > b[k];
            }
        }

        return false;
    }
}
