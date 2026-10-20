using VoltBench.Core.Instruments;

namespace VoltBench.Tests.Fakes;

public sealed class FakePowerSupply : IPowerSupply
{
    public List<string> Calls { get; } = [];

    public double VoltageReadback { get; set; } = 5;

    public double CurrentReadback { get; set; } = 0.5;

    public Exception? FailOnSetVoltage { get; set; }

    public Action<double>? OnSetVoltage { get; set; }

    public int Channel { get; private set; } = 1;

    public string Identify() => "FAKE,PSU,0,0";

    public void Reset() => Calls.Add("reset");

    public IReadOnlyList<ScpiError> CheckErrors() => [];

    public void SelectChannel(int channel)
    {
        Channel = channel;
        Calls.Add($"channel {channel}");
    }

    public void SetVoltage(double volts)
    {
        if (FailOnSetVoltage is not null)
        {
            throw FailOnSetVoltage;
        }

        Calls.Add($"volt {volts}");
        OnSetVoltage?.Invoke(volts);
    }

    public void SetCurrentLimit(double amps) => Calls.Add($"curr {amps}");

    public void SetOutput(bool enabled) => Calls.Add(enabled ? "output on" : "output off");

    public double MeasureVoltage() => VoltageReadback;

    public double MeasureCurrent() => CurrentReadback;
}

public sealed class FakeMultimeter : IMultimeter
{
    public double Voltage { get; set; } = 5;

    public double Current { get; set; } = 0.5;

    public double Resistance { get; set; } = 10;

    public Func<double>? VoltageSource { get; set; }

    public string Identify() => "FAKE,DMM,0,0";

    public void Reset()
    {
    }

    public IReadOnlyList<ScpiError> CheckErrors() => [];

    public void ConfigureDcVoltage(double? range = null)
    {
    }

    public double Read() => Voltage;

    public double MeasureDcVoltage() => VoltageSource?.Invoke() ?? Voltage;

    public double MeasureDcCurrent() => Current;

    public double MeasureResistance() => Resistance;
}
