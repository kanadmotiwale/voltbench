using VoltBench.Core.Sequencing;

namespace VoltBench.Tests;

public class LimitCheckerTests
{
    [Theory]
    [InlineData(5.0, 4.9, 5.1, true)]
    [InlineData(4.9, 4.9, 5.1, true)]
    [InlineData(5.1, 4.9, 5.1, true)]
    [InlineData(4.89, 4.9, 5.1, false)]
    [InlineData(5.11, 4.9, 5.1, false)]
    [InlineData(100, null, 5.1, false)]
    [InlineData(-100, null, 5.1, true)]
    [InlineData(100, 4.9, null, true)]
    [InlineData(0, 4.9, null, false)]
    [InlineData(123, null, null, true)]
    public void IsWithin_AppliesInclusiveBounds(double value, double? low, double? high, bool expected) =>
        Assert.Equal(expected, LimitChecker.IsWithin(value, low, high));

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void IsWithin_FailsNonFiniteEvenWithOpenLimits(double value) =>
        Assert.False(LimitChecker.IsWithin(value, null, null));
}
