using Moth.AST.Node;
using Moth.LLVM;
using Moth.LLVM.Data;

namespace Moth;

public class HeaderSerializer
{
    private int _currentIndent = 0;
    private StringBuilder _builder = new StringBuilder("root\n");
    
    public string Serialize(LLVMCompiler compiler)
    {
        IncreaseIndent();
        
        foreach (var nmspace in compiler.GlobalNamespace.Namespaces.Values)
        {
            SerializeNamespace(nmspace);
        }

        return _builder.ToString();
    }

    public void SerializeNamespace(Namespace nmspace)
    {
        AppendWithIndent($"::{nmspace.Name}\n");
        IncreaseIndent();

        foreach (var child in nmspace.Namespaces.Values)
        {
            SerializeNamespace(child);
        }
        
        foreach (var @struct in nmspace.Structs.Values)
        {
            SerializeStruct(@struct);
        }

        foreach (var func in nmspace.Functions.Values)
        {
            SerializeFunction(func, true); //TODO: should this be false instead?
        }

        foreach (var global in nmspace.GlobalVariables.Values)
        {
            AppendWithIndent($"global {global.Name} {global.Type.BaseType}\n");
        }
        
        DecreaseIndent();
    }

    public void SerializeStruct(Struct @struct)
    {
        AppendWithIndent($"#{@struct.Name}, struct={@struct is not Class}, privacy={@struct.Privacy}\n");
        IncreaseIndent();

        foreach (var field in @struct.Fields.Values)
        {
            AppendWithIndent($"field {field.Name} {field.Type}, index={field.FieldIndex}, privacy={field.Privacy}\n");
        }

        foreach (var staticMethod in @struct.StaticMethods.Values)
        {
            SerializeFunction(staticMethod, true);
        }

        if (@struct is Class @class)
        {
            foreach (var method in @class.Methods.Values)
            {
                SerializeFunction(method, false);
            }
        }
        
        DecreaseIndent();
    }
    
    public void SerializeFunction(Function func, bool isStatic)
    {
        PrivacyType privacy = func is DefinedFunction definedFunction
            ? definedFunction.Privacy
            : PrivacyType.Public;
        AppendWithIndent($"func {func.Name} {func.ReturnType}, privacy={privacy}, static={isStatic}\n");
        
    }
    
    public void Append(char ch)
    {
        _builder.Append(ch);
    }

    public void Append(string str)
    {
        _builder.Append(str);
    }
    
    public void IncreaseIndent()
    {
        _currentIndent += 2;
    }

    public void DecreaseIndent()
    {
        _currentIndent -= 2;
    }

    public void AppendIndent()
    {
        for (int i = _currentIndent; i > 0; i--)
        {
            Append(' ');
        }
    }

    public void AppendWithIndent(char ch)
    {
        AppendIndent();
        Append(ch);
    }
    
    public void AppendWithIndent(string str)
    {
        AppendIndent();
        Append(str);
    }
}
