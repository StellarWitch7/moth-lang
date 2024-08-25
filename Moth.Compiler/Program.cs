using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using CommandLine;
using LLVMSharp.Interop;
using Moth.AST;
using Spectre.Console;

namespace Moth.Compiler;

public class Program
{
    public static int Main(string[] args)
    {
        string dir = Environment.CurrentDirectory;
        var logger = new Logger("mothc");

        Parser
            .Default.ParseArguments<Options>(args)
            .WithParsed(options =>
            {
                _ = options.OutputType ?? throw new Exception("No output file type provided.");
                _ = options.InputFiles ?? throw new Exception("No input files provided.");
                _ = options.OutputFile ?? throw new Exception("No output file name provided.");

                var scripts = new List<ScriptAST>();
                var outputType = options.OutputType switch
                {
                    "exe" => OutputType.Executable,
                    "lib" => OutputType.StaticLib,
                };

                logger.Log($"Building {options.OutputFile}...");

                foreach (string filePath in options.InputFiles)
                {
                    if (options.Verbose)
                    {
                        logger.Log($"Parsing \"{filePath}\"");
                    }

                    var parser = new MothParser();
                    var result = parser.Parse(File.ReadAllText(filePath), filePath);

                    if (result is not null)
                    {
                        scripts.Add(result);
                        logger.WriteSeparator();
                        logger.WriteUnsigned(result.GetSource());
                        logger.WriteSeparator();
                    }
                    else
                        throw new Exception($"Failed to parse \"{filePath}\".");
                }

                // init LLVM stuff
                // idk what half of these are
                LLVMSharp.Interop.LLVM.LinkInMCJIT();
                LLVMSharp.Interop.LLVM.InitializeAllTargetInfos();
                LLVMSharp.Interop.LLVM.InitializeAllTargets();
                LLVMSharp.Interop.LLVM.InitializeAllTargetMCs();
                LLVMSharp.Interop.LLVM.InitializeAllAsmParsers();
                LLVMSharp.Interop.LLVM.InitializeAllAsmPrinters();

                // compile
                try
                {
                    //TODO
                }
                catch (Exception e)
                {
                    logger.Error($"Failed to compile due to: {e}");
                    throw e;
                }
            });

        return 0;
    }
}
