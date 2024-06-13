using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using CommandLine;
using LLVMSharp.Interop;
using Moth.AST;
using Moth.LLVM;
using Moth.Tokens;
using Spectre.Console;
using Version = Moth.LLVM.Metadata.Version;

namespace Moth.Compiler;

public class Program
{
    public static int Main(string[] args)
    {
        try
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
                        try
                        {
                            if (options.Verbose)
                            {
                                logger.Log($"Reading \"{filePath}\"");
                            }

                            string fileContents = File.ReadAllText(filePath);

                            // tokenize the contents of the file
                            try
                            {
                                if (options.Verbose)
                                {
                                    logger.Log($"Tokenizing \"{filePath}\"");
                                }

                                List<Token> tokens = Tokenizer.Tokenize(fileContents);

                                // convert to AST
                                try
                                {
                                    if (options.Verbose)
                                    {
                                        logger.Log($"Generating AST of \"{filePath}\"");
                                    }

                                    ScriptAST scriptAST = ASTGenerator.ProcessScript(
                                        new ParseContext(tokens)
                                    );
                                    scripts.Add(scriptAST);

                                    if (options.Verbose)
                                    {
                                        logger.WriteSeparator();
                                        logger.PrintTree(scriptAST);
                                        logger.WriteSeparator();
                                    }
                                }
                                catch (Exception e)
                                {
                                    logger.Error(
                                        $"Failed to parse tokens of \"{filePath}\" due to: {e}"
                                    );
                                    throw e;
                                }
                            }
                            catch (Exception e)
                            {
                                logger.Error($"Failed to tokenize \"{filePath}\" due to: {e}");
                                throw e;
                            }
                        }
                        catch (Exception e)
                        {
                            logger.Error($"Failed to get contents of \"{filePath}\" due to: {e}");
                            throw e;
                        }
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
                        using (
                            var compiler = new LLVMCompiler(
                                options.OutputFile,
                                logger,
                                new BuildOptions
                                {
                                    DoOptimize = !options.DoNotOptimizeIR,
                                    Version = Version.Parse(options.ModuleVersion ?? "0.0.0"),
                                    CompressionLevel = Utils.StringToCompLevel(
                                        options.CompressionLevel
                                    ),
                                    ExportLanguages = options
                                        .ExportLanguages.ToArray()
                                        .ExecuteOverAll(s => Utils.StringToLanguage(s))
                                }
                            )
                        )
                        {
                            if (options.MothLibraryFiles.Count() > 0)
                            {
                                logger.Log("Loading external Moth libraries...");

                                foreach (var path in options.MothLibraryFiles)
                                {
                                    compiler.LoadLibrary(path);
                                }
                            }

                            logger.Log("Compiling to LLVM IR...");

                            try
                            {
                                compiler.Compile(scripts);

                                if (options.NoMetadata)
                                {
                                    logger.Info("Skipping generation of assembly metadata...");
                                }
                                else
                                {
                                    logger.Log("(unsafe) Generating assembly metadata...");

                                    using (var fs = File.Create($"{options.OutputFile}.meta"))
                                        fs.Write(compiler.GenerateMetadata(options.OutputFile));
                                }
                            }
                            catch (Exception e)
                            {
                                if (options.Verbose)
                                {
                                    logger.WriteSeparator();
                                    logger.WriteUnsignedLine(compiler.Module.PrintToString());
                                    logger.WriteSeparator();
                                    logger.Log("Dumped LLVM IR for reviewal.");
                                }

                                Console.WriteLine(e);
                                throw e;
                            }

                            if (options.Verbose)
                            {
                                logger.WriteSeparator();
                                logger.WriteUnsignedLine(compiler.Module.PrintToString());
                                logger.WriteSeparator();
                            }

                            compiler.Module.PrintToFile($"{options.OutputFile}.ll");
                            logger.Log("Verifying IR validity...");
                            compiler.Module.Verify(
                                LLVMVerifierFailureAction.LLVMPrintMessageAction
                            );
                            string? linkerName = null;

                            if (outputType == OutputType.Executable)
                            {
                                // send to linker
                                try
                                {
                                    string bcFile = $"{options.OutputFile}.bc";
                                    string binOut = Path.Combine(dir, "bin");
                                    string path = Path.Combine(dir, bcFile);
                                    var arguments = new StringBuilder($"{path}");

                                    foreach (var lib in options.MothLibraryFiles)
                                    {
                                        arguments.Append($" {lib}");
                                    }

                                    foreach (var lib in options.CLibraryFiles)
                                    {
                                        arguments.Append($" {lib}");
                                    }

                                    logger.Log($"Outputting IR to \"{path}\"");
                                    compiler.Module.WriteBitcodeToFile(path);
                                    logger.Log("Compiling final product...");
                                    Directory.CreateDirectory(binOut);

                                    linkerName = "clang";
                                    arguments.Append($" -o {options.OutputFile}");

                                    if (OperatingSystem.IsWindows())
                                    {
                                        arguments.Append(".exe");
                                    }

                                    if (OperatingSystem.IsWindows())
                                    {
                                        arguments.Append(" --llegacy_stdio_definitions");
                                    }

                                    if (OperatingSystem.IsLinux())
                                    {
                                        arguments.Append(" -lpthread");
                                    }

                                    if (options.Verbose)
                                    {
                                        arguments.Append(" -v");
                                    }

                                    logger.Call(linkerName, arguments);
                                    logger.WriteSeparator();

                                    var linker = Process.Start(
                                        new ProcessStartInfo(linkerName, arguments.ToString())
                                        {
                                            WorkingDirectory = binOut,
                                            RedirectStandardOutput = true,
                                            RedirectStandardError = true,
                                            //UseShellExecute = true //TODO
                                        }
                                    );

                                    var linkerLogger = new Logger(linkerName);

                                    _ =
                                        linker
                                        ?? throw new Exception(
                                            $"Linker \"{linkerName}\" failed to start."
                                        );

                                    while (!linker.HasExited)
                                    {
                                        linkerLogger.WriteUnsignedLine(
                                            linker.StandardOutput.ReadToEnd()
                                        );
                                        linkerLogger.WriteUnsignedLine(
                                            linker.StandardError.ReadToEnd()
                                        );
                                    }

                                    linkerLogger.WriteSeparator();
                                    linkerLogger.ExitCode(linker.ExitCode);
                                }
                                catch (Exception e)
                                {
                                    linkerName ??= "UNKNOWN";

                                    logger.WriteEmptyLine();
                                    logger.Error(
                                        $"Failed to interact with {linkerName} due to: {e}"
                                    );
                                    throw e;
                                }
                            }
                            else if (outputType == OutputType.StaticLib)
                            {
                                var path = Path.Combine(dir, $"{options.OutputFile}.mothlib.bc");
                                logger.Log($"Outputting IR to \"{path}\"");
                                compiler.Module.WriteBitcodeToFile(path);
                            }
                            else
                            {
                                throw new NotImplementedException("Output type not supported.");
                            }

                            logger.Log("Generating headers for supported export languages...");

                            foreach (var lang in compiler.Options.ExportLanguages)
                            {
                                logger.Log(
                                    $"{lang} header generated at {Path.Combine(Environment.CurrentDirectory, compiler.Header.Build(lang))}"
                                );
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Error($"Failed to compile due to: {e}");
                        throw e;
                    }
                });
        }
        catch (Exception e)
        {
            return -1;
        }

        return 0;
    }
}
