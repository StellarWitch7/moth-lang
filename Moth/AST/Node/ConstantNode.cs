using System.CodeDom.Compiler;

namespace Moth.AST.Node;

public class ConstantNode : ExpressionNode
{
    public object? Value { get; set; }

    public ConstantNode(object? value) => Value = value;

    public override void WriteDebugString(IndentedTextWriter writer, bool indent = false)
    {
        writer.Write(nameof(ConstantNode));
        writer.Write(" { ");
        writer.Write(nameof(Value));
        writer.Write(" = ");
        writer.Write('(');

        if (Value != null)
        {
            writer.Write(Value.GetType().Name);
        }
        else
        {
            writer.Write("null");
        }

        writer.Write(") ");
        writer.Write(Value is char ch ? (ulong)ch : Value);
        writer.Write(" }");
    }
}