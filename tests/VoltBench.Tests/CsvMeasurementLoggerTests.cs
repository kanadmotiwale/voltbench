using VoltBench.Core.Logging;
using VoltBench.Core.Sequencing;

namespace VoltBench.Tests;

public sealed class CsvMeasurementLoggerTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"voltbench_{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }

    private static StepResult Result(string name, double? measured, StepOutcome outcome = StepOutcome.Pass) =>
        new(0, new TestStep { Name = name, Action = StepAction.MeasureDcVoltage, Low = 4.9, High = 5.1 },
            outcome, new DateTimeOffset(2025, 3, 14, 9, 26, 53, 500, TimeSpan.Zero), 5.0, measured);

    [Fact]
    public void WritesHeaderAndRows()
    {
        using var logger = new CsvMeasurementLogger(_folder);
        logger.Begin("Sweep");
        logger.Log(Result("Measure 5 V", 4.975));
        logger.End();

        var lines = File.ReadAllLines(logger.FilePath!);
        Assert.Equal(CsvMeasurementLogger.Header, lines[0]);
        Assert.Equal("2025-03-14T09:26:53.500+00:00,Measure 5 V,5,4.975,4.9,5.1,V,PASS", lines[1]);
    }

    [Fact]
    public void CreatesFolderAndOneFilePerRun()
    {
        using var logger = new CsvMeasurementLogger(_folder);
        logger.Begin("Run");
        var first = logger.FilePath;
        logger.Begin("Run");

        Assert.NotEqual(first, logger.FilePath);
        Assert.Equal(2, Directory.GetFiles(_folder, "*.csv").Length);
    }

    [Fact]
    public void FileNameIsSanitized()
    {
        using var logger = new CsvMeasurementLogger(_folder);
        logger.Begin("Voltage/Sweep: 1-12 V");

        var name = Path.GetFileName(logger.FilePath!);
        Assert.DoesNotContain(' ', name);
        Assert.DoesNotContain(':', name);
        Assert.StartsWith("Voltage_Sweep", name);
    }

    [Fact]
    public void EscapesCommasAndQuotesInStepNames()
    {
        using var logger = new CsvMeasurementLogger(_folder);
        logger.Begin("Escape");
        logger.Log(Result("a,\"b\"", 1));
        logger.End();

        Assert.Contains("\"a,\"\"b\"\"\"", File.ReadAllLines(logger.FilePath!)[1]);
    }

    [Fact]
    public void MissingMeasurementIsEmptyField()
    {
        using var logger = new CsvMeasurementLogger(_folder);
        logger.Begin("Err");
        logger.Log(Result("broken", null, StepOutcome.Error));
        logger.End();

        Assert.EndsWith("5,,4.9,5.1,V,ERROR", File.ReadAllLines(logger.FilePath!)[1]);
    }

    [Fact]
    public void NumbersIgnoreCurrentCulture()
    {
        var original = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("fr-FR");
            using var logger = new CsvMeasurementLogger(_folder);
            logger.Begin("Culture");
            logger.Log(Result("x", 1.5));
            logger.End();

            Assert.Contains(",1.5,", File.ReadAllLines(logger.FilePath!)[1]);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = original;
        }
    }

    [Fact]
    public void LogBeforeBegin_Throws()
    {
        using var logger = new CsvMeasurementLogger(_folder);
        Assert.Throws<InvalidOperationException>(() => logger.Log(Result("x", 1)));
    }
}
