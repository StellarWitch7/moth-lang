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
    public string Name { get; set; }
    public LLVMValueRef LLVMFunc { get; set; }
    public LLVMTypeRef LLVMFuncType { get; set; }
    public Type ReturnType { get; set; }
    public PrivacyType Privacy { get; set; }
    public Class? OwnerClass { get; set; }
    public Scope? OpeningScope { get; set; }
    public List<Parameter> Params { get; set; }
    public bool IsVariadic { get; set; }

    public Function(string name, LLVMValueRef lLVMFunc, LLVMTypeRef lLVMFuncType, Type returnType,
        PrivacyType privacy, Class? ownerClass, List<Parameter> @params, bool isVariadic)
    {
        Name = name;
        LLVMFunc = lLVMFunc;
        LLVMFuncType = lLVMFuncType;
        ReturnType = returnType;
        Privacy = privacy;
        OwnerClass = ownerClass;
        Params = @params;
        IsVariadic = isVariadic;
    }
}
