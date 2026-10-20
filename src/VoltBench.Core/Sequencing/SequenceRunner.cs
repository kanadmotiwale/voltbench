using System.Diagnostics;
using VoltBench.Core.Instruments;
using VoltBench.Core.Logging;

namespace VoltBench.Core.Sequencing;

/// <summary>
/// Executes a <see cref="TestSequence"/> against a <see cref="TestBench"/>. Cancellation and
/// instrument errors end the run with an <see cref="SequenceResult"/> rather than an exception,
/// and the supply output is always switched off before the run returns.
/// </summary>
public sealed class SequenceRunner
{
    private readonly TestBench _bench;
    private readonly IMeasurementLogger? _logger;
    private readonly TimeProvider _time;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;

    public SequenceRunner(
        TestBench bench,
        IMeasurementLogger? logger = null,
        TimeProvider? timeProvider = null,
        Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        _bench = bench;
        _logger = logger;
        _time = timeProvider ?? TimeProvider.System;
        _delay = delay ?? ((span, token) => Task.Delay(span, token));
    }

    public async Task<SequenceResult> RunAsync(
        TestSequence sequence,
        IProgress<StepProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var started = _time.GetUtcNow();
        var clock = Stopwatch.StartNew();
        var results = new List<StepResult>(sequence.Steps.Count);
        var outcome = SequenceOutcome.Passed;
        var state = new RunState();

        _logger?.Begin(sequence.Name);
        try
        {
            for (var i = 0; i < sequence.Steps.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var step = sequence.Steps[i];
                progress?.Report(new StepProgress(i, sequence.Steps.Count, step, null));

                var result = await ExecuteAsync(i, step, state, cancellationToken).ConfigureAwait(false);
                results.Add(result);
                if (step.Action.IsMeasurement())
                {
                    _logger?.Log(result);
                }

                progress?.Report(new StepProgress(i, sequence.Steps.Count, step, result));

                if (result.Outcome == StepOutcome.Error)
                {
                    outcome = SequenceOutcome.Errored;
                    break;
                }

                if (result.Outcome == StepOutcome.Fail)
                {
                    outcome = SequenceOutcome.Failed;
                }
            }
        }
        catch (OperationCanceledException)
        {
            outcome = SequenceOutcome.Aborted;
        }
        finally
        {
            DisableOutput();
            _logger?.End();
        }

        for (var i = results.Count; i < sequence.Steps.Count; i++)
        {
            var skipped = new StepResult(i, sequence.Steps[i], StepOutcome.Skipped, _time.GetUtcNow());
            results.Add(skipped);
            progress?.Report(new StepProgress(i, sequence.Steps.Count, sequence.Steps[i], skipped));
        }

        return new SequenceResult(sequence.Name, outcome, results, started, clock.Elapsed, _logger?.FilePath);
    }

    private async Task<StepResult> ExecuteAsync(int index, TestStep step, RunState state, CancellationToken ct)
    {
        try
        {
            switch (step.Action)
            {
                case StepAction.Wait:
                    await _delay(TimeSpan.FromMilliseconds(Math.Max(0, step.Value)), ct).ConfigureAwait(false);
                    return Complete(index, step, null, null);

                case StepAction.SetVoltage:
                    SelectChannel(step);
                    _bench.PowerSupply.SetVoltage(step.Value);
                    state.Voltage = step.Value;
                    return Complete(index, step, step.Value, null);

                case StepAction.SetCurrentLimit:
                    SelectChannel(step);
                    _bench.PowerSupply.SetCurrentLimit(step.Value);
                    state.CurrentLimit = step.Value;
                    return Complete(index, step, step.Value, null);

                case StepAction.SetOutput:
                    SelectChannel(step);
                    _bench.PowerSupply.SetOutput(step.Value != 0);
                    return Complete(index, step, null, null);

                default:
                    return Measure(index, step, state);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is InstrumentException or ArgumentException or InvalidOperationException or FormatException)
        {
            return new StepResult(index, step, StepOutcome.Error, _time.GetUtcNow(), Message: ex.Message);
        }
    }

    private StepResult Measure(int index, TestStep step, RunState state)
    {
        double value;
        double? setpoint;
        switch (step.Action)
        {
            case StepAction.MeasureDcVoltage:
                value = _bench.Multimeter.MeasureDcVoltage();
                setpoint = state.Voltage;
                break;
            case StepAction.MeasureDcCurrent:
                value = _bench.Multimeter.MeasureDcCurrent();
                setpoint = state.CurrentLimit;
                break;
            case StepAction.MeasureResistance:
                value = _bench.Multimeter.MeasureResistance();
                setpoint = null;
                break;
            case StepAction.MeasureSupplyVoltage:
                SelectChannel(step);
                value = _bench.PowerSupply.MeasureVoltage();
                setpoint = state.Voltage;
                break;
            case StepAction.MeasureSupplyCurrent:
                SelectChannel(step);
                value = _bench.PowerSupply.MeasureCurrent();
                setpoint = state.CurrentLimit;
                break;
            default:
                throw new InvalidOperationException($"Unsupported action {step.Action}.");
        }

        var pass = LimitChecker.IsWithin(value, step.Low, step.High);
        return new StepResult(index, step, pass ? StepOutcome.Pass : StepOutcome.Fail, _time.GetUtcNow(), setpoint, value);
    }

    private StepResult Complete(int index, TestStep step, double? setpoint, double? measured) =>
        new(index, step, StepOutcome.Pass, _time.GetUtcNow(), setpoint, measured);

    private void SelectChannel(TestStep step)
    {
        if (_bench.PowerSupply.Channel != step.Channel)
        {
            _bench.PowerSupply.SelectChannel(step.Channel);
        }
    }

    private void DisableOutput()
    {
        try
        {
            _bench.PowerSupply.SetOutput(false);
        }
        catch (Exception)
        {
            // Best effort: the instrument may be the reason the run failed.
        }
    }

    private sealed class RunState
    {
        public double? Voltage { get; set; }

        public double? CurrentLimit { get; set; }
    }
}
