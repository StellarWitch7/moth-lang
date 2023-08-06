using LLVMSharp.Interop;
using Moth.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Moth.LLVM;

public static class LLVMCodeGenerator
{
    public static void ConvertScript(CompilerContext compiler, ScriptAST script)
    {
        foreach (var @class in script.ClassNodes)
        {
            ConvertClass(compiler, @class);
        }
    }

    public static void ConvertClass(CompilerContext compiler, ClassNode @class)
    {
        LLVMTypeRef newStruct = compiler.Context.CreateNamedStruct(@class.Name);

        List<LLVMTypeRef> types = new List<LLVMTypeRef>();

        foreach (FieldNode field in @class.Scope.Statements.OfType<FieldNode>())
        {
            types.Add(DefToLLVMType(field.Type));
        }

        newStruct.StructSetBody(types.ToArray(), false);
        var newClass = new Class(newStruct, @class.Privacy);
        compiler.Classes.Add(@class.Name, newClass);

        foreach (MethodDefNode methodDef in @class.Scope.Statements.OfType<MethodDefNode>())
        {
            ConvertMethod(compiler, newClass, methodDef);
        }
    }

    public static void ConvertMethod(CompilerContext compiler, Class @class, MethodDefNode methodDef)
    {
        List<LLVMTypeRef> paramTypes = new List<LLVMTypeRef> { LLVMTypeRef.CreatePointer(@class.LLVMClass, 0) };

        foreach (ParameterNode param in methodDef.Params)
        {
            paramTypes.Add(DefToLLVMType(param.Type));
        }

        var funcType = LLVMTypeRef.CreateFunction(DefToLLVMType(methodDef.ReturnType), paramTypes.ToArray());
        LLVMValueRef func = compiler.Module.AddFunction(methodDef.Name, funcType);
        @class.Functions.Add(methodDef.Name, new Function(func, methodDef.Privacy));
    }

    public static LLVMTypeRef DefToLLVMType(DefinitionType definitionType)
    {
        switch (definitionType)
        {
            case DefinitionType.Int32:
                return LLVMTypeRef.Int32;
            case DefinitionType.Float32:
                return LLVMTypeRef.Float;
            case DefinitionType.Bool:
                return LLVMTypeRef.Int1;
            default:
                throw new NotImplementedException();
        }
    }
}
