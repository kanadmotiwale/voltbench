using System.Globalization;
using VoltBench.Core.Instruments;

namespace VoltBench.Instruments.Scpi;

/// <summary>Parsing and formatting helpers for SCPI messages.</summary>
public static class ScpiParser
{
    /// <summary>Instruments report overflow as 9.9E37 (IEEE 488.2 NR3 "not a number").</summary>
    private const double OverloadThreshold = 9.9e37;

    public static string FormatNumber(double value)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "SCPI numeric parameters must be finite.");
        }

        return value.ToString("R", CultureInfo.InvariantCulture);
    }

    /// <summary>Parses a numeric response such as <c>+1.2345E-03</c>; throws on overload or garbage.</summary>
    public static double ParseDouble(string response)
    {
        if (!TryParseDouble(response, out var value))
        {
            throw new InstrumentException($"Unparseable numeric response '{response?.Trim()}'.");
        }

        if (!double.IsFinite(value) || Math.Abs(value) >= OverloadThreshold)
        {
            throw new InstrumentException($"Instrument reported an overload or invalid reading ('{response.Trim()}').");
        }

        return value;
    }

    public static bool TryParseDouble(string? response, out double value)
    {
        value = double.NaN;
        if (string.IsNullOrWhiteSpace(response))
        {
            return false;
        }

        // Multi-value responses such as "+1.0E+00,+2.0E+00" yield the first reading.
        var first = response.Split(',', 2)[0].Trim().Trim('"');
        return double.TryParse(
            first,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out value);
    }

    public static IReadOnlyList<double> ParseDoubleList(string response) =>
        response.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ParseDouble)
            .ToArray();

    public static bool ParseBool(string response) => response.Trim().ToUpperInvariant() switch
    {
        "1" or "ON" => true,
        "0" or "OFF" => false,
        _ => throw new InstrumentException($"Unparseable boolean response '{response.Trim()}'."),
    };

    /// <summary>Parses a <c>SYST:ERR?</c> response like <c>-113,"Undefined header"</c>.</summary>
    public static ScpiError ParseError(string response)
    {
        var text = response?.Trim() ?? string.Empty;
        var comma = text.IndexOf(',');
        var codeText = comma < 0 ? text : text[..comma];
        if (!int.TryParse(codeText.Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var code))
        {
            throw new InstrumentException($"Unparseable error response '{text}'.");
        }

        var message = comma < 0 ? string.Empty : text[(comma + 1)..].Trim().Trim('"');
        return new ScpiError(code, message);
    }
}
