using System.CodeDom.Compiler;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Moth.AST;

public abstract partial class ASTNode
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
        Type type = GetType();
        PropertyInfo[] props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

        writer.Write(type.Name);

        if (props.Length == 0)
        {
            writer.WriteLine(" {}");
        }
        else if (indent && (
            props.Length != 1 ||
            props[0].PropertyType.IsAssignableTo(typeof(ASTNode)) ||
            props[0].PropertyType.IsAssignableTo(typeof(IEnumerable))
        ))
        {
            writer.WriteLine(" {");
            writer.Indent++;

            for (int i = 0; i < props.Length; i++)
            {
                PropertyInfo prop = props[i];
                object? value = prop.GetValue(this);
                if (i != 0)
                {
                    writer.WriteLine(',');
                }

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

            for (int i = 0; i < props.Length; i++)
            {
                PropertyInfo prop = props[i];
                object? value = prop.GetValue(this);
                if (i != 0)
                {
                    writer.Write(',');
                }

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

            case ReadOnlyMemory<char> str:
                {
                    writer.Write('"');
                    writer.Write(str.Span);
                    writer.Write('"');
                    break;
                }

            case ASTNode node:
                {
                    node.WriteDebugString(writer, indent);
                    break;
                }

            case IEnumerable enumerable:
                {
                    IEnumerable<object?> values = enumerable.Cast<object?>();

                    if (!values.TryGetNonEnumeratedCount(out int count))
                    {
                        count = int.MaxValue;
                    }

                    if (count == 0)
                    {
                        writer.Write("[]");
                        break;
                    }

                    if (indent)
                    {
                        writer.WriteLine('[');
                        writer.Indent++;
                        foreach (object? value in values)
                        {
                            WriteDebugString(writer, value, indent);
                            writer.Write(',');
                            writer.WriteLine();
                        }

                        writer.Indent--;
                        writer.Write(']');
                    }
                    else
                    {
                        writer.Write('[');
                        bool first = true;
                        foreach (object? value in values)
                        {
                            WriteDebugString(writer, value, indent);
                            if (!first)
                            {
                                writer.Write(", ");
                            }

                            first = false;
                        }

                        writer.Write(']');
                    }

                    break;
                }

            case ITuple tuple:
                {
                    IEnumerable<object?> values = tuple
                    .GetType()
                    .GetFields()
                    .Select(m => m.GetValue(tuple));

                    if (!values.TryGetNonEnumeratedCount(out int count))
                    {
                        count = int.MaxValue;
                    }

                    if (count == 0)
                    {
                        writer.Write("()");
                        break;
                    }

                    if (indent)
                    {
                        writer.WriteLine('(');
                        writer.Indent++;

                        foreach (object? value in values)
                        {
                            WriteDebugString(writer, value, indent);
                            writer.Write(',');
                            writer.WriteLine();
                        }

                        writer.Indent--;
                        writer.Write(')');
                    }
                    else
                    {
                        writer.Write('(');
                        bool first = true;
                        foreach (object? value in values)
                        {
                            WriteDebugString(writer, value, indent);
                            if (!first)
                            {
                                writer.Write(", ");
                            }

                            first = false;
                        }

                        writer.Write(')');
                    }

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
