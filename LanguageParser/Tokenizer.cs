
using LanguageParser.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageParser
{
    internal class Tokenizer
    {
        private List<Token> _tokenList = new List<Token>();
        private StringBuilder _script = new StringBuilder();
        private int _currentLine = 1;

        public Tokenizer(StringBuilder script)
        {
            _script = script;
        }

        public List<Token> Tokens
        {
            get
            {
                return _tokenList;
            }
        }

        public void PrintTokens()
        {
            foreach (Token token in _tokenList)
            {
                Console.WriteLine(token.ToString());
            }
        }

        public bool ParseStatements()
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

        bool ParseStatement(PeekStream stream, List<Token> tokenList)
        {
            while (stream.Current != null)
            {
                if (stream.Current == ';')
                {
                    tokenList.Add(new EndStatementToken());
                    stream.MoveNext();
                    return true;
                }
                else if (CharChecker.IsSpace(stream.Current))
                {
                    if (CharChecker.IsNewLine(stream.Current))
                    {
                        _currentLine++;
                    }

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
                        Console.WriteLine($"Expected expression at line {_currentLine}.");
                    }
                }
            }

            return false;
        }

        bool ParseExpression(PeekStream stream, List<Token> tokenList)
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
                    Console.WriteLine($"Invalid expression at line {_currentLine}.");
                    return false;
                }
            }

            return false;
        }

        bool ParseOperator(PeekStream stream, out OperatorToken token)
        {
            StringBuilder builder = new StringBuilder();
            OperatorToken newToken = null;

            while (stream.Current is char c)
            {
                if (c == '+')
                {
                    newToken = new OperatorToken(Operator.Addition);
                    builder.Append(c);
                    stream.MoveNext();
                }
                else if (c == '-')
                {
                    newToken = new OperatorToken(Operator.Subtraction);
                    builder.Append(c);
                    stream.MoveNext();
                }
                else if (c == '*')
                {
                    newToken = new OperatorToken(Operator.Multiplication);
                    builder.Append(c);
                    stream.MoveNext();
                }
                else if (c == '/')
                {
                    newToken = new OperatorToken(Operator.Division);
                    builder.Append(c);
                    stream.MoveNext();
                }
                else if (c == '^')
                {
                    newToken = new OperatorToken(Operator.Exponential);
                    builder.Append(c);
                    stream.MoveNext();
                }
                else
                {
                    break;
                }
            }

            if (builder.Length == 1)
            {
                token = newToken;
                return true;
            }
            else
            {
                token = null;
                return false;
            }
        }

        bool ParseNumber(PeekStream stream, out NumberToken token)
        {
            StringBuilder builder = new StringBuilder();
            bool isFloat = false;

            while (stream.Current is char c)
            {
                if (CharChecker.IsDigit(c))
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
                    Console.WriteLine($"{builder} at line {_currentLine} is not a valid number.");
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
                if (CharChecker.IsVariableChar(c) || (builder.Length > 0 && CharChecker.IsDigit(c)))
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
    }
}
