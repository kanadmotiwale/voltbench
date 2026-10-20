using VoltBench.Core.Instruments;
using VoltBench.Instruments.Scpi;
using VoltBench.Instruments.Simulation;

namespace VoltBench.Tests;

public class SimulatedInstrumentTests
{
    private readonly ScpiPowerSupply _psu;
    private readonly ScpiMultimeter _dmm;
    private readonly SimulatedBench _bench = new(loadOhms: 10, sourceOhms: 0, noiseFraction: 0);

    public SimulatedInstrumentTests()
    {
        var supplyTransport = new SimulatedPowerSupplyTransport(_bench);
        var dmmTransport = new SimulatedMultimeterTransport(_bench);
        supplyTransport.Open();
        dmmTransport.Open();
        _psu = new ScpiPowerSupply(supplyTransport);
        _dmm = new ScpiMultimeter(dmmTransport);
    }

    [Fact]
    public void Identify_ReturnsModelStrings()
    {
        Assert.Contains("SIM-PSU3", _psu.Identify());
        Assert.Contains("SIM-DMM6", _dmm.Identify());
    }

    [Fact]
    public void OutputOff_ReadsZero()
    {
        _psu.SetVoltage(5);
        Assert.Equal(0, _dmm.MeasureDcVoltage());
        Assert.Equal(0, _psu.MeasureCurrent());
    }

    [Theory]
    [InlineData(1, 0.1)]
    [InlineData(5, 0.5)]
    [InlineData(12, 1.2)]
    public void DmmFollowsSupplyThroughResistiveLoad(double volts, double amps)
    {
        _psu.SetCurrentLimit(3);
        _psu.SetVoltage(volts);
        _psu.SetOutput(true);

        Assert.Equal(volts, _dmm.MeasureDcVoltage(), 9);
        Assert.Equal(amps, _dmm.MeasureDcCurrent(), 9);
        Assert.Equal(amps, _psu.MeasureCurrent(), 9);
        Assert.Equal(10, _dmm.MeasureResistance(), 9);
    }

    [Fact]
    public void CurrentLimitClampsAndDropsVoltage()
    {
        _psu.SetCurrentLimit(0.25);
        _psu.SetVoltage(5);
        _psu.SetOutput(true);

        Assert.Equal(0.25, _dmm.MeasureDcCurrent(), 9);
        Assert.Equal(2.5, _dmm.MeasureDcVoltage(), 9);
    }

    [Fact]
    public void SourceResistanceCausesDroop()
    {
        _bench.SourceOhms = 1;
        _psu.SetCurrentLimit(3);
        _psu.SetVoltage(11);
        _psu.SetOutput(true);

        Assert.Equal(10, _dmm.MeasureDcVoltage(), 9);
    }

    [Fact]
    public void NoiseStaysSmallAndIsSeedable()
    {
        var noisy = new SimulatedBench(noiseFraction: 0.001, seed: 42);
        var transport = new SimulatedPowerSupplyTransport(noisy);
        transport.Open();
        var psu = new ScpiPowerSupply(transport);
        psu.SetVoltage(5);
        psu.SetOutput(true);

        var readings = Enumerable.Range(0, 50).Select(_ => psu.MeasureVoltage()).ToArray();
        Assert.All(readings, r => Assert.InRange(r, 4.9, 5.0));
        Assert.True(readings.Distinct().Count() > 1);
    }

    [Fact]
    public void OutOfRangeVoltageIsRejectedAndQueued()
    {
        _psu.SetVoltage(99);

        var errors = _psu.CheckErrors();
        Assert.Equal(-222, Assert.Single(errors).Code);
        Assert.Empty(_psu.CheckErrors());
    }

    [Fact]
    public void UnknownCommandQueuesUndefinedHeader()
    {
        ((SimulatedPowerSupplyTransport)_psu.Transport).Write("BOGUS:CMD 1");
        Assert.Equal(-113, Assert.Single(_psu.CheckErrors()).Code);
    }

    [Fact]
    public void ResetRestoresDefaults()
    {
        _psu.SetVoltage(8);
        _psu.SetOutput(true);
        _psu.Reset();

        Assert.Equal(0, _dmm.MeasureDcVoltage());
        Assert.Equal("0", _psu.Transport.Query("OUTP?"));
    }

    [Fact]
    public void LongFormMnemonicsAreAccepted()
    {
        _psu.Transport.Write("VOLTAGE 4");
        _psu.Transport.Write("OUTPUT ON");

        Assert.Equal(4, _dmm.MeasureDcVoltage(), 9);
    }

    [Fact]
    public void SecondChannelIsIndependentAndUnloaded()
    {
        _psu.SelectChannel(2);
        _psu.SetVoltage(3);
        _psu.SetOutput(true);

        Assert.Equal(3, _psu.MeasureVoltage(), 9);
        Assert.Equal(0, _psu.MeasureCurrent());
        Assert.Equal(0, _dmm.MeasureDcVoltage());
    }

    [Fact]
    public void InvalidChannelIsRejected()
    {
        _psu.SelectChannel(7);
        Assert.Equal(-222, Assert.Single(_psu.CheckErrors()).Code);
    }

    [Fact]
    public void ReadUsesConfiguredFunction()
    {
        _psu.SetCurrentLimit(3);
        _psu.SetVoltage(5);
        _psu.SetOutput(true);
        _dmm.Transport.Write("CONF:CURR:DC");

        Assert.Equal(0.5, _dmm.Read(), 9);
        _dmm.ConfigureDcVoltage();
        Assert.Equal(5, _dmm.Read(), 9);
    }

    [Fact]
    public void QueryOnClosedTransportThrows()
    {
        var transport = new SimulatedPowerSupplyTransport(_bench);
        Assert.Throws<InvalidOperationException>(() => transport.Query("*IDN?"));
    }

    [Fact]
    public void QueryOfCommandWithoutResponseTimesOut() =>
        Assert.Throws<InstrumentException>(() => _psu.Transport.Query("VOLT 1"));
}
