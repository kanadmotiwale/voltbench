using VoltBench.Core.Instruments;

namespace VoltBench.Tests.Fakes;

/// <summary>Records every command and replies from canned responses.</summary>
public sealed class FakeTransport : IScpiTransport
{
    private readonly Dictionary<string, Queue<string>> _responses = new();

    public string ResourceName => "FAKE::INSTR";

    public bool IsOpen { get; private set; }

    public int TimeoutMs { get; set; } = 1000;

    public List<string> Sent { get; } = [];

    public void Respond(string command, params string[] responses) =>
        _responses[command] = new Queue<string>(responses);

    public void Open() => IsOpen = true;

    public void Close() => IsOpen = false;

    public void Write(string command) => Sent.Add(command);

    public string Query(string command)
    {
        Sent.Add(command);
        if (_responses.TryGetValue(command, out var queue) && queue.Count > 0)
        {
            return queue.Count == 1 ? queue.Peek() : queue.Dequeue();
        }

        throw new InstrumentException($"No canned response for '{command}'.");
    }

    public void Dispose() => Close();
}
