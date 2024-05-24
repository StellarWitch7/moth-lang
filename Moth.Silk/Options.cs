using CommandLine;

namespace Moth.Silk;

public class Options
{
    [Option('v', "verbose", Required = false, HelpText = "Whether to include extensive logging.")]
    public bool Verbose { get; set; }

    [Option(
        'f',
        "force",
        Required = false,
        HelpText = "Whether to overwrite existing non-cache files."
    )]
    public bool Force { get; set; }

    [Option(
        'n',
        "namespace",
        Required = false,
        HelpText = "The top-level namespace for generated files."
    )]
    public string? TopNamespace { get; set; }

    [Option(
        'o',
        "output-dir",
        Required = false,
        HelpText = "The directory to place output files in."
    )]
    public string? OutputDir { get; set; }
}
