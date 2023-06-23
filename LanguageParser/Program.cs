using System.Numerics;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LanguageParser
{
    internal class Program
    {
        private bool _isRunning = true;

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
                else if (ParseExpression(input))
                {

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

            Console.WriteLine("Unknown command. Check your spelling.");
            return false;
        }

        bool ParseExpression(string input)
        {
            var stream = new PeekStream(input);
            string content = stream.Content;

            if (content.Length == 0)
            {
                return false;
            }

            while (true)
            {
                if (stream.Current == null)
                {
                    return true;
                }
                else if (stream.Current == ' ')
                {
                    stream.MoveNext();
                }
                else if (ParseNumber(stream, out int number))
                {
                    Console.WriteLine($"number {number}");
                }
                else if (ParseVariable(stream, out string? varName))
                {
                    Console.WriteLine($"variable {varName}");
                }
                else if (ParseOperator(stream, out Operator op)
                {

                }
                else
                {
                    Console.WriteLine("Invalid expression.");
                    return true;
                }
            }
        }

        bool ParseOperator(PeekStream stream, out Operator op)
        {
            throw new NotImplementedException();
        }

        bool ParseNumber(PeekStream stream, out int number)
        {
            StringBuilder builder = new StringBuilder();

            while (stream.Current is char c)
            {
                if (IsDigit(c))
                {
                    builder.Append(c);
                    stream.MoveNext();
                }
                else
                {
                    break;
                }
            }

            if (builder.Length > 0)
            {
                if (int.TryParse(builder.ToString(), out number))
                {
                    return true;
                }
                else
                {
                    Console.WriteLine($"{builder.ToString()} is not a valid int.");
                    return false;
                }
            }

            number = default;
            return false;
        }

        bool ParseVariable(PeekStream stream, out string? varName)
        {
            StringBuilder builder = new StringBuilder();

            while (stream.Current is char c)
            {
                if (IsVariableChar(c) || (builder.Length > 0 && IsDigit(c)))
                {
                    builder.Append(c);
                    stream.MoveNext();
                }
                else
                {
                    break;
                }
            }

            if (builder.Length > 0)
            {
                varName = builder.ToString();
                return true;
            }

            varName = default;
            return false;
        }

        bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        bool IsLetter(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }

        bool IsVariableChar(char c)
        {
            return IsLetter(c) || c == '_';
        }
    }

    internal enum Operator
    {
        Add,
        Subtract
    }
}