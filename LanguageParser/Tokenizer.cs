
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

        public void PrintTokens(float delay)
        {
            foreach (Token token in _tokenList)
            {
                foreach (char c in token.ToString())
                {
                    Console.Write(c);
                    Thread.Sleep((int)(delay * 50));
                }

                Console.WriteLine();
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
                else if (c == '[')
                {
                    _tokenList.Add(new OpeningBracketToken());
                    stream.MoveNext();
                }
                else if (c == ']')
                {
                    _tokenList.Add(new ClosingBracketToken());
                    stream.MoveNext();
                }
                else if (c == '=')
                {
                    _tokenList.Add(new AssignmentToken());
                    stream.MoveNext();
                }
                else if (c == ')')
                {
                    _tokenList.Add(new ClosingParenthesesToken());
                    stream.MoveNext();
                }
                else if (c == '(')
                {
                    _tokenList.Add(new ClosingParenthesesToken());
                    stream.MoveNext();
                }
                else if (c == '.')
                {
                    _tokenList.Add(new PeriodToken());
                    stream.MoveNext();
                }
                else if (ParseKeyword(stream, out KeywordToken keyToken))
                {
                    _tokenList.Add(keyToken);
                }
                else if (ParseNumber(stream, out NumberToken numToken))
                {
                    _tokenList.Add(numToken);
                }
                else if (ParseName(stream, out NameToken nameToken))
                {
                    _tokenList.Add(nameToken);
                }
                else if (ParseComparor(stream, out ComparisonToken comToken))
                {
                    _tokenList.Add(comToken);
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

        private bool ParseComparor(PeekStream stream, out ComparisonToken token)
        {
            if (stream.Peek(2) == "==")
            {
                token = new ComparisonToken(ComparisonType.Equal);
                stream.MoveAmount(2);
                return true;
            }
            else if (stream.Peek(2) == "!=")
            {
                token = new ComparisonToken(ComparisonType.NotEqual);
                stream.MoveAmount(2);
                return true;
            }
            else if (stream.Peek(2) == ">=")
            {
                token = new ComparisonToken(ComparisonType.LargerThanOrEqual);
                stream.MoveAmount(2);
                return true;
            }
            else if (stream.Peek(2) == "<=")
            {
                token = new ComparisonToken(ComparisonType.LessThanOrEqual);
                stream.MoveAmount(2);
                return true;
            }
            else if (stream.Peek(2) == ">>")
            {
                token = new ComparisonToken(ComparisonType.LargerThan);
                stream.MoveAmount(2);
                return true;
            }
            else if (stream.Peek(2) == "<<")
            {
                token = new ComparisonToken(ComparisonType.LessThan);
                stream.MoveAmount(2);
                return true;
            }
            else if (stream.Peek(2) == "&&")
            {
                token = new ComparisonToken(ComparisonType.And);
                stream.MoveAmount(2);
                return true;
            }
            else if (stream.Peek(2) == "!&")
            {
                token = new ComparisonToken(ComparisonType.NotAnd);
                stream.MoveAmount(2);
                return true;
            }
            else if (stream.Peek(2) == "||")
            {
                token = new ComparisonToken(ComparisonType.Or);
                stream.MoveAmount(2);
                return true;
            }
            else
            {
                token = null;
                return false;
            }
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
                stream.MoveAmount(3);
                token = new KeywordToken(Keyword.If);
                return true;
            }

            if (stream.Peek(6) == "class ")
            {
                stream.MoveAmount(6);
                token = new KeywordToken(Keyword.Class);
                return true;
            }

            if (stream.Peek(4) == "new ")
            {
                stream.MoveAmount(4);
                token = new KeywordToken(Keyword.New);
                return true;
            }

            if (stream.Peek(4) == "var ")
            {
                stream.MoveAmount(4);
                token = new KeywordToken(Keyword.Var);
                return true;
            }

            if (stream.Peek(5) == "else ")
            {
                stream.MoveAmount(5);
                token = new KeywordToken(Keyword.Else);
                return true;
            }

            if (stream.Peek(7) == "public ")
            {
                stream.MoveAmount(7);
                token = new KeywordToken(Keyword.Public);
                return true;
            }

            if (stream.Peek(8) == "private ")
            {
                stream.MoveAmount(8);
                token = new KeywordToken(Keyword.Private);
                return true;
            }

            if (stream.Peek(5) == "void ")
            {
                stream.MoveAmount(5);
                token = new KeywordToken(Keyword.Void);
                return true;
            }

            if (stream.Peek(7) == "return ")
            {
                stream.MoveAmount(7);
                token = new KeywordToken(Keyword.Return);
                return true;
            }

            if (stream.Peek(4) == "nix ")
            {
                stream.MoveAmount(4);
                token = new KeywordToken(Keyword.Nix);
                return true;
            }

            if (stream.Peek(5) == "true ")
            {
                stream.MoveAmount(5);
                token = new KeywordToken(Keyword.True);
                return true;
            }

            if (stream.Peek(6) == "false ")
            {
                stream.MoveAmount(6);
                token = new KeywordToken(Keyword.False);
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
