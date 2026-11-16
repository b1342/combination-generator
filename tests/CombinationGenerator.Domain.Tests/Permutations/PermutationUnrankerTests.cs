using CombinationGenerator.Domain.Permutations;

namespace CombinationGenerator.Domain.Tests.Permutations;

public class PermutationUnrankerTests
{
    [Theory]
    [InlineData(0L, new[] { 1, 2, 3 })]
    [InlineData(1L, new[] { 1, 3, 2 })]
    [InlineData(2L, new[] { 2, 1, 3 })]
    [InlineData(3L, new[] { 2, 3, 1 })]
    [InlineData(4L, new[] { 3, 1, 2 })]
    [InlineData(5L, new[] { 3, 2, 1 })]
    public void N3_EveryIndex_MatchesAssignmentSequence(long index, int[] expected)
    {
        Assert.Equal(expected, PermutationUnranker.Unrank(3, index));
    }

    [Fact]
    public void N4_Index10_IsPermutationNumber11()
    {
        // הדוגמה מההמחשה: סידור מספר 11 = אינדקס 10
        Assert.Equal(new[] { 2, 4, 1, 3 }, PermutationUnranker.Unrank(4, 10));
    }

    [Fact]
    public void N20_FirstAndLast_AreComputedDirectly()
    {
        int[] ascending = Enumerable.Range(1, 20).ToArray();
        int[] descending = Enumerable.Range(1, 20).Reverse().ToArray();
        long last = FactorialCalculator.Of(20) - 1;

        Assert.Equal(ascending, PermutationUnranker.Unrank(20, 0));
        Assert.Equal(descending, PermutationUnranker.Unrank(20, last));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(7)]
    public void MatchesNextPermutationWalk_ForEveryIndex(int n)
    {
        // שני האלגוריתמים חייבים להסכים על כל הרצף
        int[] walk = Enumerable.Range(1, n).ToArray();
        long index = 0;

        do
        {
            Assert.Equal(walk, PermutationUnranker.Unrank(n, index));
            index++;
        }
        while (NextPermutation.TryAdvance(walk));

        Assert.Equal(FactorialCalculator.Of(n), index);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(1_234_567_890_123_456_789L)]
    [InlineData(2_432_902_008_176_639_998L)] // 20! - 2
    public void N20_NextOfUnrank_EqualsUnrankOfNextIndex(long index)
    {
        // בודקים אינדקסים עצומים שאי אפשר להגיע אליהם בהליכה
        int[] current = PermutationUnranker.Unrank(20, index);

        Assert.True(NextPermutation.TryAdvance(current));
        Assert.Equal(PermutationUnranker.Unrank(20, index + 1), current);
    }

    [Theory]
    [InlineData(3, -1L)]
    [InlineData(3, 6L)]   // 3! = 6, האינדקס האחרון הוא 5
    [InlineData(0, 0L)]
    [InlineData(21, 0L)]
    public void InvalidInput_Throws(int n, long index)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PermutationUnranker.Unrank(n, index));
    }
}
