using LLVMSharp.Interop;
using Moth.AST.Node;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.LLVM;

public class Function
{
    public LLVMValueRef LLVMFunc { get; set; }
    public PrivacyType Privacy { get; set; }
    public Scope OpeningScope { get; set; }
    public List<Parameter> Params { get; set; } = new List<Parameter>();

    public Function(LLVMValueRef lLVMFunc, PrivacyType privacy)
    {
        LLVMFunc = lLVMFunc;
        Privacy = privacy;
    }

    public Function(LLVMValueRef lLVMFunc, PrivacyType privacy, List<Parameter> @params) : this(lLVMFunc, privacy)
    {
        Params = @params;
    }
}
