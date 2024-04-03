using CommandLine;
using System.ComponentModel.Design.Serialization;

namespace Moth.Compiler;

internal class Options
{
    [Option('v',
        "verbose",
        Required = false,
        HelpText = "Whether to include extensive logging.")]
    public bool Verbose { get; set; }

    [Option('n',
        "no-meta",
        Required = false,
        HelpText = "Whether to strip metadata from the output file. WARNING: makes libraries act like C libraries!")]
    public bool NoMetadata { get; set; }
    
    [Option("debug-test",
        Required = false,
        HelpText = "Whether to run the output on success.")]
    public bool RunTest { get; set; }

    [Option("no-advanced-ir-opt",
        Required = false,
        HelpText = "Whether to forego advanced optimizations to the IR.")]
    public bool DoNotOptimizeIR { get; set; }

    [Option('o',
        "output-file",
        Required = true,
        HelpText = "The name of the file to output. Please forego the extension.")]
    public string? OutputFile { get; set; }

    [Option('i',
        "input",
        Required = true,
        HelpText = "The files to compile.")]
    public IEnumerable<string>? InputFiles { get; set; }

    [Option('t',
        "output-type",
        Required = true,
        HelpText = "The type of file to output. Options are \"exe\" and \"lib\".")]
    public string OutputType { get; set; }
    
    [Option("moth-libs",
        Required = false,
        HelpText = "External Moth library files to include in the compiled program.")]
    public IEnumerable<string>? MothLibraryFiles { get; set; }
    
    [Option("c-libs",
        Required = false,
        HelpText = "External C library files to include in the compiled program.")]
    public IEnumerable<string>? CLibraryFiles { get; set; }
}

public enum OutputType
{
    Executable,
    StaticLib
}