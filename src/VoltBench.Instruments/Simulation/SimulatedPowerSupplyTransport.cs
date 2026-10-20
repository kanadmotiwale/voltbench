using VoltBench.Instruments.Scpi;

namespace VoltBench.Instruments.Simulation;

/// <summary>Simulated three-channel supply; use <c>Simulated</c> as the resource name in the app.</summary>
public sealed class SimulatedPowerSupplyTransport : SimulatedTransport
{
    private readonly SimulatedBench _bench;
    private int _selected = 1;

    public SimulatedPowerSupplyTransport(SimulatedBench bench)
    {
        _bench = bench;
    }

    public override string ResourceName => "Simulated";

    protected override string Identity => "VoltBench,SIM-PSU3,SN0001,1.0";

    protected override void OnReset()
    {
        _bench.Reset();
        _selected = 1;
    }

    protected override bool TryHandle(string header, string args, out string? response)
    {
        response = null;
        var state = _bench.Channels[_selected - 1];

        switch (header)
        {
            case "INST:NSEL":
                if (TryParseArg(args, out var channel))
                {
                    if (channel is >= 1 and <= SimulatedBench.ChannelCount && channel == Math.Floor(channel))
                    {
                        _selected = (int)channel;
                    }
                    else
                    {
                        PushError(-222, "Data out of range");
                    }
                }

                return true;

            case "VOLT":
                if (TryParseArg(args, out var volts))
                {
                    if (volts is >= 0 and <= SimulatedBench.MaxVoltage)
                    {
                        state.Voltage = volts;
                    }
                    else
                    {
                        PushError(-222, "Data out of range");
                    }
                }

                return true;

            case "CURR":
                if (TryParseArg(args, out var amps))
                {
                    if (amps is >= 0 and <= SimulatedBench.MaxCurrent)
                    {
                        state.CurrentLimit = amps;
                    }
                    else
                    {
                        PushError(-222, "Data out of range");
                    }
                }

                return true;

            case "OUTP":
                switch (args.ToUpperInvariant())
                {
                    case "ON" or "1":
                        state.OutputOn = true;
                        break;
                    case "OFF" or "0":
                        state.OutputOn = false;
                        break;
                    default:
                        PushError(-224, "Illegal parameter value");
                        break;
                }

                return true;

            case "VOLT?":
                response = FormatReading(state.Voltage);
                return true;
            case "CURR?":
                response = FormatReading(state.CurrentLimit);
                return true;
            case "OUTP?":
                response = state.OutputOn ? "1" : "0";
                return true;
            case "MEAS:VOLT?":
                response = FormatReading(_bench.Noisy(_bench.Operate(_selected).Volts));
                return true;
            case "MEAS:CURR?":
                response = FormatReading(_bench.Noisy(_bench.Operate(_selected).Amps));
                return true;
            default:
                return false;
        }
    }
}
