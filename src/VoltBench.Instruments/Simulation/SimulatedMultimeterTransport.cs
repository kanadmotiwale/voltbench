namespace VoltBench.Instruments.Simulation;

/// <summary>Simulated DMM wired across (voltage, resistance) or in series with (current) the bench load.</summary>
public sealed class SimulatedMultimeterTransport : SimulatedTransport
{
    private enum Function
    {
        Voltage,
        Current,
        Resistance,
    }

    private readonly SimulatedBench _bench;
    private Function _function = Function.Voltage;

    public SimulatedMultimeterTransport(SimulatedBench bench)
    {
        _bench = bench;
    }

    public override string ResourceName => "Simulated";

    protected override string Identity => "VoltBench,SIM-DMM6,SN0002,1.0";

    protected override void OnReset() => _function = Function.Voltage;

    protected override bool TryHandle(string header, string args, out string? response)
    {
        response = null;
        switch (header)
        {
            case "CONF:VOLT:DC" or "CONF:VOLT":
                _function = Function.Voltage;
                return true;
            case "CONF:CURR:DC" or "CONF:CURR":
                _function = Function.Current;
                return true;
            case "CONF:RES":
                _function = Function.Resistance;
                return true;
            case "READ?":
                response = Reading(_function);
                return true;
            case "MEAS:VOLT:DC?" or "MEAS:VOLT?":
                _function = Function.Voltage;
                response = Reading(_function);
                return true;
            case "MEAS:CURR:DC?" or "MEAS:CURR?":
                _function = Function.Current;
                response = Reading(_function);
                return true;
            case "MEAS:RES?":
                _function = Function.Resistance;
                response = Reading(_function);
                return true;
            default:
                return false;
        }
    }

    private string Reading(Function function)
    {
        var (volts, amps) = _bench.Operate(SimulatedBench.LoadedChannel);
        var value = function switch
        {
            Function.Voltage => volts,
            Function.Current => amps,
            _ => _bench.LoadOhms,
        };
        return FormatReading(_bench.Noisy(value));
    }
}
