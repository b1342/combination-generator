using CombinationGenerator.Domain.Permutations;

namespace CombinationGenerator.Domain.Tests.Permutations;

public class FactorialCalculatorTests
{
    [Theory]
    [InlineData(0, 1L)]
    [InlineData(1, 1L)]
    [InlineData(3, 6L)]
    [InlineData(10, 3_628_800L)]
    [InlineData(20, 2_432_902_008_176_640_000L)]
    public void Of_ReturnsExactFactorial(int n, long expected)
    {
        Assert.Equal(expected, FactorialCalculator.Of(n));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(21)]
    public void Of_OutsideSupportedRange_Throws(int n)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FactorialCalculator.Of(n));
    }
}
