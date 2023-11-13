using CommandLine;
using Moth.AST;
using Moth.LLVM;
using Moth.Tokens;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Moth.CLI;

internal class Program
{
    private static unsafe void Main(string[] args)
    {
        try
        {
            string dir = Environment.CurrentDirectory;
            var logger = new Logger("moth");

            Parser.Default.ParseArguments<Options>(args).WithParsed(options =>
            {
                _ = options.InputFiles ?? throw new Exception("No input files provided.");
                _ = options.OutputFile ?? throw new Exception("No output file name provided.");
                
                var compiler = new LLVMCompiler(options.OutputFile);
                var scripts = new List<ScriptAST>();

                logger.WriteLine($"Building {options.OutputFile}...");
                LLVMSharp.Interop.LLVM.LinkInMCJIT();
                LLVMSharp.Interop.LLVM.InitializeAllTargetInfos();
                LLVMSharp.Interop.LLVM.InitializeAllTargets();
                LLVMSharp.Interop.LLVM.InitializeAllTargetMCs();
                LLVMSharp.Interop.LLVM.InitializeAllAsmParsers();
                LLVMSharp.Interop.LLVM.InitializeAllAsmPrinters();

                foreach (string filePath in options.InputFiles)
                {
                    try
                    {
                        if (options.Verbose)
                        {
                            logger.WriteLine($"Reading \"{filePath}\"");
                        }

                        string fileContents = File.ReadAllText(filePath);

                        //Tokenize the contents of the file
                        try
                        {
                            if (options.Verbose)
                            {
                                logger.WriteLine($"Tokenizing \"{filePath}\"");
                            }

                            List<Token> tokens = Tokenizer.Tokenize(fileContents);

                            //Convert to AST
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
                                return;
                            }
                        }
                        catch (Exception e)
                        {
                            logger.WriteLine($"Failed to tokenize \"{filePath}\" due to: {e}");
                            return;
                        }
                    }
                    catch (Exception e)
                    {
                        logger.WriteLine($"Failed to get contents of \"{filePath}\" due to: {e}");
                        return;
                    }
                }

                //Compile
                try
                {
                    logger.WriteLine("Compiling to LLVM IR...");
                    compiler.Compile(scripts);

                    if (options.Verbose)
                    {
                        logger.WriteSeparator();
                        logger.WriteUnsignedLine(compiler.Module.PrintToString());
                        logger.WriteSeparator();
                    }

                    logger.WriteLine("Verifying IR validity...");
                    compiler.Module.Verify(LLVMSharp.Interop.LLVMVerifierFailureAction.LLVMPrintMessageAction);
                    string? linkerName = null;

                    //Send to linker
                    try
                    {
                        string @out = $"{options.OutputFile}.obj";
                        string path = Path.Join(dir, @out);
                        var arguments = new StringBuilder($"{path}");

                        string cpu = new string(LLVMSharp.Interop.LLVM.GetHostCPUName());
                        string features = new string(LLVMSharp.Interop.LLVM.GetHostCPUFeatures());

                        LLVMSharp.Interop.LLVMCodeGenOptLevel optLevel //TODO: add argument to configure this level
                            = LLVMSharp.Interop.LLVMCodeGenOptLevel.LLVMCodeGenLevelDefault;
                        var target
                            = LLVMSharp.Interop.LLVMTargetRef.GetTargetFromTriple(LLVMSharp.Interop.LLVMTargetRef.DefaultTriple);
                        LLVMSharp.Interop.LLVMTargetMachineRef machine
                            = target.CreateTargetMachine(LLVMSharp.Interop.LLVMTargetRef.DefaultTriple,
                                cpu,
                                features,
                                optLevel,LLVMSharp.Interop.LLVMRelocMode.LLVMRelocDefault,
                                LLVMSharp.Interop.LLVMCodeModel.LLVMCodeModelDefault);

                        logger.WriteLine($"Writing to object file \"{path}\"");
                        machine.EmitToFile(compiler.Module, path, LLVMSharp.Interop.LLVMCodeGenFileType.LLVMObjectFile);
                        logger.WriteLine($"Compiling final product...");

                        if (options.UseMSVC && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            throw new NotImplementedException();
                        }
                        // else if (options.UseGCC && RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                        // {
                        //     throw new NotImplementedException();
                        // }
                        else
                        {
                            linkerName = "clang";
                            arguments.Append($" -o {options.OutputFile}.exe -llegacy_stdio_definitions");

                            if (options.Verbose)
                            {
                                arguments.Append(" -v");
                            }
                        }

                        logger.WriteLine($"Attempting to call {linkerName} with arguments <{arguments}>");
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

                        if (options.RunTest && linker.ExitCode == 0)
                        {
                            string? testName = null;

                            try
                            {
                                logger.WriteLine($"Preparing to run test...");
                                logger.WriteSeparator();
                                testName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                                    ? $"{options.OutputFile}.exe"
                                    : options.OutputFile;

                                var testProgram = Process.Start(new ProcessStartInfo
                                {
                                    FileName = Path.Join(dir, testName),
                                    WorkingDirectory = dir,
                                });

                                _ = testProgram ?? throw new Exception($"Failed to start \"{testName}\".");

                                var testLogger = new Logger(testName);
                                testProgram.WaitForExit();
                                testLogger.WriteEmptyLine();
                                testLogger.WriteSeparator();
                                testLogger.WriteLine($"Exited with code {testProgram.ExitCode}");
                            }
                            catch (Exception e)
                            {
                                testName ??= "UNKNOWN";

                                logger.WriteEmptyLine();
                                logger.WriteLine($"Failed to interact with {testName} due to: {e}");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        linkerName ??= "UNKNOWN";

                        logger.WriteEmptyLine();
                        logger.WriteLine($"Failed to interact with {linkerName} due to: {e}");
                    }
                }
                catch (Exception e)
                {
                    logger.WriteEmptyLine();

                    if (options.Verbose)
                    {
                        logger.WriteSeparator();
                        logger.WriteLine(compiler.Module.PrintToString());
                        logger.WriteSeparator();
                        logger.WriteLine("Dumped LLVM IR for reviewal.");
                    }

                    logger.WriteLine($"Failed to compile due to: {e}");
                }
            });
        }
        catch (Exception e)
        {
            Console.WriteLine($"Exited due to: {e}");
        }
    }
}