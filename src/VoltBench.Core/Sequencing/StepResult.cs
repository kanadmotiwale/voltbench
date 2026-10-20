namespace VoltBench.Core.Sequencing;

public enum StepOutcome
{
    Pass,
    Fail,
    Error,
    Skipped,
}

public enum SequenceOutcome
{
    Passed,
    Failed,
    Errored,
    Aborted,
}

/// <param name="Setpoint">Last commanded voltage for voltage measurements, last current limit for current measurements.</param>
public sealed record StepResult(
    int Index,
    TestStep Step,
    StepOutcome Outcome,
    DateTimeOffset Timestamp,
    double? Setpoint = null,
    double? Measured = null,
    string? Message = null)
{
    public string Unit => Step.Action.Unit();
}

public sealed record SequenceResult(
    string SequenceName,
    SequenceOutcome Outcome,
    IReadOnlyList<StepResult> Steps,
    DateTimeOffset Started,
    TimeSpan Duration,
    string? LogFile = null);

/// <summary>Reported before a step starts (<see cref="Result"/> null) and again when it finishes.</summary>
public sealed record StepProgress(int Index, int Total, TestStep Step, StepResult? Result);
