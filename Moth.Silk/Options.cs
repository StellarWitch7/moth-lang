using CommandLine;

namespace Moth.Silk;

public class Options
{
    [Option('v',
        "verbose",
        Required = false,
        HelpText = "Whether to include extensive logging.")]
    public bool Verbose { get; set; }
    
    [Option('n',
        "namespace",
        Required = true,
        HelpText = "The top-level namespace for generated files.")]
    public string? TopNamespace { get; set; }
    
    [Option('o',
        "output-dir",
        Required = true,
        HelpText = "The directory to place output files in.")]
    public string? OutputDir { get; set; }

    [Option('i',
        "input",
        Required = true,
        HelpText = "The files to generate bindings for.")]
    public IEnumerable<string>? InputFiles { get; set; }
}
