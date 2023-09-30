using System.Text.RegularExpressions;
using System.Text;
using Moth.Tokens;
using Moth.AST;
using Moth.LLVM;
using LLVMSharp.Interop;
using CommandLine.Text;
using CommandLine;
using System.Diagnostics;

namespace Moth_cmd;

internal class Program
{
    static void Main(string[] args)
    {
        var dir = Environment.CurrentDirectory;
        Logger logger = new Logger("moth");

        Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(options => 
        {
            var compiler = new CompilerContext(options.OutputFile);
            var scripts = new List<ScriptAST>();

            logger.WriteLine($"Building {options.OutputFile}...");

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
                        }
                    }
                    catch (Exception e)
                    {
                        logger.WriteLine($"Failed to tokenize \"{filePath}\" due to: {e}");
                    }
                }
                catch (Exception e)
                {
                    logger.WriteLine($"Failed to get contents of \"{filePath}\" due to: {e}");
                }
            }

            //Compile
            try
            {
                logger.WriteLine("Compiling to LLVM IR...");
                LLVMCompiler.Compile(compiler, scripts.ToArray());

                if (options.Verbose)
                {
                    logger.WriteSeparator();
                    logger.WriteUnsignedLine(compiler.Module.PrintToString());
                    logger.WriteSeparator();
                }

                compiler.Module.Verify(LLVMVerifierFailureAction.LLVMPrintMessageAction);

                //Send to Clang
                try
                {
                    logger.WriteLine("Sending IR to Clang...");
                    logger.WriteSeparator();

                    var arguments = new StringBuilder($"-o {options.OutputFile} -llegacy_stdio_definitions");
                    var file = $"{compiler.ModuleName}.bc";
                    var outPath = Path.Join(dir, file);

                    if (options.Verbose)
                    {
                        arguments.Append(" -v");
                    }

                    compiler.Module.WriteBitcodeToFile(outPath);
                    arguments.Append(' ');
                    arguments.Append(file);

                    var clang = Process.Start(new ProcessStartInfo
                    {
                        FileName = "clang",
                        WorkingDirectory = dir,
                        Arguments = arguments.ToString(),
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    });

                    Logger clangLogger = new Logger("clang");

                    clang.WaitForExit();
                    clangLogger.WriteUnsignedLine(clang.StandardOutput.ReadToEnd());
                    clangLogger.WriteUnsignedLine(clang.StandardError.ReadToEnd());
                    clangLogger.WriteSeparator();
                    clangLogger.WriteLine($"Exited with code {clang.ExitCode}");
                }
                catch (Exception e)
                {
                    logger.WriteLine($"Failed to interact with Clang due to: {e}");
                }
            }
            catch (Exception e)
            {
                logger.WriteLine($"Failed to compile due to: {e}");

                if (!options.Verbose)
                {
                    logger.WriteLine("Dumping LLVM IR for reviewal...");
                    logger.WriteSeparator();
                    logger.WriteLine(compiler.Module.PrintToString());
                    logger.WriteSeparator();
                }
            }
        });
    }

    internal class Options
    {
        [Option('v', "verbose", Required = false, HelpText = "Whether to include extensive logging.")]
        public bool Verbose { get; set; }

        [Option('o', "output", Required = true, HelpText = "The file to output.")]
        public string OutputFile { get; set; }

        [Option('i', "input", Required = true, HelpText = "The files to compile.")]
        public IEnumerable<string> InputFiles { get; set; }

        [Option("moth-libs", Required = false, HelpText = "External LLVM IR files to include in the compiled program.")]
        public IEnumerable<string> MothLibraryFiles { get; set; }
    }
}