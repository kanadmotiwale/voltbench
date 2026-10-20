using VoltBench.Core.Sequencing;

namespace VoltBench.Tests;

public class SequenceLoaderTests
{
    private const string Valid = """
        {
          // comments are allowed
          "name": "Smoke",
          "description": "Quick check",
          "steps": [
            { "name": "Set 5 V", "action": "SetVoltage", "value": 5 },
            { "name": "Check", "action": "MeasureDcVoltage", "low": 4.9, "high": 5.1, "channel": 2 },
          ]
        }
        """;

    [Fact]
    public void Parse_ReadsStepsEnumsAndLimits()
    {
        var sequence = SequenceLoader.Parse(Valid);

        Assert.Equal("Smoke", sequence.Name);
        Assert.Equal(2, sequence.Steps.Count);
        Assert.Equal(StepAction.SetVoltage, sequence.Steps[0].Action);
        Assert.Equal(1, sequence.Steps[0].Channel);
        Assert.Equal(2, sequence.Steps[1].Channel);
        Assert.Equal(4.9, sequence.Steps[1].Low);
        Assert.Null(sequence.Steps[0].High);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("{ \"name\": \"x\" }")]
    [InlineData("{ \"name\": \"x\", \"steps\": [] }")]
    [InlineData("{ \"steps\": [ { \"name\": \"a\", \"action\": \"Wait\" } ] }")]
    [InlineData("{ \"name\": \"x\", \"steps\": [ { \"name\": \"a\", \"action\": \"Explode\" } ] }")]
    [InlineData("{ \"name\": \"x\", \"steps\": [ { \"name\": \"a\", \"action\": \"Wait\", \"value\": -5 } ] }")]
    [InlineData("{ \"name\": \"x\", \"steps\": [ { \"name\": \"a\", \"action\": \"MeasureDcVoltage\", \"low\": 5, \"high\": 1 } ] }")]
    [InlineData("{ \"name\": \"x\", \"steps\": [ { \"name\": \"a\", \"action\": \"SetVoltage\", \"channel\": 0 } ] }")]
    public void Parse_RejectsInvalidInput(string json) =>
        Assert.Throws<SequenceFormatException>(() => SequenceLoader.Parse(json));

    [Fact]
    public void LoadFile_ReadsFromDisk()
    {
        var path = Path.Combine(Path.GetTempPath(), $"voltbench_{Guid.NewGuid():N}.json");
        File.WriteAllText(path, Valid);
        try
        {
            Assert.Equal("Smoke", SequenceLoader.LoadFile(path).Name);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadFile_MissingFileThrowsFormatException() =>
        Assert.Throws<SequenceFormatException>(() => SequenceLoader.LoadFile("/definitely/not/here.json"));
}
