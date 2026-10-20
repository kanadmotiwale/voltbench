namespace VoltBench.Core.Instruments;

/// <summary>Raised when an instrument cannot be reached or returns an unusable response.</summary>
public class InstrumentException : Exception
{
    public InstrumentException(string message) : base(message) { }

    public InstrumentException(string message, Exception inner) : base(message, inner) { }
}
