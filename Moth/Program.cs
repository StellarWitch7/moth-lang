using System.Text.RegularExpressions;
using Moth.Compiler.Tokens;
using Moth.Compiler.AST;
using System.Text;
using LLVMSharp.Interop;
using Moth.Compiler.LLVM;

namespace Moth.Compiler;

internal class Program
{
    private bool _isRunning = true;
    private readonly StringBuilder _script = new();

    public static void Main()
    {
        new Program().Run();
    }

    private void Run()
    {
        Console.WriteLine("Welcome to the Moth shell.");
        Console.WriteLine("Enter '$/help' to get a list of commands.");

        while (_isRunning)
        {
            Console.Write("|>> ");
            var input = Console.ReadLine() ?? string.Empty;

            if (ProcessCommand(input))
            {
                
            }
            else
            {
                _script.Append(input);
                _script.Append('\n');
            }
        }
    }

    private bool ProcessCommand(string input)
    {
        if (!input.StartsWith("$/"))
        {
            return false;
        }

        if (input == "$/exit")
        {
            _isRunning = false;
            return true;
        }
        else if (input.StartsWith("$/run"))
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
            
	        Console.WriteLine();
            PrintTokens(tokens, 0.01f); //Testing
            var scriptAST = TokenParser.ProcessScript(new ParseContext(tokens));
            Console.WriteLine();
            Console.WriteLine(scriptAST.GetDebugString("  ")); //Testing
            Console.WriteLine();
            LLVMCodeGenerator.ConvertScript(new CompilerContext("script"), scriptAST);

            _script.Clear();
            return true;
        }
        else if (input == "$/clear")
        {
            _script.Clear();
            Console.Clear();
        }
        else if (input == "$/help")
        {
            Console.WriteLine("- '$/exit' to leave the program.");
            Console.WriteLine("- '$/run' to execute your script. " +
                "Adding a path like so '$/run @D:\\user\\scripts\\test.txt' will run the script within that file.");
            Console.WriteLine("- '$/clear' to clear your script and the console.");
        }

        Console.WriteLine("Unknown command. Check your spelling.");
        return false;
    }

    private static void PrintTokens(List<Token> tokens, float delay)
    {
	        foreach (var token in tokens)
	        {
		        Console.WriteLine(token.ToString());
		        Thread.Sleep((int)(delay * 50));
	        }
    }

    private static void PrintStatements(List<StatementNode> statements, float delay)
    {
	        foreach (var statement in statements)
	        {
		        Console.WriteLine(statement.GetDebugString("\t"));
		        Thread.Sleep((int)(delay * 50));
	        }
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