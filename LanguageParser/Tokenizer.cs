
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

        public bool ParseScript()
        {
            var stream = new PeekStream(_script.ToString());

            if (stream.Length == 0)
            {
                return false;
            }

            while (stream.Current is char c)
            {
                if (CharChecker.IsSpace(c))
                {
                    if (CharChecker.IsNewLine(c))
                    {
                        _currentLine++;
                    }

                    stream.MoveNext();
                }
                else if (ParseComment(stream))
                {

                }
                else if (c == ';')
                {
                    _tokenList.Add(new EndStatementToken());
                    stream.MoveNext();
                }
                else if (ParseKeyword(stream, out KeywordToken kToken))
                {
                    _tokenList.Add(kToken);
                    stream.MoveNext();
                }
                else if (c == '{')
                {
                    _tokenList.Add(new OpeningToken());
                    stream.MoveNext();
                }
                else if (c == '}')
                {
                    _tokenList.Add(new ClosingToken());
                    stream.MoveNext();
                }
                else if (c == '=')
                {
                    _tokenList.Add(new AssignmentToken());
                    stream.MoveNext();
                }
                else if (ParseNumber(stream, out NumberToken numToken))
                {
                    _tokenList.Add(numToken);
                }
                else if (ParseName(stream, out NameToken nameToken))
                {
                    _tokenList.Add(nameToken);
                }
                else if (ParseOperator(stream, out OperatorToken opToken))
                {
                    _tokenList.Add(opToken);
                }
                else
                {
                    break;
                }
            }

            return true;
        }

        private bool ParseComment(PeekStream stream)
        {
            if (stream.Peek(2) == "//")
            {
                while (stream.Current is char c)
                {
                    stream.MoveNext();

                    if (CharChecker.IsNewLine(c))
                    {
                        _currentLine++;
                        return true;
                    }
                }
            }

            return false;
        }

        bool ParseKeyword(PeekStream stream, out KeywordToken token)
        {
            if (!CharChecker.IsLetter(stream.Current))
            {
                token = default;
                return false;
            }

            if (stream.Peek(3) == "if ")
            {
                stream.MoveAmount(2);
                token = new KeywordToken(Keyword.If);
                return true;
            }

            if (stream.Peek(6) == "class ")
            {
                stream.MoveAmount(5);
                token = new KeywordToken(Keyword.Class);
                return true;
            }

            token = default;
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

        bool ParseName(PeekStream stream, out NameToken token)
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
