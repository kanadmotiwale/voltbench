using VoltBench.Core.Instruments;

namespace VoltBench.Instruments.Scpi;

/// <summary>Multi-channel bench supply driven with the standard SCPI command set.</summary>
public sealed class ScpiPowerSupply : ScpiInstrument, IPowerSupply
{
    public ScpiPowerSupply(IScpiTransport transport) : base(transport)
    {
    }

    public int Channel { get; private set; } = 1;

    public void SelectChannel(int channel)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(channel, 1);
        Transport.Write($"INST:NSEL {channel}");
        Channel = channel;
    }

    public void SetVoltage(double volts)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(volts);
        Transport.Write($"VOLT {ScpiParser.FormatNumber(volts)}");
    }

    public void SetCurrentLimit(double amps)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amps);
        Transport.Write($"CURR {ScpiParser.FormatNumber(amps)}");
    }

    public void SetOutput(bool enabled) => Transport.Write(enabled ? "OUTP ON" : "OUTP OFF");

    public double MeasureVoltage() => QueryDouble("MEAS:VOLT?");

    public double MeasureCurrent() => QueryDouble("MEAS:CURR?");
}
