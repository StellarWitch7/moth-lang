using LLVMSharp.Interop;
using Moth.AST.Node;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.LLVM.Data;

public class Class : CompilerData
{
    public LLVMTypeRef LLVMType { get; set; }
    public PrivacyType Privacy { get; set; }
    public Dictionary<string, Field> Fields { get; set; } = new Dictionary<string, Field>();
    public Dictionary<string, Function> Functions { get; set; } = new Dictionary<string, Function>();

    public Class(LLVMTypeRef lLVMClass, PrivacyType privacy)
    {
        LLVMType = lLVMClass;
        Privacy = privacy;
    }
}
