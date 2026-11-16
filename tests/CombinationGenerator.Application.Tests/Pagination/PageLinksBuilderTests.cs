using CombinationGenerator.Application.Pagination;
using CombinationGenerator.Domain.Permutations;

namespace CombinationGenerator.Application.Tests.Pagination;

public class PageLinksBuilderTests
{
    [Theory]
    [InlineData(1L, 1L, "[1]")]
    [InlineData(1L, 2L, "[1] 2")]
    [InlineData(1L, 100L, "[1] 2 3 … 100")]
    [InlineData(4L, 100L, "1 2 3 [4] 5 6 … 100")]
    [InlineData(5L, 100L, "1 2 3 4 [5] 6 7 … 100")]
    [InlineData(50L, 100L, "1 … 48 49 [50] 51 52 … 100")]
    [InlineData(97L, 100L, "1 … 95 96 [97] 98 99 100")]
    [InlineData(5L, 9L, "1 2 3 4 [5] 6 7 8 9")]
    public void MatchesSpecificationTable(long current, long total, string expected)
    {
        Assert.Equal(expected, Format(PageLinksBuilder.Build(current, total)));
    }

    [Fact]
    public void N20_LastPage_DoesNotOverflow()
    {
        long factorial20 = FactorialCalculator.Of(20);

        Assert.Equal(
            "1 … 2432902008176639998 2432902008176639999 [2432902008176640000]",
            Format(PageLinksBuilder.Build(factorial20, factorial20)));
    }

    [Fact]
    public void GeneralRules_HoldForEveryWindow()
    {
        for (long total = 1; total <= 60; total++)
        {
            for (long current = 1; current <= total; current++)
            {
                var tokens = PageLinksBuilder.Build(current, total);

                Assert.True(tokens.Count <= 9);
                Assert.Equal(1, Assert.IsType<PageNumberToken>(tokens[0]).Number);
                Assert.Equal(total, Assert.IsType<PageNumberToken>(tokens[^1]).Number);

                var currentToken = Assert.Single(tokens.OfType<PageNumberToken>(), t => t.IsCurrent);
                Assert.Equal(current, currentToken.Number);

                for (int i = 1; i < tokens.Count; i++)
                {
                    if (tokens[i] is PageNumberToken next && tokens[i - 1] is PageNumberToken prev)
                    {
                        Assert.Equal(prev.Number + 1, next.Number); // מספרים סמוכים בלי חור
                    }
                    else if (tokens[i] is EllipsisToken)
                    {
                        // "…" תמיד בין שני מספרים, ומסתיר לפחות שני דפים
                        var before = Assert.IsType<PageNumberToken>(tokens[i - 1]);
                        var after = Assert.IsType<PageNumberToken>(tokens[i + 1]);
                        Assert.True(after.Number - before.Number >= 3);
                    }
                }
            }
        }
    }

    [Theory]
    [InlineData(0L, 5L)]
    [InlineData(6L, 5L)]
    [InlineData(1L, 0L)]
    public void InvalidArguments_Throw(long current, long total)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PageLinksBuilder.Build(current, total));
    }

    private static string Format(IReadOnlyList<PageToken> tokens) =>
        string.Join(" ", tokens.Select(token => token switch
        {
            PageNumberToken { IsCurrent: true } page => $"[{page.Number}]",
            PageNumberToken page => page.Number.ToString(),
            EllipsisToken => "…",
            _ => "?",
        }));
}
