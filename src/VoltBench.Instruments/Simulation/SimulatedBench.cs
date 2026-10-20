namespace VoltBench.Instruments.Simulation;

/// <summary>
/// Electrical model shared by the simulated supply and multimeter: a resistive load wired to
/// supply channel 1 through the supply's output resistance, with constant-current limiting.
/// </summary>
public sealed class SimulatedBench
{
    public const int ChannelCount = 3;
    public const double MaxVoltage = 30;
    public const double MaxCurrent = 3;
    public const int LoadedChannel = 1;

    private readonly Random _random;

    public SimulatedBench(double loadOhms = 10, double sourceOhms = 0.05, double noiseFraction = 0.0005, int? seed = null)
    {
        LoadOhms = loadOhms;
        SourceOhms = sourceOhms;
        NoiseFraction = noiseFraction;
        _random = seed is null ? new Random() : new Random(seed.Value);
        Reset();
    }

    public double LoadOhms { get; set; }

    public double SourceOhms { get; set; }

    /// <summary>Standard deviation of reading noise relative to the reading.</summary>
    public double NoiseFraction { get; set; }

    public ChannelState[] Channels { get; } = Enumerable.Range(0, ChannelCount).Select(_ => new ChannelState()).ToArray();

    public static (SimulatedPowerSupplyTransport Supply, SimulatedMultimeterTransport Multimeter) CreateInstruments(
        double loadOhms = 10, double noiseFraction = 0.0005, int? seed = null)
    {
        var bench = new SimulatedBench(loadOhms, noiseFraction: noiseFraction, seed: seed);
        return (new SimulatedPowerSupplyTransport(bench), new SimulatedMultimeterTransport(bench));
    }

    public void Reset()
    {
        foreach (var channel in Channels)
        {
            channel.Voltage = 0;
            channel.CurrentLimit = 1;
            channel.OutputOn = false;
        }
    }

    /// <summary>True operating point (no noise) of a 1-based channel.</summary>
    public (double Volts, double Amps) Operate(int channel)
    {
        var state = Channels[channel - 1];
        if (!state.OutputOn)
        {
            return (0, 0);
        }

        if (channel != LoadedChannel)
        {
            return (state.Voltage, 0);
        }

        var current = state.Voltage / (LoadOhms + SourceOhms);
        if (current > state.CurrentLimit)
        {
            current = state.CurrentLimit;
        }

        return (current * LoadOhms, current);
    }

    public double Noisy(double value)
    {
        if (NoiseFraction <= 0 || value == 0)
        {
            return value;
        }

        // Box-Muller transform for a standard normal sample.
        var u1 = 1.0 - _random.NextDouble();
        var u2 = _random.NextDouble();
        var gauss = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        return value * (1 + gauss * NoiseFraction);
    }

    public sealed class ChannelState
    {
        public double Voltage { get; set; }

        public double CurrentLimit { get; set; }

        public bool OutputOn { get; set; }
    }
}
