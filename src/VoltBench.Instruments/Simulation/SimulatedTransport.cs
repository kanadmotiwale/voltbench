using System.Globalization;
using VoltBench.Core.Instruments;
using VoltBench.Instruments.Scpi;

namespace VoltBench.Instruments.Simulation;

/// <summary>Base for in-memory transports that interpret SCPI text like a real instrument.</summary>
public abstract class SimulatedTransport : IScpiTransport
{
    private static readonly Dictionary<string, string> LongForms = new()
    {
        ["SYSTEM"] = "SYST",
        ["ERROR"] = "ERR",
        ["INSTRUMENT"] = "INST",
        ["VOLTAGE"] = "VOLT",
        ["CURRENT"] = "CURR",
        ["OUTPUT"] = "OUTP",
        ["MEASURE"] = "MEAS",
        ["CONFIGURE"] = "CONF",
        ["RESISTANCE"] = "RES",
    };

    private readonly Queue<ScpiError> _errors = new();

    public abstract string ResourceName { get; }

    public bool IsOpen { get; private set; }

    public int TimeoutMs { get; set; } = 2000;

    protected abstract string Identity { get; }

    public void Open() => IsOpen = true;

    public void Close() => IsOpen = false;

    public void Write(string command) => Process(command);

    public string Query(string command) =>
        Process(command) ?? throw new InstrumentException($"Query '{command}' timed out after {TimeoutMs} ms.");

    public void Dispose() => Close();

    protected abstract void OnReset();

    /// <summary>Handles an instrument-specific command; returns false if the header is unknown.</summary>
    protected abstract bool TryHandle(string header, string args, out string? response);

    protected void PushError(int code, string message) => _errors.Enqueue(new ScpiError(code, message));

    protected static string FormatReading(double value) =>
        value.ToString("+0.000000E+00;-0.000000E+00", CultureInfo.InvariantCulture);

    protected bool TryParseArg(string args, out double value)
    {
        if (ScpiParser.TryParseDouble(args, out value))
        {
            return true;
        }

        PushError(-104, "Data type error");
        return false;
    }

    private string? Process(string command)
    {
        if (!IsOpen)
        {
            throw new InvalidOperationException($"Transport '{ResourceName}' is not open.");
        }

        var text = command.Trim();
        var split = text.IndexOf(' ');
        var header = Normalize(split < 0 ? text : text[..split]);
        var args = split < 0 ? string.Empty : text[(split + 1)..].Trim();

        switch (header)
        {
            case "*IDN?":
                return Identity;
            case "*RST":
                OnReset();
                return null;
            case "*CLS":
                _errors.Clear();
                return null;
            case "SYST:ERR?":
                return _errors.Count > 0
                    ? $"{_errors.Peek().Code:+0;-0},\"{_errors.Dequeue().Message}\""
                    : "+0,\"No error\"";
        }

        if (!TryHandle(header, args, out var response))
        {
            PushError(-113, "Undefined header");
        }

        return response;
    }

    private static string Normalize(string header)
    {
        var segments = header.Trim().TrimStart(':').ToUpperInvariant().Split(':');
        for (var i = 0; i < segments.Length; i++)
        {
            var query = segments[i].EndsWith('?');
            var name = query ? segments[i][..^1] : segments[i];
            if (LongForms.TryGetValue(name, out var shortForm))
            {
                name = shortForm;
            }

            segments[i] = query ? name + "?" : name;
        }

        return string.Join(':', segments);
    }
}
