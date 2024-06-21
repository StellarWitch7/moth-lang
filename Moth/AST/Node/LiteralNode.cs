using System.CodeDom;
using System.Text.RegularExpressions;

namespace Moth.AST.Node;

public class LiteralNode : IExpressionNode
{
    public object? Value { get; set; }

    public LiteralNode(object? value) => Value = value;

    public string GetSource() =>
        Value is string str
            ? $"\"{str}\""
            : Value is char ch
                ? $"'{ch switch
                {
                    '\n' => @"\n",
                    '\0' => @"\0",
                    _ => throw new NotImplementedException()
                }}'"
                : Value != null
                    ? Value.ToString()
                    : Reserved.Null;
}
