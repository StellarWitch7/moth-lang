using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

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
            while (_isRunning)
            {
                Console.Write("-> ");
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
            if (!input.StartsWith("/"))
            {
                return false;
            }

            if (input == "/exit")
            {
                _isRunning = false;
                return true;
            }
            else if (input == "/run")
            {
                Tokenizer _tokenizer = new Tokenizer(_script);
                _tokenizer.ParseStatements();
                _tokenizer.PrintTokens(); //Testing
                _script.Clear();
                return true;
            }

            Console.WriteLine("Unknown command. Check your spelling.");
            return false;
        }
    }
}