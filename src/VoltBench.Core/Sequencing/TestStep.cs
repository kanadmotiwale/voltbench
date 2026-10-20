namespace VoltBench.Core.Sequencing;

/// <summary>
/// One step of a test sequence. <see cref="Value"/> is volts, amps, 0/1 for output state,
/// or milliseconds for <see cref="StepAction.Wait"/>. Limits apply to measurement steps only;
/// a missing bound is open-ended.
/// </summary>
public sealed record TestStep
{
    public required string Name { get; init; }

    public StepAction Action { get; init; }

    public double Value { get; init; }

    public int Channel { get; init; } = 1;

    public double? Low { get; init; }

    public double? High { get; init; }
}
