namespace Moth;

public class CriticalException : Exception
{
    public string? CustomMessage { get; init; }

    public CriticalException(string? customMessage)
    {
        CustomMessage = customMessage;
    }

    public override string Message
    {
        get { return ToString(); }
    }

    public override string ToString()
    {
        string err = $"CRITICAL ERROR; REPORT TO DEVELOPMENT ASAP.";

        if (CustomMessage is not null)
            err = $"{err}\n{CustomMessage}";

        return err;
    }
}
