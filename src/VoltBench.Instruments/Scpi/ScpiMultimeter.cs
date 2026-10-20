using VoltBench.Core.Instruments;

namespace VoltBench.Instruments.Scpi;

/// <summary>Digital multimeter driven with the standard SCPI command set.</summary>
public sealed class ScpiMultimeter : ScpiInstrument, IMultimeter
{
    public ScpiMultimeter(IScpiTransport transport) : base(transport)
    {
    }

    public void ConfigureDcVoltage(double? range = null) =>
        Transport.Write(range is null ? "CONF:VOLT:DC AUTO" : $"CONF:VOLT:DC {ScpiParser.FormatNumber(range.Value)}");

    public double Read() => QueryDouble("READ?");

    public double MeasureDcVoltage() => QueryDouble("MEAS:VOLT:DC?");

    public double MeasureDcCurrent() => QueryDouble("MEAS:CURR:DC?");

    public double MeasureResistance() => QueryDouble("MEAS:RES?");
}
