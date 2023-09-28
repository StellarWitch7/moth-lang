using System.Text.RegularExpressions;
using System.Text;
using Moth.Tokens;
using Moth.AST;
using Moth.LLVM;
using LLVMSharp.Interop;
using CommandLine.Text;
using CommandLine;

namespace Moth_cmd;

internal class Program
{
    private bool _isRunning = true;
    private readonly StringBuilder _script = new();

    public static void Main(string[] args)
    {
        Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(options => 
        {
            var compiler = new CompilerContext(options.OutputFile);
            var scripts = new List<ScriptAST>();

            foreach (var filePath in options.InputFiles)
            {
                try
                {
                    var fileContents = File.ReadAllText(filePath);

                    //Tokenize the contents of the file
                    try
                    {
                        var tokens = Tokenizer.Tokenize(fileContents);

                        //Convert to AST
                        try
                        {
                            var scriptAST = TokenParser.ProcessScript(new ParseContext(tokens));
                            scripts.Add(scriptAST);

                            if (options.Verbose)
                            {
                                Console.WriteLine();
                                Console.WriteLine(scriptAST.GetDebugString("  "));
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
                LLVMCompiler.Compile(compiler, scripts.ToArray());

                if (options.Verbose)
                {
                    Console.WriteLine();
                    compiler.Module.Dump();
                    Console.WriteLine();
                    compiler.Module.Verify(LLVMVerifierFailureAction.LLVMPrintMessageAction);
                }
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
        public bool Verbose { get; set; } = false;

        [Option('o', "output", Required = true, HelpText = "The file to output.")]
        public string OutputFile { get; set; }

        [Option('i', "input", Required = true, HelpText = "The files to compile.")]
        public List<string> InputFiles { get; set; } = new List<string>();

        [Option("moth-libs", Required = false, HelpText = "External LLVM IR files to include in the compiled program.")]
        public List<string> MothLibraryFiles { get; set; } = new List<string>();
    }

    private void Run()
    {
        Console.WriteLine("Welcome to the Moth shell.");
        Console.WriteLine("Enter '$/help' to get a list of commands.");

        while (_isRunning)
        {
            Console.Write("|>> ");
            var input = Console.ReadLine() ?? string.Empty;

            if (!input.StartsWith("$/"))
            {
                _script.Append(input);
                _script.Append('\n');
            }
            else
            {
                if (!ProcessCommand(input))
                {
                    _script.Clear();
                }
            }
        }
    }

    private bool ProcessCommand(string input)
    {
        if (input == "$/exit")
        {
            _isRunning = false;
            return true;
        }
        else if (input.StartsWith("$/run"))
        {
            try
            {
                List<Token> tokens;
                if (input.StartsWith("$/run @"))
                {
                    var fileContents = File.ReadAllText(input[(input.IndexOf("@", StringComparison.Ordinal) + 1)..]);
                    tokens = Tokenizer.Tokenize(fileContents);

                    Console.WriteLine();
                    WriteBlock(fileContents, "\r\n|\r|\n", 0.002f);
                }
                else
                {
                    tokens = Tokenizer.Tokenize(_script.ToString());
                }

                try
                {
                    Console.WriteLine();
                    var scriptAST = TokenParser.ProcessScript(new ParseContext(tokens));
                    Console.WriteLine();
                    Console.WriteLine(scriptAST.GetDebugString("  ")); //Testing
                    Console.WriteLine();

                    try
                    {
                        var compiler = new CompilerContext("script");
                        LLVMCompiler.DefineScript(compiler, scriptAST);
                        LLVMCompiler.CompileScript(compiler, scriptAST);
                        compiler.Module.Dump(); //Testing
                        Console.WriteLine(); //Testing
                        compiler.Module.Verify(LLVMVerifierFailureAction.LLVMPrintMessageAction); //Testing

                        _script.Clear();
                        return true;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Compiler error: {e.Message}");
                        return false;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Parser error: {e.Message}");
                    return false;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Tokenizer error: {e.Message}");
                return false;
            }
        }
        else if (input == "$/clear")
        {
            _script.Clear();
            Console.Clear();
            return true;
        }
        else if (input == "$/help")
        {
            Console.WriteLine("- '$/exit' to leave the program.");
            Console.WriteLine("- '$/run' to execute your script. " +
                "Adding a path like so '$/run @D:\\user\\scripts\\test.txt' will run the script within that file.");
            Console.WriteLine("- '$/clear' to clear your script and the console.");
            return true;
        }

        Console.WriteLine("Unknown command. Check your spelling.");
        return false;
    }

    private static void WriteBlock(string content, string regex, float delay)
    {
        var strings = Regex.Split(content, regex);
        foreach (var s in strings)
        {
            foreach (var c in s)
            {
                Console.Write(c);
                Thread.Sleep((int)(delay * 50));
            }

            Console.WriteLine();
        }
    }
}