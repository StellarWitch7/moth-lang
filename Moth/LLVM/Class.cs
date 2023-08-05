using LLVMSharp.Interop;
using Moth.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.LLVM;

public class Class
{
    public LLVMTypeRef LLVMClass { get; set; }
    public PrivacyType Privacy { get; set; }
    public Dictionary<string, Function> Functions { get; set; }

    public Class(LLVMTypeRef lLVMClass, PrivacyType privacy)
    {
        LLVMClass = lLVMClass;
        Privacy = privacy;
    }
}
