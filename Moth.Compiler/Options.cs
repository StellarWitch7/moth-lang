using System.ComponentModel.Design.Serialization;
using CommandLine;

namespace Moth.Compiler;

internal class Options
{
    [Option('v', "verbose", Required = false, HelpText = "Whether to include extensive logging.")]
    public bool Verbose { get; set; }

    [Option(
        'n',
        "no-meta",
        Required = false,
        HelpText = "Whether to strip metadata from the output file. WARNING: disables reflection!"
    )]
    public bool NoMetadata { get; set; }

    [Option(
        "no-advanced-ir-opt",
        Required = false,
        HelpText = "Whether to forego advanced optimizations to the IR."
    )]
    public bool DoNotOptimizeIR { get; set; }

    [Option(
        'o',
        "output-file",
        Required = true,
        HelpText = "The name of the file to output. Please forego the extension."
    )]
    public string? OutputFile { get; set; }

    [Option('i', "input", Required = true, HelpText = "The files to compile.")]
    public IEnumerable<string>? InputFiles { get; set; }

    [Option(
        't',
        "output-type",
        Required = true,
        HelpText = "The type of file to output. Options are \"exe\" and \"lib\"."
    )]
    public string OutputType { get; set; }

    [Option(
        'V',
        "module-version",
        Required = false,
        HelpText = "The version of the compiled module."
    )]
    public string? ModuleVersion { get; set; }

    [Option(
        'g',
        "compression-level",
        Required = false,
        HelpText = "The type of compression to use for mothlib embedded metadata. Only really matters for huge projects. "
            + "Options are: \"none\", \"low\", \"mid\", and \"high\"."
    )]
    public string CompressionLevel { get; set; } = "mid";

    [Option(
        'm',
        "moth-libs",
        Required = false,
        HelpText = "External Moth library files to include in the compiled program."
    )]
    public IEnumerable<string>? MothLibraryFiles { get; set; }

    [Option(
        'c',
        "c-libs",
        Required = false,
        HelpText = "External C library files to include in the compiled program."
    )]
    public IEnumerable<string>? CLibraryFiles { get; set; }

    [Option(
        'e',
        "export-for",
        Required = false,
        HelpText = "Languages to @Export() functions for. Use the file extension for the language."
    )]
    public IEnumerable<string>? ExportLanguages { get; set; }
}

public enum OutputType
{
    Executable,
    StaticLib
}
