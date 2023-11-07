using CommandLine;

namespace Moth_cmd;

internal class Options
{
    [Option('v', "verbose", Required = false, HelpText = "Whether to include extensive logging.")]
    public bool Verbose { get; set; }

    [Option("msvc", Required = false, HelpText = "Whether to use MSVC instead of Clang on Windows. Requires '--windows-sdk' if used.")]
    public bool UseMSVC { get; set; }

    [Option("debug-test", Required = false, HelpText = "Whether to run the output on success.")]
    public bool RunTest { get; set; }

    [Option('o', "output", Required = true, HelpText = "The name of the file to output. Please forego the extension.")]
    public string OutputFile { get; set; }

    [Option("windows-sdk", Required = false, HelpText = "The path to the Windows SDK. Required if --msvc is set, useless otherwise.")]
    public string WindowsSDKPath { get; set; }

    [Option('i', "input", Required = true, HelpText = "The files to compile.")]
    public IEnumerable<string> InputFiles { get; set; }

    [Option("moth-libs", Required = false, HelpText = "External LLVM IR files to include in the compiled program.")]
    public IEnumerable<string> MothLibraryFiles { get; set; }
}
