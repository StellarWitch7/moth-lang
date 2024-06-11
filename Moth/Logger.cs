using Spectre.Console;

namespace Moth;

public class Logger
{
    private static string LogDirectory { get; } = Path.Join(Environment.CurrentDirectory, "logs");
    private static string LogFile { get; } = Path.Join(LogDirectory, "latest.log");
    private static string BackupLogFile { get; } = Path.Join(LogDirectory, "backup.log");
    private static bool BeganLogging { get; set; } = false;
    public string Name { get; set; }

    public Logger(string name)
    {
        Name = name;
        Directory.CreateDirectory(LogDirectory);

        if (!BeganLogging && File.Exists(LogFile))
        {
            if (File.Exists(BackupLogFile))
            {
                File.Delete(BackupLogFile);
            }

            File.Move(LogFile, BackupLogFile);
        }

        BeganLogging = true;
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
        WriteToLog(message);
        Spectre.Console.AnsiConsole.Write(new Text(message, style));
    }

    public void WriteUnsigned(string message)
    {
        WriteUnsigned(message, Style.Plain);
    }

    public void WriteToLog(string message)
    {
        FileStream fs = File.Open(LogFile, FileMode.Append);
        var writer = new StreamWriter(fs) { AutoFlush = true };

        writer.Write(message);
        writer.Close();
    }

    public void WriteToLog(char ch) => WriteToLog(ch.ToString());

    public void WriteUnsigned(char ch) => WriteUnsigned(ch.ToString(), Style.Plain);
}
