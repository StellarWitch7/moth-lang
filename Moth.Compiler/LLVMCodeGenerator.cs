using LLVMSharp.Interop;
using Moth.Compiler.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Moth.Compiler;

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
        LLVMTypeRef newClass = compiler.Context.CreateNamedStruct(@class.Name);

        List<LLVMTypeRef> types = new List<LLVMTypeRef>();

        foreach (FieldNode field in @class.StatementListNode.StatementNodes.OfType<FieldNode>())
        {
            types.Add(DefToLLVMType(field.Type));
        }

        newClass.StructSetBody(types.ToArray(), false);

        foreach (MethodDefNode methodDef in @class.StatementListNode.StatementNodes.OfType<MethodDefNode>())
        {
            List<LLVMTypeRef> paramTypes = new List<LLVMTypeRef> { LLVMTypeRef.CreatePointer(newClass, 0) };

            foreach (ParameterNode param in methodDef.Params.ParameterNodes)
            {
                paramTypes.Add(DefToLLVMType(param.Type));
            }

            var funcType = LLVMTypeRef.CreateFunction(DefToLLVMType(methodDef.ReturnType), paramTypes.ToArray());
            LLVMValueRef func = compiler.Module.AddFunction(methodDef.Name, funcType);
        }
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
