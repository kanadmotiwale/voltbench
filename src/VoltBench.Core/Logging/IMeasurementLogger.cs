using VoltBench.Core.Sequencing;

namespace VoltBench.Core.Logging;

public interface IMeasurementLogger : IDisposable
{
    /// <summary>Path of the file for the current run, if any.</summary>
    string? FilePath { get; }

    void Begin(string sequenceName);

    void Log(StepResult result);

    void End();
}
