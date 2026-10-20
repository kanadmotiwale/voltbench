namespace VoltBench.Core.Instruments;

/// <summary>Message-based link to an instrument that speaks SCPI.</summary>
public interface IScpiTransport : IDisposable
{
    /// <summary>Resource name, for example a VISA address or "Simulated".</summary>
    string ResourceName { get; }

    bool IsOpen { get; }

    /// <summary>I/O timeout in milliseconds.</summary>
    int TimeoutMs { get; set; }

    void Open();

    void Close();

    void Write(string command);

    string Query(string command);
}
