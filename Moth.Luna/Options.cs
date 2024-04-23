using CommandLine;
using System.Text.Json.Serialization;

namespace Moth.Luna;

internal class Options
{
    [Option('v',
        "verbose",
        Required = false,
        HelpText = "Whether to include extensive logging.")]
    public bool Verbose { get; set; }
    
    [Option('c',
        "clear-cache",
        Required = false,
        HelpText = "Whether to clear dependency cache prior to build.")]
    public bool ClearCache { get; set; }

    [Option('n',
        "no-meta",
        Required = false,
        HelpText = "Whether to strip metadata from the output file. WARNING: disables reflection!")]
    public bool NoMetadata { get; set; }

    [Option("no-advanced-ir-opt",
        Required = false,
        HelpText = "Whether to forego advanced optimizations to the IR.")]
    public bool DoNotOptimizeIR { get; set; }
    
    [Option('p',
        "project",
        Required = false,
        HelpText = "The project file to use.")]
    public string ProjFile { get; set; }
    
    [Option("name",
        Required = false,
        HelpText = "When initializing a new project, pass this option with the name to use.")]
    public string ProjName { get; set; }
    
    [Option("lib",
        Required = false,
        HelpText = "When initializing a new project, pass this option to create a static library instead of an executable project.")]
    public bool InitLib { get; set; }
    
    [Option("run-args",
        Required = false,
        HelpText = "When running a project, pass this option with the arguments to use.")]
    public string RunArgs { get; set; }
    
    [Option("run-dir",
        Required = false,
        HelpText = "When running a project, pass this option with the working directory to use.")]
    public string RunDir { get; set; }
}
