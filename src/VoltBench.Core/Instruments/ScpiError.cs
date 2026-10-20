namespace VoltBench.Core.Instruments;

/// <summary>An entry from the instrument error queue (<c>SYST:ERR?</c>).</summary>
public sealed record ScpiError(int Code, string Message)
{
    public bool IsError => Code != 0;

    public override string ToString() => $"{Code}: {Message}";
}
