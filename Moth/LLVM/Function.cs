using LLVMSharp.Interop;
using Moth.AST.Node;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.LLVM;

public class Function : CompilerData
{
    public LLVMValueRef LLVMFunc { get; set; }
    public LLVMTypeRef LLVMFuncType { get; set; }
    public LLVMTypeRef LLVMReturnType { get; set; }
    public PrivacyType Privacy { get; set; }
    public TypeRefNode ReturnType { get; set; }
    public Scope OpeningScope { get; set; }
    public List<Parameter> Params { get; set; }
    public bool IsGlobal { get; set; }

    public Function(LLVMValueRef lLVMFunc, LLVMTypeRef lLVMFuncType, LLVMTypeRef lLVMReturnType,
        PrivacyType privacy, TypeRefNode returnType, List<Parameter> @params, bool isGlobal = false)
    {
        LLVMFunc = lLVMFunc;
        LLVMFuncType = lLVMFuncType;
        LLVMReturnType = lLVMReturnType;
        Privacy = privacy;
        ReturnType = returnType;
        Params = @params;
        IsGlobal = isGlobal;
    }
}
