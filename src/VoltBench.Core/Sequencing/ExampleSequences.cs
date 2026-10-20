namespace VoltBench.Core.Sequencing;

/// <summary>Built-in sequences, tuned for the simulated 10 ohm load.</summary>
public static class ExampleSequences
{
    public static IReadOnlyList<TestSequence> All => [VoltageSweep(), LoadRegulation(), CurrentLimit()];

    /// <summary>Steps the supply from 1 V to 12 V and checks the DMM reading within 3 percent.</summary>
    public static TestSequence VoltageSweep(int startVolts = 1, int endVolts = 12, double tolerance = 0.03)
    {
        var steps = new List<TestStep>
        {
            new() { Name = "Set current limit", Action = StepAction.SetCurrentLimit, Value = 2 },
            new() { Name = "Output on", Action = StepAction.SetOutput, Value = 1 },
        };

        for (var v = startVolts; v <= endVolts; v++)
        {
            steps.Add(new TestStep { Name = $"Set {v} V", Action = StepAction.SetVoltage, Value = v });
            steps.Add(new TestStep { Name = $"Settle {v} V", Action = StepAction.Wait, Value = 150 });
            steps.Add(new TestStep
            {
                Name = $"Measure {v} V",
                Action = StepAction.MeasureDcVoltage,
                Low = v * (1 - tolerance),
                High = v * (1 + tolerance),
            });
        }

        steps.Add(new TestStep { Name = "Output off", Action = StepAction.SetOutput, Value = 0 });
        return new TestSequence
        {
            Name = "Voltage Sweep 1-12 V",
            Description = "Sweeps the supply and verifies the DMM voltage tracks the setpoint within 3 percent.",
            Steps = steps,
        };
    }

    /// <summary>Checks that output voltage under load stays within 1 percent and the load current is sane.</summary>
    public static TestSequence LoadRegulation()
    {
        var steps = new List<TestStep>
        {
            new() { Name = "Set current limit", Action = StepAction.SetCurrentLimit, Value = 3 },
            new() { Name = "Output on", Action = StepAction.SetOutput, Value = 1 },
        };

        foreach (var (volts, amps) in new[] { (5.0, 0.5), (12.0, 1.2) })
        {
            steps.Add(new TestStep { Name = $"Set {volts:0} V", Action = StepAction.SetVoltage, Value = volts });
            steps.Add(new TestStep { Name = "Settle", Action = StepAction.Wait, Value = 300 });
            steps.Add(new TestStep
            {
                Name = $"Load voltage at {volts:0} V",
                Action = StepAction.MeasureDcVoltage,
                Low = volts * 0.99,
                High = volts * 1.01,
            });
            steps.Add(new TestStep
            {
                Name = $"Supply readback at {volts:0} V",
                Action = StepAction.MeasureSupplyVoltage,
                Low = volts * 0.99,
                High = volts * 1.01,
            });
            steps.Add(new TestStep
            {
                Name = $"Load current at {volts:0} V",
                Action = StepAction.MeasureDcCurrent,
                Low = amps * 0.95,
                High = amps * 1.05,
            });
        }

        steps.Add(new TestStep { Name = "Output off", Action = StepAction.SetOutput, Value = 0 });
        return new TestSequence
        {
            Name = "Load Regulation",
            Description = "Verifies voltage regulation and load current at 5 V and 12 V.",
            Steps = steps,
        };
    }

    /// <summary>Forces the supply into constant-current mode and checks the clamp.</summary>
    public static TestSequence CurrentLimit() => new()
    {
        Name = "Current Limit",
        Description = "Demands more current than allowed and checks the supply clamps at 250 mA.",
        Steps =
        [
            new() { Name = "Set current limit", Action = StepAction.SetCurrentLimit, Value = 0.25 },
            new() { Name = "Set 5 V", Action = StepAction.SetVoltage, Value = 5 },
            new() { Name = "Output on", Action = StepAction.SetOutput, Value = 1 },
            new() { Name = "Settle", Action = StepAction.Wait, Value = 300 },
            new() { Name = "Clamped current", Action = StepAction.MeasureDcCurrent, Low = 0.2375, High = 0.2625 },
            new() { Name = "Foldback voltage", Action = StepAction.MeasureDcVoltage, Low = 2.3, High = 2.7 },
            new() { Name = "Output off", Action = StepAction.SetOutput, Value = 0 },
        ],
    };
}
