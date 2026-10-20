using System.Text.Json;
using System.Text.Json.Serialization;

namespace VoltBench.Core.Sequencing;

/// <summary>Reads and writes sequences as JSON.</summary>
public static class SequenceLoader
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    public static TestSequence LoadFile(string path)
    {
        try
        {
            return Parse(File.ReadAllText(path));
        }
        catch (IOException ex)
        {
            throw new SequenceFormatException($"Cannot read '{path}': {ex.Message}", ex);
        }
    }

    public static TestSequence Parse(string json)
    {
        TestSequence? sequence;
        try
        {
            sequence = JsonSerializer.Deserialize<TestSequence>(json, Options);
        }
        catch (JsonException ex)
        {
            throw new SequenceFormatException($"Invalid sequence JSON: {ex.Message}", ex);
        }

        if (sequence is null)
        {
            throw new SequenceFormatException("Sequence file is empty.");
        }

        Validate(sequence);
        return sequence;
    }

    public static string Serialize(TestSequence sequence) => JsonSerializer.Serialize(sequence, Options);

    private static void Validate(TestSequence sequence)
    {
        if (string.IsNullOrWhiteSpace(sequence.Name))
        {
            throw new SequenceFormatException("Sequence name is required.");
        }

        if (sequence.Steps.Count == 0)
        {
            throw new SequenceFormatException($"Sequence '{sequence.Name}' has no steps.");
        }

        for (var i = 0; i < sequence.Steps.Count; i++)
        {
            var step = sequence.Steps[i];
            var where = $"Step {i + 1} ('{step.Name}')";
            if (!Enum.IsDefined(step.Action))
            {
                throw new SequenceFormatException($"{where}: unknown action.");
            }

            if (step.Channel < 1)
            {
                throw new SequenceFormatException($"{where}: channel must be 1 or greater.");
            }

            if (step.Action is StepAction.Wait or StepAction.SetCurrentLimit && step.Value < 0)
            {
                throw new SequenceFormatException($"{where}: value must not be negative.");
            }

            if (step.Low > step.High)
            {
                throw new SequenceFormatException($"{where}: low limit exceeds high limit.");
            }
        }
    }
}
