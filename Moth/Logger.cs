using Spectre.Console;

namespace Moth;

public class Logger : TextWriter
{
    private static string LogDirectory { get; }
    private static string LogFile { get; }
    private static string BackupLogFile { get; }
    private static FileStream Stream { get; }
    private static StreamWriter Writer { get; }

    public string Name { get; set; }
    public override Encoding Encoding { get; }

    static Logger()
    {
        LogDirectory = Path.Combine(Environment.CurrentDirectory, "logs");
        LogFile = Path.Combine(LogDirectory, "latest.log");
        BackupLogFile = Path.Combine(LogDirectory, "backup.log");

        Directory.CreateDirectory(LogDirectory);

        if (File.Exists(LogFile))
            File.Move(LogFile, BackupLogFile, true);

        Stream = File.Create(LogFile);
        Writer = new StreamWriter(Stream) { AutoFlush = true };
    }

    public Logger(string name)
        : base()
    {
        Name = name;
        Encoding = Writer.Encoding;
    }

    public Logger MakeSubLogger(string subname)
    {
        return new Logger($"{Name}/{subname}");
    }

    public void Log(string message)
    {
        WriteLine(message, Style.Plain);
    }

    public void Info(string message)
    {
        WriteLine($"INFO: {message}", new Style(Color.Aqua));
    }

    public void Warn(string message)
    {
        WriteLine($"WARN: {message}", new Style(Color.Orange1, null));
    }

    public void Error(string message)
    {
        WriteLine($"ERR: {message}", new Style(Color.Red, null, Decoration.Bold));
    }

    public void Error(Exception e) => Error(e.ToString());

    public void Call(string programName, string arguments)
    {
        WriteLine(
            $"CALL: {programName} {arguments}",
            new Style(Color.SpringGreen3, null, Decoration.Italic)
        );
    }

    public void Call(string programName, StringBuilder arguments)
    {
        Call(programName, arguments.ToString());
    }

    public void ExitCode(int code)
    {
        Info($"Exit with code {code}");
    }

    public void PrintTree<T>(T rootNode)
        where T : ITreeNode
    {
        rootNode.PrintTree(this);
    }

    public void WriteEmptyLine() => WriteUnsignedLine("\n", Style.Plain);

    public void WriteSeparator() =>
        WriteUnsignedLine("###################################", Style.Plain);

    private void WriteLine(string message, Style style)
    {
        string signedMessage = $"[{Name}] {message}";
        WriteUnsignedLine(signedMessage, style);
    }

    public void WriteUnsignedLine(string message, Style style) =>
        WriteUnsigned($"{message}\n", style);

    public void WriteUnsignedLine(string message) => WriteUnsignedLine(message, Style.Plain);

    public void WriteUnsigned(string message, Style style)
    {
        AnsiConsole.Write(new Text(message, style));
        Write(message);
    }

    public void WriteUnsigned(string message)
    {
        WriteUnsigned(message, Style.Plain);
    }

    public void WriteUnsigned(char ch) => WriteUnsigned(ch.ToString(), Style.Plain);

    public override void Flush() => Writer.Flush();

    public override void Write(char value) => WriteUnsigned(value);

    public override void Write(string? value) => Writer.Write(value ?? String.Empty);
}
