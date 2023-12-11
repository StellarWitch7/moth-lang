using CommandLine;
using Moth.AST;
using Moth.LLVM;
using Moth.Tokens;
using LLVMSharp.Interop;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Moth.CLI;

internal class Program
{
    private static int Main(string[] args)
    {
        try
        {
            string dir = Environment.CurrentDirectory;
            var logger = new Logger("moth/CLI");

            Parser.Default.ParseArguments<Options>(args).WithParsed(options =>
            {
                _ = options.InputFiles ?? throw new Exception("No input files provided.");
                _ = options.OutputFile ?? throw new Exception("No output file name provided.");
                
                var scripts = new List<ScriptAST>();

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

                LLVMSharp.Interop.LLVM.LinkInMCJIT();
                LLVMSharp.Interop.LLVM.InitializeAllTargetInfos();
                LLVMSharp.Interop.LLVM.InitializeAllTargets();
                LLVMSharp.Interop.LLVM.InitializeAllTargetMCs();
                LLVMSharp.Interop.LLVM.InitializeAllAsmParsers();
                LLVMSharp.Interop.LLVM.InitializeAllAsmPrinters();
                var compiler = new LLVMCompiler(options.OutputFile, !options.DoNotOptimizeIR);
                
                //Compile
                try
                {
                    logger.WriteLine("Compiling to LLVM IR...");

                    try
                    {
                        compiler.Compile(scripts);
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
                        return;
                    }

                    if (options.Verbose)
                    {
                        logger.WriteSeparator();
                        logger.WriteUnsignedLine(new HeaderSerializer().Serialize(compiler));
                        logger.WriteSeparator();
                        logger.WriteSeparator();
                        logger.WriteUnsignedLine(compiler.Module.PrintToString());
                        logger.WriteSeparator();
                    }

                    logger.WriteLine("Verifying IR validity...");
                    compiler.Module.Verify(LLVMVerifierFailureAction.LLVMPrintMessageAction);
                    string? linkerName = null;

                    //Send to linker
                    try
                    {
                        string @out = $"{options.OutputFile}.obj";
                        string path = Path.Join(dir, @out);
                        var arguments = new StringBuilder($"{path}");

                        logger.WriteLine("(unsafe) Retrieving host machine info...");
                        
                        unsafe
                        {
                            string cpu = new string(LLVMSharp.Interop.LLVM.GetHostCPUName());
                            string features = new string(LLVMSharp.Interop.LLVM.GetHostCPUFeatures());

                            //TODO: add argument to configure this level
                            LLVMCodeGenOptLevel optLevel = LLVMCodeGenOptLevel.LLVMCodeGenLevelDefault;
                            var target = LLVMTargetRef.GetTargetFromTriple(LLVMTargetRef.DefaultTriple);
                            LLVMTargetMachineRef machine = target.CreateTargetMachine(LLVMTargetRef.DefaultTriple,
                                cpu,
                                features,
                                optLevel,LLVMRelocMode.LLVMRelocDefault,
                                LLVMCodeModel.LLVMCodeModelDefault);

                            logger.WriteLine($"Writing to object file \"{path}\"");
                            machine.EmitToFile(compiler.Module, path, LLVMCodeGenFileType.LLVMObjectFile);
                        }
                        
                        logger.WriteLine($"Compiling final product...");
                        
                        linkerName = "clang";
                        arguments.Append($" -o {options.OutputFile}.exe");

                        if (OperatingSystem.IsWindows())
                        {
                            arguments.Append(" --llegacy_stdio_definitions");
                        }

                        if (options.Verbose)
                        {
                            arguments.Append(" -v");
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
                    logger.WriteLine($"Failed to compile due to: {e}");
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