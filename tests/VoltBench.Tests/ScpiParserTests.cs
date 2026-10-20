using VoltBench.Core.Instruments;
using VoltBench.Instruments.Scpi;

namespace VoltBench.Tests;

public class ScpiParserTests
{
    [Theory]
    [InlineData("+1.234500E+00", 1.2345)]
    [InlineData("1.5e-3", 0.0015)]
    [InlineData("-4.7E+01", -47)]
    [InlineData("  5.0\r\n", 5.0)]
    [InlineData("0", 0)]
    [InlineData("+9.99999E-06", 9.99999e-6)]
    [InlineData("\"3.3\"", 3.3)]
    [InlineData("+1.0E+00,+2.0E+00", 1.0)]
    public void ParseDouble_ReadsValidNumbers(string text, double expected) =>
        Assert.Equal(expected, ScpiParser.ParseDouble(text), 12);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("--3")]
    public void ParseDouble_RejectsGarbage(string text) =>
        Assert.Throws<InstrumentException>(() => ScpiParser.ParseDouble(text));

    [Theory]
    [InlineData("+9.9E+37")]
    [InlineData("9.91E37")]
    [InlineData("-9.91E+37")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    public void ParseDouble_RejectsOverloadAndNonFinite(string text) =>
        Assert.Throws<InstrumentException>(() => ScpiParser.ParseDouble(text));

    [Fact]
    public void ParseDouble_IsCultureInvariant()
    {
        var original = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            Assert.Equal(1.5, ScpiParser.ParseDouble("1.5"));
            Assert.Equal("1.5", ScpiParser.FormatNumber(1.5));
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = original;
        }
    }

    [Theory]
    [InlineData(5, "5")]
    [InlineData(3.3, "3.3")]
    [InlineData(0.25, "0.25")]
    [InlineData(-1.5, "-1.5")]
    public void FormatNumber_UsesInvariantShortestForm(double value, string expected) =>
        Assert.Equal(expected, ScpiParser.FormatNumber(value));

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void FormatNumber_RejectsNonFinite(double value) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => ScpiParser.FormatNumber(value));

    [Fact]
    public void ParseDoubleList_ReadsAllValues() =>
        Assert.Equal([1.0, 2.5, -3.0], ScpiParser.ParseDoubleList("+1.0E+00, +2.5E+00,-3"));

    [Theory]
    [InlineData("1", true)]
    [InlineData("ON", true)]
    [InlineData("0\n", false)]
    [InlineData("off", false)]
    public void ParseBool_ReadsBothForms(string text, bool expected) =>
        Assert.Equal(expected, ScpiParser.ParseBool(text));

    [Fact]
    public void ParseBool_RejectsUnknown() =>
        Assert.Throws<InstrumentException>(() => ScpiParser.ParseBool("maybe"));

    [Theory]
    [InlineData("+0,\"No error\"", 0, "No error")]
    [InlineData("-113,\"Undefined header\"", -113, "Undefined header")]
    [InlineData("-222,\"Data out of range, value clipped\"", -222, "Data out of range, value clipped")]
    [InlineData("0", 0, "")]
    public void ParseError_SplitsCodeAndMessage(string text, int code, string message)
    {
        var error = ScpiParser.ParseError(text);
        Assert.Equal(code, error.Code);
        Assert.Equal(message, error.Message);
        Assert.Equal(code != 0, error.IsError);
    }

    [Fact]
    public void ParseError_RejectsNonNumericCode() =>
        Assert.Throws<InstrumentException>(() => ScpiParser.ParseError("oops,\"x\""));
}
