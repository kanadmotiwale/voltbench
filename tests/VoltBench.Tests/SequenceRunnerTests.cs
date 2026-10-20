using VoltBench.Core.Instruments;
using VoltBench.Core.Logging;
using VoltBench.Core.Sequencing;
using VoltBench.Instruments.Scpi;
using VoltBench.Instruments.Simulation;
using VoltBench.Tests.Fakes;

namespace VoltBench.Tests;

public class SequenceRunnerTests
{
    private readonly FakePowerSupply _psu = new();
    private readonly FakeMultimeter _dmm = new();

    private SequenceRunner CreateRunner(IMeasurementLogger? logger = null, Func<TimeSpan, CancellationToken, Task>? delay = null) =>
        new(new TestBench(_psu, _dmm), logger, delay: delay ?? ((_, _) => Task.CompletedTask));

    private static TestSequence Sequence(params TestStep[] steps) => new() { Name = "Test", Steps = steps };

    private static TestStep Step(StepAction action, double value = 0, double? low = null, double? high = null, string name = "step") =>
        new() { Name = name, Action = action, Value = value, Low = low, High = high };

    [Fact]
    public async Task AllStepsWithinLimits_Passes()
    {
        _dmm.Voltage = 5.0;
        var result = await CreateRunner().RunAsync(Sequence(
            Step(StepAction.SetVoltage, 5),
            Step(StepAction.MeasureDcVoltage, low: 4.9, high: 5.1)));

        Assert.Equal(SequenceOutcome.Passed, result.Outcome);
        Assert.All(result.Steps, s => Assert.Equal(StepOutcome.Pass, s.Outcome));
    }

    [Fact]
    public async Task MeasurementOutsideLimits_FailsSequenceButContinues()
    {
        _dmm.Voltage = 6.0;
        var result = await CreateRunner().RunAsync(Sequence(
            Step(StepAction.MeasureDcVoltage, low: 4.9, high: 5.1),
            Step(StepAction.MeasureDcCurrent)));

        Assert.Equal(SequenceOutcome.Failed, result.Outcome);
        Assert.Equal(StepOutcome.Fail, result.Steps[0].Outcome);
        Assert.Equal(StepOutcome.Pass, result.Steps[1].Outcome);
        Assert.Equal(6.0, result.Steps[0].Measured);
    }

    [Fact]
    public async Task MeasureStep_RecordsSetpointFromLastSetVoltage()
    {
        var result = await CreateRunner().RunAsync(Sequence(
            Step(StepAction.SetVoltage, 3.3),
            Step(StepAction.MeasureDcVoltage)));

        Assert.Equal(3.3, result.Steps[1].Setpoint);
    }

    [Fact]
    public async Task Cancellation_AbortsSkipsRemainingStepsAndTurnsOutputOff()
    {
        using var cts = new CancellationTokenSource();
        _psu.OnSetVoltage = _ => cts.Cancel();

        var result = await CreateRunner().RunAsync(
            Sequence(
                Step(StepAction.SetVoltage, 1),
                Step(StepAction.SetVoltage, 2),
                Step(StepAction.MeasureDcVoltage)),
            cancellationToken: cts.Token);

        Assert.Equal(SequenceOutcome.Aborted, result.Outcome);
        Assert.Equal(StepOutcome.Pass, result.Steps[0].Outcome);
        Assert.Equal(StepOutcome.Skipped, result.Steps[1].Outcome);
        Assert.Equal(StepOutcome.Skipped, result.Steps[2].Outcome);
        Assert.Equal("output off", _psu.Calls[^1]);
    }

    [Fact]
    public async Task CancellationDuringWait_Aborts()
    {
        using var cts = new CancellationTokenSource();
        var runner = CreateRunner(delay: async (span, token) =>
        {
            cts.Cancel();
            await Task.Delay(span, token);
        });

        var result = await runner.RunAsync(Sequence(Step(StepAction.Wait, 60_000)), cancellationToken: cts.Token);

        Assert.Equal(SequenceOutcome.Aborted, result.Outcome);
        Assert.Equal(StepOutcome.Skipped, Assert.Single(result.Steps).Outcome);
    }

    [Fact]
    public async Task PreCancelledToken_RunsNothing()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await CreateRunner().RunAsync(
            Sequence(Step(StepAction.SetVoltage, 5)), cancellationToken: cts.Token);

