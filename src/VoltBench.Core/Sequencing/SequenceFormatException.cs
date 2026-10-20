namespace VoltBench.Core.Sequencing;

public sealed class SequenceFormatException : Exception
{
    public SequenceFormatException(string message) : base(message) { }

    public SequenceFormatException(string message, Exception inner) : base(message, inner) { }
}
