namespace Moth;

public class MultiWriter : TextWriter
{
    public TextWriter[] Writers { get; }
    public override Encoding Encoding { get; }

    public MultiWriter(TextWriter[] writers, Encoding encoding)
    {
        Writers = writers;
        Encoding = encoding;
    }

    public MultiWriter(TextWriter[] writers)
        : this(writers, Encoding.Default) { }

    public void Do(Action<TextWriter> func)
    {
        foreach (var writer in Writers)
        {
            func(writer);
        }
    }

    public override void Close() => Do(w => w.Close());

    public override void Write(string? value) => Do(w => w.Write(value));
}
