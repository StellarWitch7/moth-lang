namespace Moth;

public class Logger
{
    static string LogDirectory { get; } = Path.Join(Environment.CurrentDirectory, "logs");
    static string LogFile { get; } = Path.Join(LogDirectory, "latest.log");
    static string BackupLogFile { get; } = Path.Join(LogDirectory, "backup.log");
    static bool BeganLogging { get; set; } = false;
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

    public void WriteEmptyLine()
    {
        WriteUnsignedLine("\n");
    }

    public void WriteSeparator()
    {
        WriteUnsignedLine("///////////////////////////////////////////////////////////");
    }

    public void WriteLine(string message)
    {
        string signedMessage = $"[{Name}] {message}";
        WriteUnsignedLine(signedMessage);
    }

    public void WriteUnsignedLine(string message)
    {
        WriteUnsigned(message + '\n');
    }

    public void WriteUnsigned(string message)
    {
        WriteToLog(message);
        Console.Write(message);
    }

    public void WriteToLog(string message)
    {
        FileStream fs = File.Open(LogFile, FileMode.Append);
        StreamWriter writer = new StreamWriter(fs);
        writer.AutoFlush = true;

        writer.Write(message);
        writer.Close();
    }

    public void WriteToLog(char ch)
    {
        WriteToLog(ch.ToString());
    }

    public void WriteUnsigned(char ch)
    {
        WriteUnsigned(ch.ToString());
    }
}
