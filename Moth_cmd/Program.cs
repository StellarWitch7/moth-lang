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
    public static void Main(string[] args)
    {
        Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(options => 
        {
            var dir = Environment.CurrentDirectory;
            var compiler = new CompilerContext(options.OutputFile);
            var scripts = new List<ScriptAST>();

            Console.WriteLine($"Building {options.OutputFile}...");
            Console.WriteLine();

            foreach (var filePath in options.InputFiles)
            {
                try
                {
                    if (options.Verbose)
                    {
                        Console.WriteLine($"Reading \"{filePath}\"");
                    }

                    var fileContents = File.ReadAllText(filePath);

                    //Tokenize the contents of the file
                    try
                    {
                        if (options.Verbose)
                        {
                            Console.WriteLine($"Tokenizing \"{filePath}\"");
                        }

                        var tokens = Tokenizer.Tokenize(fileContents);

                        //Convert to AST
                        try
                        {
                            if (options.Verbose)
                            {
                                Console.WriteLine($"Generating AST of \"{filePath}\"");
                                Console.WriteLine();
                            }

                            var scriptAST = TokenParser.ProcessScript(new ParseContext(tokens));
                            scripts.Add(scriptAST);

                            if (options.Verbose)
                            {
                                Console.WriteLine(scriptAST.GetDebugString("  "));
                                Console.WriteLine();
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Failed to parse tokens of \"{filePath}\" due to: {e.Message}");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Failed to tokenize \"{filePath}\" due to: {e.Message}");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to get contents of \"{filePath}\" due to: {e.Message}");
                }
            }

            //Compile
            try
            {
                if (options.Verbose)
                {
                    Console.WriteLine("Compiling ASTs...");
                    Console.WriteLine();
                }

                LLVMCompiler.Compile(compiler, scripts.ToArray());

                if (options.Verbose)
                {
                    compiler.Module.Dump();
                    Console.WriteLine();
                    compiler.Module.Verify(LLVMVerifierFailureAction.LLVMPrintMessageAction);
                    Console.WriteLine();
                }

                var arguments = new StringBuilder($"-o {options.OutputFile}");
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
                });

                clang.WaitForExit();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to compile due to: {e.Message}");
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