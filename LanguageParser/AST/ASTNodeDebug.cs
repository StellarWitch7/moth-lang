using System.CodeDom.Compiler;
using System.Reflection;

namespace LanguageParser.AST;

internal partial class ASTNode
{
	public string GetDebugString(string? indent = null)
	{
		var buffer = new StringWriter();
		var writer = new IndentedTextWriter(buffer, indent ?? string.Empty);
		WriteDebugString(writer, indent is not null);
		return buffer.ToString();
	}

	public virtual void WriteDebugString(IndentedTextWriter writer, bool indent = false)
	{
		var type = GetType();
		var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

		writer.Write(type.Name);

		if (props.Length == 0)
		{
			writer.WriteLine(" {}");
		}
		else if (indent && (props.Length != 1 || props[0].PropertyType.IsAssignableTo(typeof(ASTNode))))
		{
			writer.WriteLine();
			writer.WriteLine('{');
			writer.Indent++;

			for (var i = 0; i < props.Length; i++)
			{
				var prop = props[i];
				var value = prop.GetValue(this);
				if (i != 0) writer.WriteLine(',');
				writer.Write(prop.Name);
				writer.Write(" = ");
				WriteDebugString(writer, value, indent);
			}

			writer.WriteLine();

			writer.Indent--;
			writer.Write('}');
		}
		else
		{
			writer.Write(" { ");

			for (var i = 0; i < props.Length; i++)
			{
				var prop = props[i];
				var value = prop.GetValue(this);
				if (i != 0) writer.Write(',');
				writer.Write(prop.Name);
				writer.Write(" = ");
				WriteDebugString(writer, value, indent);
			}

			writer.Write(" }");
		}
	}

	protected static void WriteDebugString(IndentedTextWriter writer, object? obj, bool indent = false)
	{
		switch (obj)
		{
			case null:
			{
				writer.Write("null");
				break;
			}

			case string str:
			{
				writer.Write('"');
				writer.Write(str);
				writer.Write('"');
				break;
			}

			case ASTNode node:
			{
				node.WriteDebugString(writer, indent);
				break;
			}

			default:
			{
				writer.Write(obj.ToString() ?? "???");
				break;
			}
		}
	}
}
