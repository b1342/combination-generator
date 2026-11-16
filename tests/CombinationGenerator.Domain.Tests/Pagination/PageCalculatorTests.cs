using CombinationGenerator.Domain.Pagination;
using CombinationGenerator.Domain.Permutations;

namespace CombinationGenerator.Domain.Tests.Pagination;

public class PageCalculatorTests
{
    private static readonly long Factorial20 = FactorialCalculator.Of(20);

    // ---------- CountPages ----------

    [Theory]
    [InlineData(6L, 2L, 2, 2L)]   // הדוגמה: 4 שנשארו, 2 בעמוד
    [InlineData(6L, 2L, 3, 2L)]   // 4 שנשארו, 3 בעמוד: עמוד מלא ועמוד של שורה אחת
    [InlineData(6L, 0L, 1, 6L)]
    [InlineData(6L, 5L, 10, 1L)]  // נשארה תמורה אחת
    public void CountPages_RoundsUp(long total, long anchor, int pageSize, long expected)
    {
        Assert.Equal(expected, PageCalculator.CountPages(total, anchor, pageSize));
    }

    [Fact]
    public void CountPages_N20_PageSize1_IsExactly20Factorial()
    {
        Assert.Equal(Factorial20, PageCalculator.CountPages(Factorial20, 0, 1));
    }

    [Theory]
    [InlineData(0L, 0L, 1)]   // אין תמורות
    [InlineData(6L, -1L, 1)]  // עוגן שלילי
    [InlineData(6L, 6L, 1)]   // עוגן אחרי הסוף: לא נשאר כלום
    [InlineData(6L, 0L, 0)]   // גודל עמוד 0
    public void CountPages_InvalidArguments_Throw(long total, long anchor, int pageSize)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PageCalculator.CountPages(total, anchor, pageSize));
    }

    // ---------- TryGetWindow ----------

    [Fact]
    public void Window_AssignmentExample_N3_Anchor2_Size2()
    {
        Assert.True(PageCalculator.TryGetWindow(6, 2, 2, 1, out var page1));
        Assert.Equal(new PageWindow(1, 2, 2, 2), page1);
        Assert.Equal(3, page1.LastIndex); // "חזור" מעמוד 1 = אינדקס 3

        Assert.True(PageCalculator.TryGetWindow(6, 2, 2, 2, out var page2));
        Assert.Equal(new PageWindow(2, 2, 4, 2), page2);
    }

    [Fact]
    public void Window_LastPageCanBeShort()
    {
        Assert.True(PageCalculator.TryGetWindow(6, 2, 3, 2, out var lastPage));
        Assert.Equal(5, lastPage.StartIndex);
        Assert.Equal(1, lastPage.Count);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(3L)]
    [InlineData(-5L)]
    public void Window_PageOutsideRange_ReturnsFalse(long pageNumber)
    {
        Assert.False(PageCalculator.TryGetWindow(6, 2, 2, pageNumber, out _));
    }

    [Fact]
    public void Window_N20_LastPage_IsComputedDirectly()
    {
        Assert.True(PageCalculator.TryGetWindow(Factorial20, 0, 1, Factorial20, out var last));
        Assert.Equal(Factorial20 - 1, last.StartIndex);
        Assert.Equal(1, last.Count);
    }

    [Fact]
    public void Window_N20_PageJustBeforeLast_IsExact()
    {
        // מספר שלא נשמר במדויק ב-double: מוודא שאין עיגול
        Assert.True(PageCalculator.TryGetWindow(Factorial20, 0, 1, 2_432_902_008_176_639_999L, out var window));
        Assert.Equal(2_432_902_008_176_639_998L, window.StartIndex);
    }

    [Fact]
    public void Window_HugePageNumber_ReturnsFalse_WithoutOverflow()
    {
        Assert.False(PageCalculator.TryGetWindow(Factorial20, 0, 200, long.MaxValue, out _));
    }

    [Theory]
    [InlineData(1L)]
    [InlineData(2L)]
    [InlineData(6L)]
    [InlineData(24L)]
    public void Windows_CoverRemainderExactly_WithoutGapsOrOverlap(long total)
    {
        // לכל עוגן ולכל גודל: העמודים מכסים בדיוק את מה שנשאר, ברצף
        for (long anchor = 0; anchor < total; anchor++)
        {
            for (int pageSize = 1; pageSize <= 30; pageSize++)
            {
                long pages = PageCalculator.CountPages(total, anchor, pageSize);
                long expectedStart = anchor;

                for (long page = 1; page <= pages; page++)
                {
                    Assert.True(PageCalculator.TryGetWindow(total, anchor, pageSize, page, out var window));
                    Assert.Equal(expectedStart, window.StartIndex);
                    Assert.InRange(window.Count, 1, pageSize);
                    expectedStart += window.Count;
                }

                Assert.Equal(total, expectedStart);
                Assert.False(PageCalculator.TryGetWindow(total, anchor, pageSize, pages + 1, out _));
            }
        }
    }
}
