namespace VoltBench.Core.Instruments;

public interface IInstrument
{
    /// <summary>Returns the <c>*IDN?</c> identification string.</summary>
    string Identify();

    /// <summary>Sends <c>*RST</c>.</summary>
    void Reset();

    /// <summary>Drains the error queue and returns every pending error.</summary>
    IReadOnlyList<ScpiError> CheckErrors();
}
