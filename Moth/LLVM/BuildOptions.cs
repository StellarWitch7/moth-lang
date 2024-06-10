namespace Moth.LLVM;

public class BuildOptions
{
    public bool DoOptimize { get; init; } = false;
    public Language[] ExportLanguages { get; init; }

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
