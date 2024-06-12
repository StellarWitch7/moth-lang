using System.IO.Compression;

namespace Moth.LLVM;

public class BuildOptions
{
    public bool DoOptimize { get; init; } = false;
    public Version Version { get; init; } = new Version();
    public CompressionLevel CompressionLevel { get; init; } = CompressionLevel.Optimal;
    public Language[] ExportLanguages { get; init; } = new Language[0];

    public bool DoExport
    {
        get { return ExportLanguages.Length > 0; }
    }
}

public enum Language
{
    C,
    CPP
}
