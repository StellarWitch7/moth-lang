using System.CodeDom.Compiler;

namespace LanguageParser.AST;

public class ConstantNode : ExpressionNode
{
	public object Value { get; }

	public ConstantNode(object value)
	{
		Value = value;
	}

	public override void WriteDebugString(IndentedTextWriter writer, bool indent = false)
	{
		writer.Write(nameof(ConstantNode));
		writer.Write(" { ");
		writer.Write(nameof(Value));
		writer.Write(" = ");
		writer.Write('(');
		writer.Write(Value.GetType().Name);
		writer.Write(") ");
		writer.Write(Value);
		writer.Write(" }");
	}
}