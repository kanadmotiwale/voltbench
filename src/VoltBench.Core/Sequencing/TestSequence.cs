namespace VoltBench.Core.Sequencing;

public sealed record TestSequence
{
    public required string Name { get; init; }

    public string? Description { get; init; }

    public IReadOnlyList<TestStep> Steps { get; init; } = [];

    public override string ToString() => Name;
}
