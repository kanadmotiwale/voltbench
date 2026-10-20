# VoltBench ATE Suite

A WPF desktop application for automated test equipment (ATE). It drives a bench power
supply and a digital multimeter over SCPI, runs scripted test sequences with pass/fail
limits, plots measurements live and logs every measurement to CSV.

The app runs without VISA or hardware: the default resource name `Simulated` connects to
an in-memory supply and DMM wired to a resistive load.

![Screenshot](docs/screenshot.png)

<!-- Replace docs/screenshot.png with a capture of the main window. -->

## Solution layout

| Project | Purpose |
| --- | --- |
| `src/VoltBench.Core` | Instrument interfaces, test sequencing engine, JSON sequence loader, CSV logger |
| `src/VoltBench.Instruments` | SCPI parsing and drivers, VISA transport, simulated instruments |
| `src/VoltBench.App` | WPF front end (MVVM, CommunityToolkit.Mvvm, ScottPlot 5) |
| `tests/VoltBench.Tests` | xUnit tests |

## Architecture

```
MainViewModel --> SequenceRunner --> IPowerSupply / IMultimeter
                                          |
                          ScpiPowerSupply / ScpiMultimeter  (SCPI strings)
                                          |
                                    IScpiTransport
                                 /                    \
                        VisaTransport          SimulatedTransport
                      (Ivi.Visa session)    (models supply + load + DMM)
```

- **Drivers** build and parse SCPI text only; they never know whether the other end is
  hardware. Swapping `VisaTransport` for a simulated transport exercises the same driver code.
- **Simulation** shares one `SimulatedBench` model: supply channel 1 feeds a resistive load
  (default 10 ohm) through a small output resistance with constant-current limiting, and the
  DMM reads that circuit with Gaussian noise.
- **Sequences** are lists of steps (`SetVoltage`, `SetCurrentLimit`, `SetOutput`, `Wait`,
  `MeasureDcVoltage`, `MeasureDcCurrent`, `MeasureResistance`, `MeasureSupplyVoltage`,
  `MeasureSupplyCurrent`). Measurement steps carry optional low/high limits.
  `SequenceRunner` is async, reports progress per step and returns a result on cancellation or
  instrument error instead of throwing. It always switches the supply output off on exit.
- **MVVM**: views bind to `MainViewModel`; the window has no code-behind beyond
  `InitializeComponent`. The plot is fed through the `PlotBehavior.Model` attached property.

## Build, run, test

Requires the .NET 8 SDK. The app itself needs Windows to run; the library projects and tests
build and run on any platform.

```
dotnet build VoltBench.sln
dotnet test VoltBench.sln
dotnet run --project src/VoltBench.App
```

Building the WPF project on macOS or Linux works because the repository sets
`EnableWindowsTargeting`; running it does not.

## Connecting real hardware

1. Install a VISA implementation (NI-VISA, Keysight IO Libraries or similar) and confirm
   the instrument appears in its resource manager.
2. Enter the VISA resource string in the connection panel, for example
   `USB0::0x1AB1::0x0E11::DP8A000000000::INSTR` or `TCPIP0::192.168.1.50::INSTR`.
   Each instrument can be real or `Simulated` independently.
3. Connect. The `*IDN?` responses appear in the log, and `*RST` is sent to both instruments.

The drivers use standard SCPI (`INST:NSEL`, `VOLT`, `CURR`, `OUTP`, `MEAS:VOLT?`,
`CONF:VOLT:DC`, `READ?`, `MEAS:RES?`). Instruments with non-standard command sets need
a small subclass of `ScpiInstrument`.

## Sequence files

Place `.json` files in the `Sequences` folder next to the executable, or use
**Load from file...**. Enum values are strings and `//` comments are allowed.

```json
{
  "name": "Quick Check",
  "steps": [
    { "name": "Set 3.3 V", "action": "SetVoltage", "value": 3.3 },
    { "name": "Output on", "action": "SetOutput", "value": 1 },
    { "name": "Settle", "action": "Wait", "value": 250 },
    { "name": "Rail voltage", "action": "MeasureDcVoltage", "low": 3.2, "high": 3.4 }
  ]
}
```

`value` is volts, amps, 0/1 for output state, or milliseconds for `Wait`. `channel`
(default 1) selects the supply channel.

## CSV logs

One file per run in the configured folder (default `Documents/VoltBench/Logs`), named
`<sequence>_<yyyyMMdd_HHmmss>.csv` with columns
`Timestamp,Step,Setpoint,Measured,LowLimit,HighLimit,Unit,Result`.
