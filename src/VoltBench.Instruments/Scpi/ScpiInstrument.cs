using VoltBench.Core.Instruments;

namespace VoltBench.Instruments.Scpi;

/// <summary>Common IEEE 488.2 behaviour shared by all SCPI drivers.</summary>
public abstract class ScpiInstrument : IInstrument
{
    private const int MaxErrorsRead = 20;

    protected ScpiInstrument(IScpiTransport transport)
    {
        Transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    public IScpiTransport Transport { get; }

    public string Identify() => Transport.Query("*IDN?").Trim();

    public void Reset() => Transport.Write("*RST");

    public IReadOnlyList<ScpiError> CheckErrors()
    {
        var errors = new List<ScpiError>();
        for (var i = 0; i < MaxErrorsRead; i++)
        {
            var error = ScpiParser.ParseError(Transport.Query("SYST:ERR?"));
            if (!error.IsError)
            {
                break;
            }

            errors.Add(error);
        }

        return errors;
    }

    protected double QueryDouble(string command) => ScpiParser.ParseDouble(Transport.Query(command));
}
