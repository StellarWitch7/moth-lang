using CommandLine;
using Moth.AST;
using Moth.LLVM;
using Moth.Tokens;
using LLVMSharp.Interop;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Moth.Compiler;

internal class Program
{
    private static int Main(string[] args)
    {
        try
        {
            string dir = Environment.CurrentDirectory;
            var logger = new Logger("mothc");

            Parser.Default.ParseArguments<Options>(args).WithParsed(options =>
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

                logger.WriteLine($"Building {options.OutputFile}...");

                foreach (string filePath in options.InputFiles)
                {
                    try
                    {
                        if (options.Verbose)
                        {
                            logger.WriteLine($"Reading \"{filePath}\"");
                        }

                        string fileContents = File.ReadAllText(filePath);

                        // tokenize the contents of the file
                        try
                        {
                            if (options.Verbose)
                            {
                                logger.WriteLine($"Tokenizing \"{filePath}\"");
                            }

                            List<Token> tokens = Tokenizer.Tokenize(fileContents);

                            // convert to AST
                            try
                            {
                                if (options.Verbose)
                                {
                                    logger.WriteLine($"Generating AST of \"{filePath}\"");
                                }

                                ScriptAST scriptAST = ASTGenerator.ProcessScript(new ParseContext(tokens));
                                scripts.Add(scriptAST);

                                if (options.Verbose)
                                {
                                    logger.WriteSeparator();
                                    logger.WriteUnsignedLine(scriptAST.GetDebugString("  "));
                                    logger.WriteSeparator();
                                }
                            }
                            catch (Exception e)
                            {
                                logger.WriteLine($"Failed to parse tokens of \"{filePath}\" due to: {e}");
                                throw e;
                            }
                        }
                        catch (Exception e)
                        {
                            logger.WriteLine($"Failed to tokenize \"{filePath}\" due to: {e}");
                            throw e;
                        }
                    }
                    catch (Exception e)
                    {
                        logger.WriteLine($"Failed to get contents of \"{filePath}\" due to: {e}");
                        throw e;
                    }
                }

                LLVMSharp.Interop.LLVM.LinkInMCJIT();
                LLVMSharp.Interop.LLVM.InitializeAllTargetInfos();
                LLVMSharp.Interop.LLVM.InitializeAllTargets();
                LLVMSharp.Interop.LLVM.InitializeAllTargetMCs();
                LLVMSharp.Interop.LLVM.InitializeAllAsmParsers();
                LLVMSharp.Interop.LLVM.InitializeAllAsmPrinters();
                var compiler = new LLVMCompiler(options.OutputFile, !options.DoNotOptimizeIR);
                
                // compile
                try
                {
                    if (options.MothLibraryFiles.LongCount() > 0)
                    {
                        logger.WriteLine("Loading external Moth libraries...");

                        foreach (var path in options.MothLibraryFiles)
                        {
                            compiler.LoadLibrary(path);
                        }
                    }
                    
                    logger.WriteLine("Compiling to LLVM IR...");

                    try
                    {
                        compiler.Compile(scripts);

                        if (options.NoMetadata)
                        {
                            logger.WriteLine("Skipping generation of assembly metadata...");
                        }
                        else
                        {
                            logger.WriteLine("(unsafe) Generating assembly metadata...");
                            var fs = File.Create(Path.Join(Environment.CurrentDirectory, $"{options.OutputFile}.meta"));
                            fs.Write(compiler.GenerateMetadata(options.OutputFile));
                            fs.Close();
                        }
                    }
                    catch (Exception e)
                    {
                        if (options.Verbose)
                        {
                            logger.WriteSeparator();
                            logger.WriteUnsignedLine(compiler.Module.PrintToString());
                            logger.WriteSeparator();
                            logger.WriteLine("Dumped LLVM IR for reviewal.");
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

                    logger.WriteLine("Verifying IR validity...");
                    compiler.Module.Verify(LLVMVerifierFailureAction.LLVMPrintMessageAction);
                    string? linkerName = null;

                    if (outputType == OutputType.Executable)
                    {
                        // send to linker
                        try
                        {
                            string @out = $"{options.OutputFile}.bc";
                            string path = Path.Join(dir, @out);
                            var arguments = new StringBuilder($"{path}");

                            foreach (var lib in options.MothLibraryFiles)
                            {
                                arguments.Append($" {lib}");
                            }
                            
                            foreach (var lib in options.CLibraryFiles)
                            {
                                arguments.Append($" {lib}");
                            }

                            logger.WriteLine($"Outputting IR to \"{path}\"");
                            compiler.Module.WriteBitcodeToFile(path);
                            logger.WriteLine("Compiling final product...");
                            
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

                            logger.WriteLine($"Attempting to call {linkerName} with arguments \"{arguments}\"");
                            logger.WriteSeparator();
                            
                            var linker = Process.Start(new ProcessStartInfo
                            {
                                FileName = linkerName,
                                WorkingDirectory = dir,
                                Arguments = arguments.ToString(),
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                            });

                            var linkerLogger = new Logger(linkerName);

                            _ = linker ?? throw new Exception($"Linker \"{linkerName}\" failed to start.");

                            while (!linker.HasExited)
                            {
                                linkerLogger.WriteUnsignedLine(linker.StandardOutput.ReadToEnd());
                                linkerLogger.WriteUnsignedLine(linker.StandardError.ReadToEnd());
                            }

                            linkerLogger.WriteSeparator();
                            linkerLogger.WriteLine($"Exited with code {linker.ExitCode}");
                        }
                        catch (Exception e)
                        {
                            linkerName ??= "UNKNOWN";

                            logger.WriteEmptyLine();
                            logger.WriteLine($"Failed to interact with {linkerName} due to: {e}");
                            throw e;
                        }
                    }
                    else if (outputType == OutputType.StaticLib)
                    {
                        var path = Path.Join(dir, $"{options.OutputFile}.mothlib.bc");
                        logger.WriteLine($"Outputting IR to \"{path}\"");
                        compiler.Module.WriteBitcodeToFile(path);
                    }
                    else
                    {
                        throw new NotImplementedException("Output type not supported.");
                    }
                }
                catch (Exception e)
                {
                    logger.WriteLine($"Failed to compile due to: {e}");
                    throw e;
                }
            });
        }
        catch (Exception e)
        {
            Console.WriteLine($"Exited due to: {e}");
            return -1;
        }

        return 0;
    }
}