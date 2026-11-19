using CombinationGenerator.Domain.Permutations;

namespace CombinationGenerator.Domain.Tests.Permutations;

public class PreviousPermutationTests
{
    [Fact]
    public void N3_ProducesAssignmentSequenceBackwards()
    {
        int[][] expected =
        [
            [3, 2, 1], [3, 1, 2], [2, 3, 1], [2, 1, 3], [1, 3, 2], [1, 2, 3],
        ];

        int[] current = [3, 2, 1];
        var produced = new List<int[]> { (int[])current.Clone() };
        while (PreviousPermutation.TryRetreat(current))
        {
            produced.Add((int[])current.Clone());
        }

        Assert.Equal(expected, produced);
    }

    [Fact]
    public void FirstPermutation_ReturnsFalse_AndDoesNotModifyArray()
    {
        int[] first = [1, 2, 3];

        Assert.False(PreviousPermutation.TryRetreat(first));
        Assert.Equal(new[] { 1, 2, 3 }, first);
    }

    [Fact]
    public void N1_HasNoPreviousPermutation()
    {
        int[] single = [1];

        Assert.False(PreviousPermutation.TryRetreat(single));
        Assert.Equal(new[] { 1 }, single);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(6)]
    public void IsExactInverseOfNextPermutation(int n)
    {
        // לכל תמורה: קדימה ואז אחורה מחזיר בדיוק לאותה תמורה
        int[] current = Enumerable.Range(1, n).ToArray();

        do
        {
            int[] copy = (int[])current.Clone();
            if (NextPermutation.TryAdvance(copy))
            {
                Assert.True(PreviousPermutation.TryRetreat(copy));
                Assert.Equal(current, copy);
            }
        }
        while (NextPermutation.TryAdvance(current));
    }
}
