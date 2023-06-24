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
                    _tokenList.Add(new Token(TokenType.Semicolon));
                    stream.MoveNext();
                }
                else if (c == '{')
                {
                    _tokenList.Add(new Token(TokenType.OpeningBracket));
                    stream.MoveNext();
                }
                else if (c == '}')
                {
                    _tokenList.Add(new Token(TokenType.ClosingBracket));
                    stream.MoveNext();
                }
                else if (c == '=')
                {
                    _tokenList.Add(new Token(TokenType.AssignmentSeparator));
                    stream.MoveNext();
                }
                else if (c == ')')
                {
                    _tokenList.Add(new Token(TokenType.ClosingParentheses));
                    stream.MoveNext();
                }
                else if (c == '(')
                {
                    _tokenList.Add(new Token(TokenType.OpeningParentheses));
                    stream.MoveNext();
                }
                else if (c == '.')
                {
                    _tokenList.Add(new Token(TokenType.Period));
                    stream.MoveNext();
                }
                else if (c == ',')
                {
                    _tokenList.Add(new Token(TokenType.Comma));
                    stream.MoveNext();
                }
                else if (c == '@')
                {
                    _tokenList.Add(new Token(TokenType.NamespaceTag));
                    stream.MoveNext();
                }
                else if (ParseKeyword(stream, out Token keyToken))
                {
                    _tokenList.Add(keyToken);
                }
                else if (ParseNumber(stream, out Token numToken))
                {
                    _tokenList.Add(numToken);
                }
                else if (ParseName(stream, out Token nameToken))
                {
                    _tokenList.Add(nameToken);
                }
                else if (ParseComparor(stream, out Token comToken))
                {
                    _tokenList.Add(comToken);
                }
                else if (ParseOperator(stream, out Token opToken))
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

        private bool ParseComparor(PeekStream stream, out Token token)
        {
            if (stream.Peek(2) == "==")
            {
                token = new Token(TokenType.Equal);
                stream.MoveAmount(2);
                return true;
            }
            else if (stream.Peek(2) == "!=")
            {
                token = new Token(TokenType.NotEqual);
                stream.MoveAmount(2);
                return true;
            }
            else if (stream.Peek(2) == ">=")
            {
                token = new Token(TokenType.LargerThanOrEqual);
                stream.MoveAmount(2);
                return true;
            }
            else if (stream.Peek(2) == "<=")
            {
                token = new Token(TokenType.LessThanOrEqual);
                stream.MoveAmount(2);
                return true;
            }
            else if (stream.Peek(1) == ">")
            {
                token = new Token(TokenType.LargerThan);
                stream.MoveAmount(2);
                return true;
            }
            else if (stream.Peek(1) == "<")
            {
                token = new Token(TokenType.LessThan);
                stream.MoveAmount(2);
                return true;
            }
            else if (stream.Peek(2) == "&&")
            {
                token = new Token(TokenType.And);
                stream.MoveAmount(2);
                return true;
            }
            else if (stream.Peek(2) == "!&")
            {
                token = new Token(TokenType.NotAnd);
                stream.MoveAmount(2);
                return true;
            }
            else if (stream.Peek(2) == "||")
            {
                token = new Token(TokenType.Or);
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

        bool ParseKeyword(PeekStream stream, out Token token)
        {
            if (!CharChecker.IsLetter(stream.Current))
            {
                token = default;
                return false;
            }

            if (stream.Peek(3) == "if ")
            {
                stream.MoveAmount(3);
                token = new Token(TokenType.If);
                return true;
            }

            if (stream.Peek(6) == "class ")
            {
                stream.MoveAmount(6);
                token = new Token(TokenType.Class);
                return true;
            }

            if (stream.Peek(4) == "new ")
            {
                stream.MoveAmount(4);
                token = new Token(TokenType.New);
                return true;
            }

            if (stream.Peek(4) == "var ")
            {
                stream.MoveAmount(4);
                token = new Token(TokenType.Var);
                return true;
            }

            if (stream.Peek(5) == "else ")
            {
                stream.MoveAmount(5);
                token = new Token(TokenType.Else);
                return true;
            }

            if (stream.Peek(7) == "public ")
            {
                stream.MoveAmount(7);
                token = new Token(TokenType.Public);
                return true;
            }

            if (stream.Peek(8) == "private ")
            {
                stream.MoveAmount(8);
                token = new Token(TokenType.Private);
                return true;
            }

            if (stream.Peek(5) == "void ")
            {
                stream.MoveAmount(5);
                token = new Token(TokenType.Void);
                return true;
            }

            if (stream.Peek(7) == "return ")
            {
                stream.MoveAmount(7);
                token = new Token(TokenType.Return);
                return true;
            }

            if (stream.Peek(4) == "nix ")
            {
                stream.MoveAmount(4);
                token = new Token(TokenType.Nix);
                return true;
            }

            if (stream.Peek(5) == "true ")
            {
                stream.MoveAmount(5);
                token = new Token(TokenType.True);
                return true;
            }

            if (stream.Peek(6) == "false ")
            {
                stream.MoveAmount(6);
                token = new Token(TokenType.False);
                return true;
            }

            if (stream.Peek(5) == "bool ")
            {
                stream.MoveAmount(5);
                token = new Token(TokenType.Bool);
                return true;
            }

            if (stream.Peek(4) == "num ")
            {
                stream.MoveAmount(4);
                token = new Token(TokenType.Num);
                return true;
            }

            if (stream.Peek(4) == "obj ")
            {
                stream.MoveAmount(4);
                token = new Token(TokenType.Obj);
                return true;
            }

            if (stream.Peek(4) == "str ")
            {
                stream.MoveAmount(4);
                token = new Token(TokenType.Str);
                return true;
            }

            if (stream.Peek(7) == "import ")
            {
                stream.MoveAmount(7);
                token = new Token(TokenType.Import);
                return true;
            }

            if (stream.Peek(4) == "set ")
            {
                stream.MoveAmount(4);
                token = new Token(TokenType.Set);
                return true;
            }

            if (stream.Peek(5) == "call ")
            {
                stream.MoveAmount(5);
                token = new Token(TokenType.Call);
                return true;
            }

            token = default;
            return false;
        }

        bool ParseOperator(PeekStream stream, out Token token)
        {
            StringBuilder builder = new StringBuilder();
            Token newToken = null;

            while (stream.Current is char c)
            {
                if (c == '+')
                {
                    newToken = new Token(TokenType.Addition);
                    builder.Append(c);
                    stream.MoveNext();
                }
                else if (c == '-')
                {
                    newToken = new Token(TokenType.Subtraction);
                    builder.Append(c);
                    stream.MoveNext();
                }
                else if (c == '*')
                {
                    newToken = new Token(TokenType.Multiplication);
                    builder.Append(c);
                    stream.MoveNext();
                }
                else if (c == '/')
                {
                    newToken = new Token(TokenType.Division);
                    builder.Append(c);
                    stream.MoveNext();
                }
                else if (c == '^')
                {
                    newToken = new Token(TokenType.Exponential);
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

        bool ParseNumber(PeekStream stream, out Token token)
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
                    token = new Token(TokenType.Float, fValue);
                    return true;
                }
                else if (int.TryParse(builder.ToString(), out int iValue))
                {
                    token = new Token(TokenType.Int, iValue);
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

        bool ParseName(PeekStream stream, out Token token)
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
                token = new Token(TokenType.Name, builder.ToString());
                return true;
            }

            token = default;
            return false;
        }
    }
}
