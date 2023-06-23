using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
using System.Text.RegularExpressions;

namespace LanguageParser
{
    internal class Program
    {
        private bool _isRunning = true;
        private StringBuilder _script = new StringBuilder();

        static void Main(string[] args)
        {
            new Program().Run();
        }

        void Run()
        {
            Console.WriteLine("Welcome to the Storm shell.");
            Console.WriteLine("Enter '$/help' to get a list of commands.");

            while (_isRunning)
            {
                Console.Write("|>> ");
                string input = Console.ReadLine();

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

        bool ProcessCommand(string input)
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
                Tokenizer tokenizer = new Tokenizer(_script);

                if (input.StartsWith("$/run @"))
                {
                    var fileContents = File
                        .ReadAllText(input
                        .Substring(input
                        .IndexOf("@") + 1));
                    tokenizer = new Tokenizer(new StringBuilder()
                        .Append(fileContents));

                    WriteBlock(fileContents, "\r\n|\r|\n", 0.6f);
                }
                
                tokenizer.ParseScript();
                tokenizer.PrintTokens(0.6f); //Testing
                _script.Clear();
                Compiler compiler = new Compiler(tokenizer.Tokens);
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

        public static void WriteBlock(string content, string regex, float delay)
        {
            var strings = Regex.Split(content, regex);
            foreach (var s in strings)
            {
                foreach (char c in s)
                {
                    Console.Write(c);
                    Thread.Sleep((int)(delay * 50));
                }

                Console.WriteLine();
            }
        }
    }
}