        Assert.Equal(SequenceOutcome.Aborted, result.Outcome);
        Assert.DoesNotContain("volt 5", _psu.Calls);
    }

    [Fact]
    public async Task InstrumentError_StopsRunAndMarksRemainingSkipped()
    {
        _psu.FailOnSetVoltage = new InstrumentException("link down");

        var result = await CreateRunner().RunAsync(Sequence(
            Step(StepAction.SetVoltage, 5),
            Step(StepAction.MeasureDcVoltage)));

        Assert.Equal(SequenceOutcome.Errored, result.Outcome);
        Assert.Equal(StepOutcome.Error, result.Steps[0].Outcome);
        Assert.Equal("link down", result.Steps[0].Message);
        Assert.Equal(StepOutcome.Skipped, result.Steps[1].Outcome);
    }

    [Fact]
    public async Task OutputIsAlwaysDisabledAtEnd()
    {
        await CreateRunner().RunAsync(Sequence(Step(StepAction.SetOutput, 1)));
        Assert.Equal(["output on", "output off"], _psu.Calls);
    }

    [Fact]
    public async Task WaitStep_UsesInjectedDelay()
    {
        TimeSpan? requested = null;
        var runner = CreateRunner(delay: (span, _) =>
        {
            requested = span;
            return Task.CompletedTask;
        });

        await runner.RunAsync(Sequence(Step(StepAction.Wait, 250)));
        Assert.Equal(TimeSpan.FromMilliseconds(250), requested);
    }

    [Fact]
    public async Task ChannelIsSelectedOnlyWhenItChanges()
    {
        var steps = new[]
        {
            new TestStep { Name = "a", Action = StepAction.SetVoltage, Value = 1, Channel = 2 },
            new TestStep { Name = "b", Action = StepAction.SetVoltage, Value = 2, Channel = 2 },
            new TestStep { Name = "c", Action = StepAction.SetVoltage, Value = 3, Channel = 1 },
        };

        await CreateRunner().RunAsync(Sequence(steps));

        Assert.Equal(2, _psu.Calls.Count(c => c.StartsWith("channel")));
    }

    [Fact]
    public async Task Progress_ReportsStartAndFinishForEachStep()
    {
        var reports = new List<StepProgress>();
        var progress = new SyncProgress<StepProgress>(reports.Add);

        await CreateRunner().RunAsync(
            Sequence(Step(StepAction.SetVoltage, 1), Step(StepAction.MeasureDcVoltage)), progress);

        Assert.Equal(4, reports.Count);
        Assert.Null(reports[0].Result);
        Assert.NotNull(reports[1].Result);
        Assert.Equal(2, reports[3].Total);
    }

    [Fact]
    public async Task MeasurementsAreLoggedButSetupStepsAreNot()
    {
        var logger = new RecordingLogger();
        await CreateRunner(logger).RunAsync(Sequence(
            Step(StepAction.SetVoltage, 5),
            Step(StepAction.MeasureDcVoltage),
            Step(StepAction.MeasureSupplyCurrent)));

        Assert.Equal(2, logger.Logged.Count);
        Assert.True(logger.Began);
        Assert.True(logger.Ended);
    }

    [Fact]
    public async Task FullExampleSweepPassesAgainstSimulatedBench()
    {
        var (supply, dmm) = SimulatedBench.CreateInstruments(noiseFraction: 0.0005, seed: 1);
        supply.Open();
        dmm.Open();
        var runner = new SequenceRunner(
            new TestBench(new ScpiPowerSupply(supply), new ScpiMultimeter(dmm)),
            delay: (_, _) => Task.CompletedTask);

        foreach (var sequence in ExampleSequences.All)
        {
            var result = await runner.RunAsync(sequence);
            Assert.True(result.Outcome == SequenceOutcome.Passed, $"{sequence.Name}: {result.Outcome}");
        }
    }

    [Fact]
    public async Task OutOfSpecDevice_FailsSweepAgainstSimulatedBench()
    {
        var (supply, dmm) = SimulatedBench.CreateInstruments(loadOhms: 10, noiseFraction: 0, seed: 1);
        supply.Open();
        dmm.Open();
        var psu = new ScpiPowerSupply(supply);
        var runner = new SequenceRunner(new TestBench(psu, new ScpiMultimeter(dmm)), delay: (_, _) => Task.CompletedTask);

        // A 10 A shunt in series is simulated by clamping current far below demand.
        var sequence = Sequence(
            Step(StepAction.SetCurrentLimit, 0.1),
            Step(StepAction.SetVoltage, 5),
            Step(StepAction.SetOutput, 1),
            Step(StepAction.MeasureDcVoltage, low: 4.9, high: 5.1));

        var result = await runner.RunAsync(sequence);
        Assert.Equal(SequenceOutcome.Failed, result.Outcome);
    }

    [Fact]
    public void ExampleSequences_AreValidAndRoundTripThroughJson()
    {
        foreach (var sequence in ExampleSequences.All)
        {
            var parsed = SequenceLoader.Parse(SequenceLoader.Serialize(sequence));
            Assert.Equal(sequence.Name, parsed.Name);
            Assert.Equal(sequence.Steps, parsed.Steps);
        }
    }

    private sealed class SyncProgress<T>(Action<T> handler) : IProgress<T>
    {
        public void Report(T value) => handler(value);
    }

    private sealed class RecordingLogger : IMeasurementLogger
    {
        public List<StepResult> Logged { get; } = [];

        public bool Began { get; private set; }

        public bool Ended { get; private set; }

        public string? FilePath => null;

        public void Begin(string sequenceName) => Began = true;

        public void Log(StepResult result) => Logged.Add(result);

        public void End() => Ended = true;

        public void Dispose()
        {
        }
    }
}
