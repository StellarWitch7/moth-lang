using LLVMSharp.Interop;
using Moth.AST.Node;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.LLVM.Data;

public class Function : CompilerData
{
    public LLVMValueRef LLVMFunc { get; set; }
    public LLVMTypeRef LLVMFuncType { get; set; }
    public LLVMTypeRef LLVMReturnType { get; set; }
    public PrivacyType Privacy { get; set; }
    public Class ClassOfReturnType { get; set; }
    public Class OwnerClass { get; set; }
    public Scope OpeningScope { get; set; }
    public List<Parameter> Params { get; set; }
    public bool IsVariadic { get; set; }

    public Function(LLVMValueRef lLVMFunc, LLVMTypeRef lLVMFuncType, LLVMTypeRef lLVMReturnType,
        PrivacyType privacy, Class classOfReturnType, Class ownerClass, List<Parameter> @params,
        bool isVariadic)
    {
        LLVMFunc = lLVMFunc;
        LLVMFuncType = lLVMFuncType;
        LLVMReturnType = lLVMReturnType;
        Privacy = privacy;
        ClassOfReturnType = classOfReturnType;
        OwnerClass = ownerClass;
        Params = @params;
        IsVariadic = isVariadic;
    }
}
