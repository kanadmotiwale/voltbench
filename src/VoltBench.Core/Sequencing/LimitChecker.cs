namespace VoltBench.Core.Sequencing;

public static class LimitChecker
{
    /// <summary>Inclusive range check; null bounds are open and non-finite values always fail.</summary>
    public static bool IsWithin(double value, double? low, double? high)
    {
        if (!double.IsFinite(value))
        {
            return false;
        }

        return (low is null || value >= low) && (high is null || value <= high);
    }
}
