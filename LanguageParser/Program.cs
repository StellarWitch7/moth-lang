using LanguageParser.Token;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LanguageParser
{
    internal class Program
    {
        private bool _isRunning = true;
        private StringBuilder _script = new StringBuilder();
        private List<ParsedToken> _tokenList = new List<ParsedToken>();

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
                ParseStatements();
                PrintTokens(_tokenList); //Testing
                _script.Clear();
                _tokenList.Clear();
                return true;
            }

            Console.WriteLine("Unknown command. Check your spelling.");
            return false;
        }

        void PrintTokens(List<ParsedToken> tokens)
        {
            foreach (ParsedToken token in tokens)
            {
                Console.WriteLine(token.ToString());
            }
        }

        bool ParseStatements()
        {
            var stream = new PeekStream(_script.ToString());
            string content = stream.Content;

            if (content.Length == 0)
            {
                return false;
            }

            while (stream.Current != null)
            {
                ParseStatement(stream, _tokenList);
            }

            return true;
        }

        bool ParseStatement(PeekStream stream, List<ParsedToken> tokenList)
        {
            while (stream.Current != null)
            {
                if (stream.Current == ';')
                {
                    tokenList.Add(new EndStatementToken());
                    stream.MoveNext();
                    return true;
                }
                else if (IsSpace(stream.Current))
                {
                    stream.MoveNext();
                }
                else if (ParseVariable(stream, out NameToken vToken))
                {
                    tokenList.Add(vToken);
                }
                else if (stream.Current == '=')
                {
                    tokenList.Add(new AssignmentToken());
                    stream.MoveNext();
                    if (!ParseExpression(stream, tokenList))
                    {
                        Console.WriteLine("Expected expression.");
                    }
                }
            }

            return false;
        }

        bool IsSpace(char? c)
        {
            return (c == ' ' || c == '\t' || c == '\r' || c == '\n');
        }

        bool ParseExpression(PeekStream stream, List<ParsedToken> tokenList)
        {
            while (stream.Current != null)
            {
                if (stream.Current == ';')
                {
                    return true;
                }
                else if (stream.Current == ' ')
                {
                    stream.MoveNext();
                }
                else if (ParseNumber(stream, out NumberToken nToken))
                {
                    tokenList.Add(nToken);
                }
                else if (ParseVariable(stream, out NameToken vToken))
                {
                    tokenList.Add(vToken);
                }
                else if (ParseOperator(stream, out OperatorToken oToken))
                {
                    tokenList.Add(oToken);
                }
                else
                {
                    Console.WriteLine("Invalid expression.");
                    return false;
                }
            }

            return false;
        }

        bool ParseOperator(PeekStream stream, out OperatorToken token)
        {
            token = default;
            return false;
        }

        bool ParseNumber(PeekStream stream, out NumberToken token)
        {
            StringBuilder builder = new StringBuilder();
            bool isFloat = false;

            while (stream.Current is char c)
            {
                if (IsDigit(c))
                {
                    builder.Append(c);
                    stream.MoveNext();
                }
                else if (c == '.')
                {
                    isFloat = true;
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
                if (isFloat && float.TryParse(builder.ToString(), out float fValue))
                {
                    token = new FloatToken(fValue);
                    return true;
                }
                else if (int.TryParse(builder.ToString(), out int iValue))
                {
                    token = new IntToken(iValue);
                    return true;
                }
                else
                {
                    Console.WriteLine($"{builder.ToString()} is not a valid number.");
                    token = default;
                    return false;
                }
            }

            token = default;
            return false;
        }

        bool ParseVariable(PeekStream stream, out NameToken token)
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
                token = new NameToken(builder.ToString());
                return true;
            }

            token = default;
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
}