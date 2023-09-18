using LLVMSharp.Interop;
using Moth.AST.Node;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.LLVM;

public class DefTypedValueContext : ValueContext
{
    public TypeRefNode TypeRef { get; set; }
    public LLVMTypeRef LLVMType { get; set; }
    public LLVMValueRef LLVMValue { get; set; }

    public DefTypedValueContext(CompilerContext compiler, TypeRefNode typeRef, LLVMValueRef value)
        : base(LLVMCompiler.DefToLLVMType(compiler, typeRef), value)
    {
        TypeRef = typeRef;
    }
}