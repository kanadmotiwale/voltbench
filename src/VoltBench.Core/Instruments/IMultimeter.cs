namespace VoltBench.Core.Instruments;

public interface IMultimeter : IInstrument
{
    /// <summary>Configures DC voltage; a null range selects autorange.</summary>
    void ConfigureDcVoltage(double? range = null);

    /// <summary>Triggers and returns a reading using the configured function.</summary>
    double Read();

    double MeasureDcVoltage();

    double MeasureDcCurrent();

    double MeasureResistance();
}
