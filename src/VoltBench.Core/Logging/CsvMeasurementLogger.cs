using System.Globalization;
using System.Text;
using VoltBench.Core.Sequencing;

namespace VoltBench.Core.Logging;

/// <summary>Writes one CSV file per run into a folder.</summary>
public sealed class CsvMeasurementLogger : IMeasurementLogger
{
    public const string Header = "Timestamp,Step,Setpoint,Measured,LowLimit,HighLimit,Unit,Result";

    private readonly string _folder;
    private readonly TimeProvider _time;
    private StreamWriter? _writer;

    public CsvMeasurementLogger(string folder, TimeProvider? timeProvider = null)
    {
        _folder = folder;
        _time = timeProvider ?? TimeProvider.System;
    }

    public string? FilePath { get; private set; }

    public void Begin(string sequenceName)
    {
        End();
        Directory.CreateDirectory(_folder);

        var stamp = _time.GetLocalNow().ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var baseName = $"{Sanitize(sequenceName)}_{stamp}";
        for (var attempt = 0; ; attempt++)
        {
            var path = Path.Combine(_folder, attempt == 0 ? $"{baseName}.csv" : $"{baseName}_{attempt}.csv");
            try
            {
                var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                _writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
                FilePath = path;
                break;
            }
            catch (IOException) when (File.Exists(path))
            {
            }
        }

        _writer.WriteLine(Header);
    }

    public void Log(StepResult result)
    {
        if (_writer is null)
        {
            throw new InvalidOperationException("Begin must be called before Log.");
        }

        var fields = new[]
        {
            result.Timestamp.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK", CultureInfo.InvariantCulture),
            Escape(result.Step.Name),
            Format(result.Setpoint),
            Format(result.Measured),
            Format(result.Step.Low),
            Format(result.Step.High),
            result.Unit,
            result.Outcome.ToString().ToUpperInvariant(),
        };
        _writer.WriteLine(string.Join(',', fields));
    }

    public void End()
    {
        _writer?.Dispose();
        _writer = null;
    }

    public void Dispose() => End();

    private static string Format(double? value) =>
        value is null ? string.Empty : value.Value.ToString("R", CultureInfo.InvariantCulture);

    private static string Escape(string text) =>
        text.AsSpan().IndexOfAny(",\"\r\n") >= 0 ? $"\"{text.Replace("\"", "\"\"")}\"" : text;

    private static string Sanitize(string name)
    {
        var invalid = Path.GetInvalidFileNameChars().Concat("<>:\"/\\|?*").ToArray();
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            sb.Append(Array.IndexOf(invalid, c) >= 0 || c == ' ' ? '_' : c);
        }

        return sb.Length == 0 ? "run" : sb.ToString();
    }
}
