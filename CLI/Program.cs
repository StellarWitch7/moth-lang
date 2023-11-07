using System.Text;
using Moth;
using Moth.Tokens;
using Moth.AST;
using Moth.LLVM;
using LLVMSharp.Interop;
using CommandLine;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CLI;

internal class Program
{
    unsafe static void Main(string[] args)
    {
        try
        {
            var dir = Environment.CurrentDirectory;
            Logger logger = new Logger("moth");

            Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(options =>
            {
                var compiler = new CompilerContext(options.OutputFile);
                var scripts = new List<ScriptAST>();

                logger.WriteLine($"Building {options.OutputFile}...");
                LLVM.LinkInMCJIT();
                LLVM.InitializeAllTargetInfos();
                LLVM.InitializeAllTargets();
                LLVM.InitializeAllTargetMCs();
                LLVM.InitializeAllAsmParsers();
                LLVM.InitializeAllAsmPrinters();

                foreach (var filePath in options.InputFiles)
                {
                    try
                    {
                        if (options.Verbose)
                        {
                            logger.WriteLine($"Reading \"{filePath}\"");
                        }

                        var fileContents = File.ReadAllText(filePath);

                        //Tokenize the contents of the file
                        try
                        {
                            if (options.Verbose)
                            {
                                logger.WriteLine($"Tokenizing \"{filePath}\"");
                            }

                            var tokens = Tokenizer.Tokenize(fileContents);

                            //Convert to AST
                            try
                            {
                                if (options.Verbose)
                                {
                                    logger.WriteLine($"Generating AST of \"{filePath}\"");
                                }

                                var scriptAST = TokenParser.ProcessScript(new ParseContext(tokens));
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
                    LLVMCompiler.Compile(compiler, scripts);

                    if (options.Verbose)
                    {
                        logger.WriteSeparator();
                        logger.WriteUnsignedLine(compiler.Module.PrintToString());
                        logger.WriteSeparator();
                    }

                    logger.WriteLine("Verifying IR validity...");
                    compiler.Module.Verify(LLVMVerifierFailureAction.LLVMPrintMessageAction);
                    string linkerName = null;

                    //Send to linker
                    try
                    {
                        var @out = $"{options.OutputFile}.obj";
                        var path = Path.Join(dir, @out);
                        var arguments = new StringBuilder($"{path}");

                        var cpu = new string(LLVM.GetHostCPUName());
                        var features = new string(LLVM.GetHostCPUFeatures());

                        var optLevel = LLVMCodeGenOptLevel.LLVMCodeGenLevelDefault; //TODO: add argument to configure this level
                        var target = LLVMTargetRef.GetTargetFromTriple(LLVMTargetRef.DefaultTriple);
                        var machine = target.CreateTargetMachine(LLVMTargetRef.DefaultTriple, cpu, features, optLevel,
                            LLVMRelocMode.LLVMRelocDefault, LLVMCodeModel.LLVMCodeModelDefault);

                        logger.WriteLine($"Writing to object file \"{path}\"");
                        machine.EmitToFile(compiler.Module, path, LLVMCodeGenFileType.LLVMObjectFile);
                        logger.WriteLine($"Compiling final product...");

                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            if (options.UseMSVC)
                            {
                                throw new NotImplementedException(); //TODO: fix this up

                                linkerName = "MSVC";
                                string msvc = null;
                                var directories = new List<string>();
                                var startPath = @"C:\Program Files\Microsoft Visual Studio\";
                                var startPathx86 = @"C:\Program Files (x86)\Microsoft Visual Studio\";
                                arguments.Append($" /OUT:{options.OutputFile}.exe /RELEASE msvcrt.lib");

                                if (Directory.Exists(startPath))
                                {
                                    foreach (var d in Directory.GetDirectories(startPath))
                                    {
                                        directories.Add(d);
                                    }
                                }

                                if (Directory.Exists(startPathx86))
                                {
                                    foreach (var d in Directory.GetDirectories(startPathx86))
                                    {
                                        directories.Add(d);
                                    }
                                }

                                foreach (var d in directories)
                                {
                                    var combinedPath = Path.Join(d, @"BuildTools\VC\Tools\MSVC\");

                                    if (Directory.Exists(combinedPath))
                                    {
                                        foreach (var d2 in Directory.GetDirectories(combinedPath))
                                        {
                                            var combinedPath2 = Path.Join(d2, @"bin\Hostx64\x64");
                                            var libPath = Path.Join(d2, @"lib\x64");

                                            if (Directory.Exists(combinedPath2))
                                            {
                                                foreach (var f in Directory.GetFiles(combinedPath2))
                                                {
                                                    if (f.EndsWith("link.exe"))
                                                    {
                                                        msvc = f;
                                                    }
                                                }
                                            }

                                            if (Directory.Exists(libPath))
                                            {
                                                arguments.Append($" /LIBPATH:\"{libPath}\"");
                                                logger.WriteLine($"Located libs at \"{libPath}\"");
                                            }
                                        }
                                    }
                                }

                                if (options.Verbose)
                                {
                                    arguments.Append(" /VERBOSE");
                                }

                                if (msvc != null)
                                {
                                    linkerName = msvc;
                                }
                                else
                                {
                                    throw new Exception("Could not locate MSVC. Please ensure it is installed.");
                                }
                            }
                            else
                            {
                                linkerName = "clang";
                                arguments.Append($" -o {options.OutputFile}.exe -llegacy_stdio_definitions");

                                if (options.Verbose)
                                {
                                    arguments.Append(" -v");
                                }
                            }
                        }
                        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                        {
                            throw new NotImplementedException("Sorry, Linux is not supported at the moment, but we do have plans to!");
                        }
                        else
                        {
                            throw new Exception("Operating system not supported.");
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

                        Logger linkerLogger = new Logger(linkerName);

                        while (!linker.HasExited)
                        {
                            linkerLogger.WriteUnsignedLine(linker.StandardOutput.ReadToEnd());
                            linkerLogger.WriteUnsignedLine(linker.StandardError.ReadToEnd());
                        }

                        linkerLogger.WriteSeparator();
                        linkerLogger.WriteLine($"Exited with code {linker.ExitCode}");

                        if (options.RunTest && linker.ExitCode == 0)
                        {
                            string testName = null;

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

                                Logger testLogger = new Logger(testName);
                                testProgram.WaitForExit();
                                testLogger.WriteEmptyLine();
                                testLogger.WriteSeparator();
                                testLogger.WriteLine($"Exited with code {testProgram.ExitCode}");
                            }
                            catch (Exception e)
                            {
                                if (testName == null)
                                {
                                    testName = "UNKNOWN";
                                }

                                logger.WriteEmptyLine();
                                logger.WriteLine($"Failed to interact with {testName} due to: {e}");
                                return;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (linkerName == null)
                        {
                            linkerName = "UNKNOWN";
                        }

                        logger.WriteEmptyLine();
                        logger.WriteLine($"Failed to interact with {linkerName} due to: {e}");
                        return;
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
                    return;
                }
            });
        }
        catch (Exception e)
        {
            Console.WriteLine($"Exited due to: {e}");
            return;
        }
    }
}