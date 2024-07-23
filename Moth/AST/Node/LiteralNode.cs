using System.CodeDom;
using System.Text.RegularExpressions;

namespace Moth.AST.Node;

public class LiteralNode : IExpressionNode
{
    public required int ColumnStart { get; init; }
    public required int LineStart { get; init; }
    public required int ColumnEnd { get; init; }
    public required int LineEnd { get; init; }
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
