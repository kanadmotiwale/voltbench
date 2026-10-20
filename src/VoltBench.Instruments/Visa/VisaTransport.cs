using Ivi.Visa;
using VoltBench.Core.Instruments;

namespace VoltBench.Instruments.Visa;

/// <summary>SCPI transport over an IVI VISA.NET message-based session.</summary>
public sealed class VisaTransport : IScpiTransport
{
    private IMessageBasedSession? _session;
    private int _timeoutMs = 2000;

    public VisaTransport(string resourceName)
    {
        ResourceName = resourceName ?? throw new ArgumentNullException(nameof(resourceName));
    }

    public string ResourceName { get; }

    public bool IsOpen => _session is not null;

    public int TimeoutMs
    {
        get => _timeoutMs;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            _timeoutMs = value;
            if (_session is not null)
            {
                _session.TimeoutMilliseconds = value;
            }
        }
    }

    /// <summary>Lists VISA resources visible to the installed VISA implementation; empty if none is installed.</summary>
    public static IReadOnlyList<string> FindResources()
    {
        try
        {
            return GlobalResourceManager.Find("?*INSTR").ToArray();
        }
        catch (Exception)
        {
            return [];
        }
    }

    public void Open()
    {
        if (_session is not null)
        {
            return;
        }

        try
        {
            var session = GlobalResourceManager.Open(ResourceName, AccessModes.None, _timeoutMs);
            _session = session as IMessageBasedSession
                ?? throw new InstrumentException($"'{ResourceName}' is not a message-based resource.");
            _session.TimeoutMilliseconds = _timeoutMs;
        }
        catch (Exception ex) when (ex is not InstrumentException)
        {
            throw new InstrumentException($"Cannot open VISA resource '{ResourceName}': {ex.Message}", ex);
        }
    }

    public void Close()
    {
        _session?.Dispose();
        _session = null;
    }

    public void Write(string command)
    {
        var session = RequireSession();
        try
        {
            session.FormattedIO.WriteLine(command);
        }
        catch (Exception ex) when (ex is VisaException or IOException)
        {
            throw new InstrumentException($"Write '{command}' failed: {ex.Message}", ex);
        }
    }

    public string Query(string command)
    {
        var session = RequireSession();
        try
        {
            session.FormattedIO.WriteLine(command);
            return session.FormattedIO.ReadLine();
        }
        catch (IOTimeoutException ex)
        {
            throw new InstrumentException($"Query '{command}' timed out after {_timeoutMs} ms.", ex);
        }
        catch (Exception ex) when (ex is VisaException or IOException)
        {
            throw new InstrumentException($"Query '{command}' failed: {ex.Message}", ex);
        }
    }

    public void Dispose() => Close();

    private IMessageBasedSession RequireSession() =>
        _session ?? throw new InvalidOperationException($"Transport '{ResourceName}' is not open.");
}
