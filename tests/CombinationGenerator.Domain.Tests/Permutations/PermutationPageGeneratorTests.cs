using CombinationGenerator.Domain.Pagination;
using CombinationGenerator.Domain.Permutations;

namespace CombinationGenerator.Domain.Tests.Permutations;

public class PermutationPageGeneratorTests
{
    [Fact]
    public void AssignmentExample_N3_TwoPagesOfTwo()
    {
        PageCalculator.TryGetWindow(6, 2, 2, 1, out var page1);
        PageCalculator.TryGetWindow(6, 2, 2, 2, out var page2);

        Assert.Equal(new[] { new[] { 2, 1, 3 }, new[] { 2, 3, 1 } }, PermutationPageGenerator.Generate(3, page1));
        Assert.Equal(new[] { new[] { 3, 1, 2 }, new[] { 3, 2, 1 } }, PermutationPageGenerator.Generate(3, page2));
    }

    [Fact]
    public void ShortLastPage_ContainsOnlyTheRemainingRow()
    {
        PageCalculator.TryGetWindow(6, 2, 3, 2, out var lastPage);

        var rows = PermutationPageGenerator.Generate(3, lastPage);

        Assert.Equal(new[] { new[] { 3, 2, 1 } }, rows);
    }

    [Fact]
    public void Rows_AreIndependentCopies()
    {
        PageCalculator.TryGetWindow(6, 0, 3, 1, out var window);
        var rows = PermutationPageGenerator.Generate(3, window);

        rows[0][0] = 99;

        Assert.Equal(new[] { 1, 3, 2 }, rows[1]);
        Assert.Equal(new[] { 2, 1, 3 }, rows[2]);
    }

    [Theory]
    [InlineData(0L, 7)]
    [InlineData(37L, 10)]
    [InlineData(115L, 5)]   // מסתיים בדיוק בתמורה האחרונה של n=5
    public void EveryRow_EqualsUnrankOfItsIndex(long startIndex, int count)
    {
        var rows = PermutationPageGenerator.Generate(5, new PageWindow(1, 1, startIndex, count));

        for (int i = 0; i < count; i++)
        {
            Assert.Equal(PermutationUnranker.Unrank(5, startIndex + i), rows[i]);
        }
    }

    [Fact]
    public void N20_LastPage_EndsWithDescendingPermutation()
    {
        long total = FactorialCalculator.Of(20);
        long pages = PageCalculator.CountPages(total, 0, 3);
        PageCalculator.TryGetWindow(total, 0, 3, pages, out var lastPage);

        var rows = PermutationPageGenerator.Generate(20, lastPage);

        Assert.Equal(Enumerable.Range(1, 20).Reverse(), rows[^1]);
    }

    [Fact]
    public void WindowPastTheEnd_IsABug_NotAShortPage()
    {
        var invalid = new PageWindow(1, 1, StartIndex: 4, Count: 5); // n=3 נגמר באינדקס 5

        Assert.Throws<InvalidOperationException>(() => PermutationPageGenerator.Generate(3, invalid));
    }
}
