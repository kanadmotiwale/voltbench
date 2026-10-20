using VoltBench.Core.Instruments;
using VoltBench.Instruments.Scpi;
using VoltBench.Tests.Fakes;

namespace VoltBench.Tests;

public class ScpiDriverTests
{
    private readonly FakeTransport _transport = new();

    [Fact]
    public void Identify_QueriesIdnAndTrims()
    {
        _transport.Respond("*IDN?", "ACME,PSU-1,123,1.0\r\n");
        var psu = new ScpiPowerSupply(_transport);

        Assert.Equal("ACME,PSU-1,123,1.0", psu.Identify());
        Assert.Equal(["*IDN?"], _transport.Sent);
    }

    [Fact]
    public void Reset_SendsRst()
    {
        new ScpiMultimeter(_transport).Reset();
        Assert.Equal(["*RST"], _transport.Sent);
    }

    [Fact]
    public void CheckErrors_DrainsQueueUntilNoError()
    {
        _transport.Respond("SYST:ERR?", "-113,\"Undefined header\"", "-222,\"Data out of range\"", "+0,\"No error\"");
        var errors = new ScpiPowerSupply(_transport).CheckErrors();

        Assert.Equal([-113, -222], errors.Select(e => e.Code));
        Assert.Equal(3, _transport.Sent.Count);
    }

    [Fact]
    public void CheckErrors_ReturnsEmptyWhenClean()
    {
        _transport.Respond("SYST:ERR?", "+0,\"No error\"");
        Assert.Empty(new ScpiPowerSupply(_transport).CheckErrors());
    }

    [Fact]
    public void CheckErrors_StopsAfterTwentyEntries()
    {
        _transport.Respond("SYST:ERR?", "-100,\"Command error\"");
        Assert.Equal(20, new ScpiPowerSupply(_transport).CheckErrors().Count);
    }

    [Fact]
    public void SelectChannel_SendsNselAndTracksChannel()
    {
        var psu = new ScpiPowerSupply(_transport);
        psu.SelectChannel(2);

        Assert.Equal(["INST:NSEL 2"], _transport.Sent);
        Assert.Equal(2, psu.Channel);
    }

    [Fact]
    public void SelectChannel_RejectsZero() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new ScpiPowerSupply(_transport).SelectChannel(0));

    [Theory]
    [InlineData(5, "VOLT 5")]
    [InlineData(3.3, "VOLT 3.3")]
    [InlineData(0, "VOLT 0")]
    [InlineData(12.5, "VOLT 12.5")]
    public void SetVoltage_SendsExactCommand(double volts, string expected)
    {
        new ScpiPowerSupply(_transport).SetVoltage(volts);
        Assert.Equal([expected], _transport.Sent);
    }

    [Fact]
    public void SetVoltage_RejectsNegative() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new ScpiPowerSupply(_transport).SetVoltage(-1));

    [Fact]
    public void SetCurrentLimit_SendsExactCommand()
    {
        new ScpiPowerSupply(_transport).SetCurrentLimit(0.25);
        Assert.Equal(["CURR 0.25"], _transport.Sent);
    }

    [Fact]
    public void SetCurrentLimit_RejectsNegative() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new ScpiPowerSupply(_transport).SetCurrentLimit(-0.1));

    [Theory]
    [InlineData(true, "OUTP ON")]
    [InlineData(false, "OUTP OFF")]
    public void SetOutput_SendsOnOff(bool enabled, string expected)
    {
        new ScpiPowerSupply(_transport).SetOutput(enabled);
        Assert.Equal([expected], _transport.Sent);
    }

    [Fact]
    public void MeasureVoltage_ParsesScientificResponse()
    {
        _transport.Respond("MEAS:VOLT?", "+4.998200E+00");
        Assert.Equal(4.9982, new ScpiPowerSupply(_transport).MeasureVoltage(), 9);
    }

    [Fact]
    public void MeasureCurrent_ParsesResponse()
    {
        _transport.Respond("MEAS:CURR?", "+4.9810E-01");
        Assert.Equal(0.4981, new ScpiPowerSupply(_transport).MeasureCurrent(), 9);
    }

    [Fact]
    public void Measure_ThrowsOnOverloadResponse()
    {
        _transport.Respond("MEAS:VOLT?", "+9.91000000E+37");
        Assert.Throws<InstrumentException>(() => new ScpiPowerSupply(_transport).MeasureVoltage());
    }

    [Fact]
    public void Multimeter_MeasureCommandsAreExact()
    {
        _transport.Respond("MEAS:VOLT:DC?", "+1.0E+00");
        _transport.Respond("MEAS:CURR:DC?", "+2.0E-03");
        _transport.Respond("MEAS:RES?", "+1.0E+03");
        var dmm = new ScpiMultimeter(_transport);

        Assert.Equal(1.0, dmm.MeasureDcVoltage());
        Assert.Equal(0.002, dmm.MeasureDcCurrent());
        Assert.Equal(1000, dmm.MeasureResistance());
        Assert.Equal(["MEAS:VOLT:DC?", "MEAS:CURR:DC?", "MEAS:RES?"], _transport.Sent);
    }

    [Theory]
    [InlineData(null, "CONF:VOLT:DC AUTO")]
    [InlineData(10.0, "CONF:VOLT:DC 10")]
    [InlineData(0.1, "CONF:VOLT:DC 0.1")]
    public void Multimeter_ConfigureDcVoltage(double? range, string expected)
    {
        new ScpiMultimeter(_transport).ConfigureDcVoltage(range);
        Assert.Equal([expected], _transport.Sent);
    }

    [Fact]
    public void Multimeter_ReadQueriesReadCommand()
    {
        _transport.Respond("READ?", "-3.250000E-01");
        Assert.Equal(-0.325, new ScpiMultimeter(_transport).Read(), 9);
        Assert.Equal(["READ?"], _transport.Sent);
    }

    [Fact]
    public void Constructor_RejectsNullTransport() =>
        Assert.Throws<ArgumentNullException>(() => new ScpiPowerSupply(null!));
}
