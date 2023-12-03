using CommandLine;

namespace Moth.CLI;

internal class Options
{
    [Option('v',
        "verbose",
        Required = false,
        HelpText = "Whether to include extensive logging.")]
    public bool Verbose { get; set; }

    [Option("debug-test",
        Required = false,
        HelpText = "Whether to run the output on success.")]
    public bool RunTest { get; set; }

    [Option("advanced-ir-opt",
        Required = false,
        HelpText = "Whether to apply advanced optimizations to the IR. True by default. ",
        Default = true)]
    public bool OptimizeIR { get; set; }

    [Option('o',
        "output",
        Required = true,
        HelpText = "The name of the file to output. Please forego the extension.")]
    public string? OutputFile { get; set; }

    [Option('i',
        "input",
        Required = true,
        HelpText = "The files to compile.")]
    public IEnumerable<string>? InputFiles { get; set; }

    [Option("moth-libs",
        Required = false,
        HelpText = "External Moth library files to include in the compiled program.")]
    public IEnumerable<string>? MothLibraryFiles { get; set; }
    
    [Option("c-libs",
        Required = false,
        HelpText = "External C library files to include in the compiled program.")]
    public IEnumerable<string>? CLibraryFiles { get; set; }
}
