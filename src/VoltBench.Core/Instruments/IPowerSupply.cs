namespace VoltBench.Core.Instruments;

public interface IPowerSupply : IInstrument
{
    /// <summary>Currently selected channel (1-based).</summary>
    int Channel { get; }

    void SelectChannel(int channel);

    void SetVoltage(double volts);

    void SetCurrentLimit(double amps);

    void SetOutput(bool enabled);

    double MeasureVoltage();

    double MeasureCurrent();
}
